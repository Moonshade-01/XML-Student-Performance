namespace StudentPerformance;

public interface IDocumentPicker
{
    Task<string?> OpenAsync(string extension);
    Task<string?> SaveAsync(string extension, string suggestedName);
}
