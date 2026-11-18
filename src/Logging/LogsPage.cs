namespace StudentPerformance.Logging;

public sealed class LogsPage : ContentPage
{
    private readonly AppLogger logger;
    private readonly IDocumentPicker picker;
    private readonly Picker level = new() { Title = "Рівень подій" };
    private readonly Editor entries = new() { IsReadOnly = true, FontFamily = "Consolas", FontSize = 13,
        TextColor = Color.FromArgb("EEF2FA"), BackgroundColor = Color.FromArgb("191F2B"), MinimumHeightRequest = 250 };
    private readonly Label status = new() { FontSize = 12 };
    private bool busy;

    public LogsPage(AppLogger logger, IDocumentPicker picker)
    {
        this.logger = logger; this.picker = picker;
        Title = "Логи"; BackgroundColor = Color.FromArgb("10141D");
        level.ItemsSource = new[] { "Усі події", "Зміни", "Перехід", "Помилка" }; level.SelectedIndex = 0;
        level.SelectedIndexChanged += async (_, _) => await Refresh();
        var refresh = new Button { Text = "Оновити" };
        refresh.Clicked += async (_, _) => await Refresh();
        var export = new Button { Text = "Зберегти журнал…" };
        export.Clicked += async (_, _) => await Export();
        var back = new Button { Text = "Назад" };
        back.Clicked += async (_, _) => { logger.Navigation("Log viewer closed."); await Navigation.PopModalAsync(); };
        var buttons = new HorizontalStackLayout { Spacing = 10 };
        buttons.Add(refresh); buttons.Add(export); buttons.Add(back);
        var layout = new Grid { Padding = 24, RowSpacing = 12,
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)] };
        layout.Add(new Label { Text = "Журнал подій", FontSize = 24, FontAttributes = FontAttributes.Bold }, 0, 0);
        var controls = new VerticalStackLayout { Spacing = 8 }; controls.Add(level); controls.Add(buttons);
        layout.Add(controls, 0, 1); layout.Add(entries, 0, 2); layout.Add(status, 0, 3);
        Content = layout;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await Refresh(); }
    private async Task Refresh()
    {
        if (busy) return;
        busy = true;
        try
        {
            var filter = level.SelectedIndex > 0 ? (LogLevel?)(level.SelectedIndex - 1) : null;
            var items = await Task.Run(() => logger.Read(filter));
            entries.Text = items.Count == 0 ? "Подій поки немає." : string.Join(Environment.NewLine, items.Select(AppLogger.Format));
            status.Text = $"Останні {items.Count} подій, новіші зверху. Файл: {logger.FilePath}";
            if (logger.StorageError is not null) status.Text += "\nФайл журналу недоступний; показано події поточного сеансу.";
        }
        finally { busy = false; }
    }

    private async Task Export()
    {
        if (busy) return;
        busy = true;
        try
        {
            var path = await picker.SaveAsync(".txt", "Student-Performance-logs");
            if (path is null) return;
            if (Path.GetFullPath(path).Equals(Path.GetFullPath(logger.FilePath), StringComparison.OrdinalIgnoreCase)
                || Path.GetFullPath(path).Equals(Path.GetFullPath(logger.FilePath + ".previous"), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Оберіть інший файл для копії журналу.");
            var filter = level.SelectedIndex > 0 ? (LogLevel?)(level.SelectedIndex - 1) : null;
            var text = await Task.Run(() => string.Join(Environment.NewLine, logger.Read(filter).Select(AppLogger.Format)));
            await File.WriteAllTextAsync(path, text);
            logger.Change("Log selection exported as text."); status.Text = "Журнал збережено.";
        }
        catch (Exception ex) { logger.Error("Log export failed", ex); status.Text = "Не вдалося зберегти журнал: " + ex.Message; }
        finally { busy = false; }
    }
}
