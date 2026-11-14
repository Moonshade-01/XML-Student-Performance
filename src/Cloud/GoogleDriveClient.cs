using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StudentPerformance.Cloud;

public sealed record DriveXmlFile(string Id, string Name, string ModifiedTime)
{
    public string Label => $"{Name}  ·  {ModifiedTime}";
}

public sealed class GoogleDriveClient
{
    private readonly GoogleDriveAuth auth;
    private readonly HttpClient http;
    public GoogleDriveClient(GoogleDriveAuth auth) : this(auth, new HttpClient { Timeout = TimeSpan.FromSeconds(90) }) { }
    internal GoogleDriveClient(GoogleDriveAuth auth, HttpClient http) { this.auth = auth; this.http = http; }

    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> create, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            var accessToken = await auth.GetTokenAsync(token);
            using var request = create();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await http.SendAsync(request, token);
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
            {
                response.Dispose(); auth.InvalidateAccessToken(); continue;
            }
            if (response.IsSuccessStatusCode) return response;
            var status = (int)response.StatusCode;
            response.Dispose();
            throw new InvalidOperationException($"Google Drive API: помилка {status}. Перевірте інтернет, дозволи та ввімкнення Drive API у Google Cloud.");
        }
    }

    public async Task<IReadOnlyList<DriveXmlFile>> ListXmlAsync(CancellationToken token)
    {
        var files = new List<DriveXmlFile>();
        string? next = null;
        do
        {
            var query = "trashed = false and (mimeType = 'application/xml' or mimeType = 'text/xml' or name contains '.xml' or name contains '.XML')";
            var url = "https://www.googleapis.com/drive/v3/files?pageSize=100&orderBy=modifiedTime%20desc&fields=nextPageToken,files(id,name,modifiedTime)&q=" + Uri.EscapeDataString(query);
            if (next is not null) url += "&pageToken=" + Uri.EscapeDataString(next);
            using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, url), token);
            using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            foreach (var file in data.RootElement.GetProperty("files").EnumerateArray())
                files.Add(new(file.GetProperty("id").GetString()!, file.GetProperty("name").GetString()!, file.TryGetProperty("modifiedTime", out var date) ? date.GetString() ?? "" : ""));
            next = data.RootElement.TryGetProperty("nextPageToken", out var page) ? page.GetString() : null;
        } while (!string.IsNullOrEmpty(next));
        return files;
    }

    public async Task DownloadAsync(DriveXmlFile file, string path, CancellationToken token)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get,
            "https://www.googleapis.com/drive/v3/files/" + Uri.EscapeDataString(file.Id) + "?alt=media"), token);
        await using var output = File.Create(path);
        await response.Content.CopyToAsync(output, token);
    }

    public async Task UploadAsync(string name, string xml, CancellationToken token)
    {
        // Always create a new file; never overwrite the user's source document.
        using var response = await SendAsync(() =>
        {
            var multipart = new MultipartContent("related");
            multipart.Add(new StringContent(JsonSerializer.Serialize(new { name, mimeType = "application/xml" }), Encoding.UTF8, "application/json"));
            multipart.Add(new StringContent(xml, Encoding.UTF8, "application/xml"));
            return new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name") { Content = multipart };
        }, token);
    }
}
