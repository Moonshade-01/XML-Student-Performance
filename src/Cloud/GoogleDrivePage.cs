using StudentPerformance.Core;

namespace StudentPerformance.Cloud;

// UI coordinates services; OAuth and Drive operations live in separate classes.
public sealed class GoogleDrivePage : ContentPage
{
    private readonly GoogleDriveAuth auth;
    private readonly GoogleDriveClient drive;
    private readonly PerformanceService service;
    private readonly IReadOnlyList<GradeRecord> rows;
    private readonly Func<string, Task> openXml;
    private readonly Picker files = new() { Title = "XML-файл у Google Drive", ItemDisplayBinding = new Binding(nameof(DriveXmlFile.Label)) };
    private readonly Entry filename = new() { Text = "Student-Performance-results.xml", Placeholder = "Назва нового XML" };
    private readonly Label message = new() { Text = "Імпортуйте Desktop OAuth JSON, потім увійдіть через Google.", LineBreakMode = LineBreakMode.WordWrap };
    private readonly ActivityIndicator progress = new() { Color = Color.FromArgb("9DB8FF") };
    private readonly List<Button> actions = [];
    private CancellationTokenSource? operation;

    public GoogleDrivePage(GoogleDriveAuth auth, GoogleDriveClient drive, PerformanceService service,
        IReadOnlyList<GradeRecord> rows, Func<string, Task> openXml)
    {
        this.auth = auth; this.drive = drive; this.service = service;
        this.rows = rows.ToArray(); this.openXml = openXml;
        Title = "Google Drive"; BackgroundColor = Color.FromArgb("10141D");
        var layout = new VerticalStackLayout { Padding = 28, Spacing = 14, MaximumWidthRequest = 850, HorizontalOptions = LayoutOptions.Fill };
        layout.Add(new Label { Text = "Google Drive", FontSize = 28, FontAttributes = FontAttributes.Bold });
        layout.Add(new Label { Text = "Прямий доступ через Google Drive API. Вхід відкриється у браузері; надайте дозвіл на читання файлів і створення результатів." });
        var controls = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        AddButton(controls, "Імпорт OAuth JSON", Import);
        AddButton(controls, "Увійти / оновити список", Refresh);
        AddButton(controls, "Вийти", SignOut);
        layout.Add(controls); layout.Add(files);
        var open = new Button { Text = "Відкрити вибраний XML" };
        actions.Add(open); open.Clicked += async (_, _) => await Run(Open); layout.Add(open);
        layout.Add(new Label { Text = $"Зберегти поточну вибірку: {this.rows.Count} записів. Буде створено новий файл у корені My Drive." });
        layout.Add(filename);
        var save = new Button { Text = "Створити XML у Google Drive" };
        actions.Add(save); save.Clicked += async (_, _) => await Run(Upload); layout.Add(save);
        layout.Add(progress); layout.Add(message);
        var cancel = new Button { Text = "Скасувати операцію" };
        cancel.Clicked += (_, _) => operation?.Cancel(); layout.Add(cancel);
        var close = new Button { Text = "Назад" };
        close.Clicked += async (_, _) => { operation?.Cancel(); await Navigation.PopModalAsync(); };
        layout.Add(close);
        Content = new ScrollView { Content = layout };
    }

    private void AddButton(FlexLayout parent, string title, Func<CancellationToken, Task> action)
    {
        var button = new Button { Text = title, Margin = new Thickness(0, 0, 8, 8) };
        actions.Add(button); button.Clicked += async (_, _) => await Run(action); parent.Children.Add(button);
    }

    private async Task Run(Func<CancellationToken, Task> action)
    {
        if (operation is not null) return;
        using var cancellation = new CancellationTokenSource(); operation = cancellation;
        foreach (var button in actions) button.IsEnabled = false;
        files.IsEnabled = filename.IsEnabled = false;
        progress.IsRunning = true; message.Text = "Виконується… Якщо відкрився браузер, завершіть вхід Google.";
        try { await action(cancellation.Token); }
        catch (OperationCanceledException) { message.Text = "Операцію скасовано або час очікування входу минув. Можна спробувати знову."; }
        catch (Exception ex) { message.Text = ex.Message; }
        finally
        {
            operation = null; progress.IsRunning = false;
            foreach (var button in actions) button.IsEnabled = true;
            files.IsEnabled = filename.IsEnabled = true;
        }
    }

    private async Task Import(CancellationToken token)
    {
        var selected = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "OAuth client JSON — Desktop app" });
        if (selected is null) { message.Text = "Імпорт скасовано."; return; }
        using var stream = await selected.OpenReadAsync();
        using var reader = new StreamReader(stream);
        await auth.ImportClientAsync(await reader.ReadToEndAsync(token));
        files.ItemsSource = null;
        message.Text = "Налаштування імпортовано локально. Натисніть «Увійти / оновити список».";
    }

    private async Task Refresh(CancellationToken token)
    {
        var found = await drive.ListXmlAsync(token);
        files.ItemsSource = found.ToList(); files.SelectedIndex = found.Count > 0 ? 0 : -1;
        message.Text = found.Count == 0 ? "XML-файлів не знайдено. Можна зберегти поточну вибірку або додати XML через сайт Google Drive й оновити список."
            : $"XML-файлів: {found.Count}. Оберіть файл зі списку.";
    }

    private async Task Open(CancellationToken token)
    {
        if (files.SelectedItem is not DriveXmlFile file) throw new InvalidOperationException("Спочатку оновіть список та оберіть XML.");
        var temporary = Path.Combine(FileSystem.CacheDirectory, "drive-" + Guid.NewGuid() + ".xml");
        try { await drive.DownloadAsync(file, temporary, token); token.ThrowIfCancellationRequested(); await openXml(temporary); }
        catch { if (File.Exists(temporary)) File.Delete(temporary); throw; }
        message.Text = $"Відкрито: {file.Name}. Поверніться до результатів кнопкою «Назад».";
    }

    private async Task Upload(CancellationToken token)
    {
        var name = filename.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Введіть назву XML-файла.");
        if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) name += ".xml";
        // Authenticate before writing the temporary selection.
        await auth.GetTokenAsync(token);
        var temporary = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid() + ".xml");
        try
        {
            service.Save(temporary, rows);
            await drive.UploadAsync(name, await File.ReadAllTextAsync(temporary, token), token);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        message.Text = $"Створено у Google Drive: {name}. Оновіть список, щоб відкрити новий файл.";
    }

    private async Task SignOut(CancellationToken token)
    {
        await auth.SignOutAsync(); files.ItemsSource = null;
        message.Text = "Локальну сесію видалено. Щоб відкликати доступ застосунку, використайте налаштування Google-акаунта.";
    }

    protected override void OnDisappearing() { operation?.Cancel(); base.OnDisappearing(); }
}
