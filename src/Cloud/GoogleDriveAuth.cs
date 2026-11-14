using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StudentPerformance.Cloud;

public sealed class GoogleDriveAuth
{
    private const string CredentialsKey = "drive-desktop-client";
    private const string RefreshKey = "drive-refresh-token";
    private const string Scope = "https://www.googleapis.com/auth/drive.readonly https://www.googleapis.com/auth/drive.file";
    private readonly HttpClient http;
    public GoogleDriveAuth() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(45) }) { }
    internal GoogleDriveAuth(HttpClient http) { this.http = http; }
    private string? accessToken;
    private DateTimeOffset expires;
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task ImportClientAsync(string json)
    {
        ReadClient(json); // Reject Web OAuth credentials before replacing a working configuration.
        await gate.WaitAsync();
        try
        {
            await SecureStorage.Default.SetAsync(CredentialsKey, json);
            SecureStorage.Default.Remove(RefreshKey);
            accessToken = null;
        }
        finally { gate.Release(); }
    }

    private static (string Id, string Secret) ReadClient(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("installed", out var installed)
            || !installed.TryGetProperty("client_id", out var id) || string.IsNullOrWhiteSpace(id.GetString()))
            throw new FormatException("Оберіть JSON OAuth-клієнта типу Desktop app (секція installed).");
        return (id.GetString()!, installed.TryGetProperty("client_secret", out var secret) ? secret.GetString() ?? "" : "");
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (accessToken is not null && DateTimeOffset.UtcNow < expires) return accessToken;
            var json = await SecureStorage.Default.GetAsync(CredentialsKey)
                ?? throw new InvalidOperationException("Спочатку імпортуйте OAuth JSON. Інструкція — у README.");
            var client = ReadClient(json);
            var refresh = await SecureStorage.Default.GetAsync(RefreshKey);
            if (refresh is not null)
            {
                using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["client_id"] = client.Id, ["client_secret"] = client.Secret, ["refresh_token"] = refresh, ["grant_type"] = "refresh_token" }), cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode) return await AcceptTokenAsync(body);
                using var error = JsonDocument.Parse(body);
                if (!error.RootElement.TryGetProperty("error", out var code) || code.GetString() != "invalid_grant")
                    throw new InvalidOperationException($"Google OAuth: не вдалося оновити сесію ({(int)response.StatusCode}). Спробуйте пізніше.");
                SecureStorage.Default.Remove(RefreshKey);
            }
            return await SignInAsync(client.Id, client.Secret, cancellationToken);
        }
        finally { gate.Release(); }
    }

    private async Task<string> SignInAsync(string clientId, string secret, CancellationToken cancellationToken)
    {
        // Desktop OAuth loopback redirect: listen only on the local IPv4 interface.
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var redirect = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirect);
        listener.Start();
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId, ["redirect_uri"] = redirect, ["response_type"] = "code",
            ["scope"] = Scope, ["state"] = state, ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256", ["access_type"] = "offline", ["prompt"] = "consent"
        };
        var url = "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", parameters.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        if (!await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred))
            throw new InvalidOperationException("Не вдалося відкрити браузер для входу Google.");
        string? authorizationCode = null;
        while (authorizationCode is null)
        {
            var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
            var query = context.Request.QueryString;
            var valid = query["state"] == state && context.Request.Url?.AbsolutePath == "/";
            var bytes = Encoding.UTF8.GetBytes(valid ? "Google sign-in received. Return to Student-Performance." : "Invalid OAuth callback.");
            context.Response.StatusCode = valid ? 200 : 400;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, timeout.Token);
            context.Response.Close();
            if (!valid) continue;
            if (query["error"] is not null) throw new InvalidOperationException("Вхід Google скасовано або доступ не надано.");
            authorizationCode = query["code"];
        }
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId, ["client_secret"] = secret, ["code"] = authorizationCode,
            ["redirect_uri"] = redirect, ["code_verifier"] = verifier, ["grant_type"] = "authorization_code"
        }), timeout.Token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Google OAuth: обмін коду не вдався ({(int)response.StatusCode}).");
        return await AcceptTokenAsync(await response.Content.ReadAsStringAsync(timeout.Token));
    }

    private async Task<string> AcceptTokenAsync(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        // Initial consent must include both read and create scopes.
        if (root.TryGetProperty("scope", out var granted))
        {
            var scopes = (granted.GetString() ?? "").Split(' ');
            if (!Scope.Split(' ').All(scopes.Contains))
                throw new InvalidOperationException("Надайте обидва запитані дозволи Google Drive та повторіть вхід.");
        }
        if (root.TryGetProperty("refresh_token", out var refresh))
            await SecureStorage.Default.SetAsync(RefreshKey, refresh.GetString()!);
        accessToken = root.GetProperty("access_token").GetString()!;
        expires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, root.GetProperty("expires_in").GetInt32() - 60));
        return accessToken;
    }

    public async Task SignOutAsync()
    {
        await gate.WaitAsync();
        try { accessToken = null; SecureStorage.Default.Remove(RefreshKey); }
        finally { gate.Release(); }
    }

    public void InvalidateAccessToken() { accessToken = null; }
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
