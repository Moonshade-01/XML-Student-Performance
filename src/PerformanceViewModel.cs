using System.ComponentModel;
using System.Runtime.CompilerServices;
using StudentPerformance.Core;

namespace StudentPerformance;

public sealed class PerformanceViewModel : INotifyPropertyChanged
{
    private readonly PerformanceService service;
    private readonly IDocumentPicker picker;
    private string? cloudSource;
    private string source = "", xslPath = "";
    private bool initialized, busy;
    private IReadOnlyList<GradeRecord> rows = [];
    private string status = "Відкрийте XML, щоб завантажити дані.", error = "";
    private IXmlAnalysisStrategy selected;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public IXmlAnalysisStrategy[] Strategies { get; } = [new SaxAnalysisStrategy(), new DomAnalysisStrategy(), new LinqAnalysisStrategy()];
    public IXmlAnalysisStrategy SelectedStrategy { get => selected; set { selected = value; Changed(); } }
    public string Keywords { get; set; } = "";
    public string Faculty { get; set; } = "";
    public string Department { get; set; } = "";
    public string Group { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string Semester { get; set; } = "";
    public string MinScore { get; set; } = "";
    public string MaxScore { get; set; } = "";
    public IReadOnlyList<GradeRecord> Rows { get => rows; private set { rows = value; Changed(); Changed(nameof(ResultCount)); Changed(nameof(Average)); } }
    public string ResultCount => CountLabel(Rows.Count);
    private static string CountLabel(int count)
    {
        var noun = count % 100 is >= 11 and <= 14 ? "записів"
            : count % 10 == 1 ? "запис" : count % 10 is >= 2 and <= 4 ? "записи" : "записів";
        return $"{count} {noun}";
    }
    public string Average => Rows.Count == 0 ? "—" : Rows.Average(x => x.Score).ToString("0.0");
    public string Source => source;
    public string Status { get => status; private set { status = value; Changed(); } }
    public string Error { get => error; private set { error = value; Changed(); Changed(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool IsBusy { get => busy; private set { busy = value; Changed(); foreach (var c in commands) c.Refresh(); } }
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand OpenCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand ExportCommand { get; }
    public AsyncCommand ResetCommand { get; }
    private readonly List<AsyncCommand> commands = [];

    public PerformanceViewModel(PerformanceService service, IDocumentPicker picker)
    {
        this.service = service; this.picker = picker; selected = Strategies[0];
        AsyncCommand Make(Func<Task> action) { var c = new AsyncCommand(() => Run(action), () => !IsBusy && initialized); commands.Add(c); return c; }
        SearchCommand = Make(Search); OpenCommand = Make(Open); SaveCommand = Make(Save); ExportCommand = Make(Export);
        ResetCommand = Make(async () => {
            Keywords = Faculty = Department = Group = Discipline = Semester = MinScore = MaxScore = "";
            foreach (var key in new[] { nameof(Keywords), nameof(Faculty), nameof(Department), nameof(Group), nameof(Discipline), nameof(Semester), nameof(MinScore), nameof(MaxScore) }) Changed(key);
            await Search();
        });
    }
    public async Task InitializeAsync()
    {
        if (initialized) return;
        await Run(async () => {
            xslPath = Path.Combine(FileSystem.CacheDirectory, "Student-Performance.xsl");
            using (var input = await FileSystem.OpenAppPackageFileAsync("Student-Performance.xsl"))
            using (var output = File.Create(xslPath))
                await input.CopyToAsync(output);
            initialized = true;
        });
    }
    private async Task Run(Func<Task> action)
    {
        IsBusy = true; Error = "";
        try { await action(); }
        catch (Exception ex) { Error = "Не вдалося виконати дію: " + ex.Message; }
        finally { IsBusy = false; }
    }
    private static int? Number(string value) => string.IsNullOrWhiteSpace(value) ? null : int.Parse(value);
    private async Task Search()
    {
        if (string.IsNullOrEmpty(source))
        {
            Status = "Відкрийте XML, щоб завантажити дані.";
            return;
        }
        var criteria = new SearchCriteria(Keywords, Faculty, Department, Group, Discipline, Number(Semester), Number(MinScore), Number(MaxScore));
        service.Strategy = SelectedStrategy;
        Rows = await Task.Run(() => service.Search(source, criteria));
        Status = $"{SelectedStrategy.Name} • {ResultCount} у вибірці";
    }
    private async Task Open()
    {
        var file = await picker.OpenAsync(".xml"); if (file is null) return;
        var previous = source; source = file;
        try { await Search(); Changed(nameof(Source)); ClearCloudSource(); }
        catch { source = previous; throw; }
    }
    public async Task OpenCloudXmlAsync(string path)
    {
        var previous = source; source = path;
        try { await Search(); Changed(nameof(Source)); }
        catch { source = previous; throw; }
        ClearCloudSource(); cloudSource = path;
    }
    private void ClearCloudSource()
    {
        if (cloudSource is not null)
        {
            try { File.Delete(cloudSource); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            cloudSource = null;
        }
    }
    private async Task Save()
    {
        var file = await picker.SaveAsync(".xml", "Student-Performance-results"); if (file is null) return;
        await Task.Run(() => service.Save(file, Rows)); Status = $"XML збережено: {file}";
    }
    private async Task Export()
    {
        // XSL-шаблон завантажено з вбудованих ресурсів застосунку.
        var file = await picker.SaveAsync(".html", "Student-Performance"); if (file is null) return;
        var temporary = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid() + ".xml");
        try { await Task.Run(() => { service.Save(temporary, Rows); service.ExportHtml(temporary, xslPath, file); }); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        Status = $"HTML збережено: {file}";
    }
}
