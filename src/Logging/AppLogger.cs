using System.Text;
using System.Text.Json;

namespace StudentPerformance.Logging;

public enum LogLevel { Change, Navigation, Error }
public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Message);

// Singleton: all application features write to the same journal instance.
public sealed class AppLogger
{
    private static readonly Lazy<AppLogger> instance = new(() => new AppLogger(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Student-Performance", "Logs")));
    public static AppLogger Instance => instance.Value;
    private readonly object gate = new();
    private readonly Queue<LogEntry> recent = new();
    public string FilePath { get; }
    public string? StorageError { get; private set; }
    private const long MaximumFileSize = 2 * 1024 * 1024;
    private const int MaximumVisibleEntries = 500;

    private AppLogger(string directory) => FilePath = Path.Combine(directory, "application.log");

    public void Write(LogLevel level, string message)
    {
        var entry = new LogEntry(DateTimeOffset.Now, level, message);
        lock (gate)
        {
            recent.Enqueue(entry);
            while (recent.Count > MaximumVisibleEntries) recent.Dequeue();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length >= MaximumFileSize)
                    File.Move(FilePath, FilePath + ".previous", overwrite: true);
                File.AppendAllText(FilePath, JsonSerializer.Serialize(entry) + Environment.NewLine, Encoding.UTF8);
                StorageError = null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Logging must not prevent the user from continuing their work.
                StorageError = ex.GetType().Name;
            }
        }
    }

    public void Change(string message) => Write(LogLevel.Change, message);
    public void Navigation(string message) => Write(LogLevel.Navigation, message);
    public void Error(string operation, Exception exception) => Write(LogLevel.Error,
        $"{operation}: {exception.GetType().Name}");

    public IReadOnlyList<LogEntry> Read(LogLevel? level = null)
    {
        lock (gate)
        {
            var entries = new List<LogEntry>();
            if (StorageError is null)
            {
                try
                {
                    foreach (var path in new[] { FilePath + ".previous", FilePath })
                    {
                        if (!File.Exists(path)) continue;
                        foreach (var line in File.ReadLines(path, Encoding.UTF8))
                        {
                            try
                            {
                                if (JsonSerializer.Deserialize<LogEntry>(line) is { } entry) entries.Add(entry);
                            }
                            catch (JsonException) { /* Ignore an incomplete line left by an interrupted write. */ }
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                { StorageError = ex.GetType().Name; }
            }
            if (StorageError is not null) entries = recent.ToList();
            return entries.Where(e => level is null || e.Level == level).TakeLast(MaximumVisibleEntries).Reverse().ToList();
        }
    }

    public static string Format(LogEntry entry)
    {
        var label = entry.Level switch { LogLevel.Change => "Зміни", LogLevel.Navigation => "Перехід", _ => "Помилка" };
        return $"{entry.Time:dd.MM.yyyy HH:mm:ss zzz} [{label}] {entry.Message}";
    }
}
