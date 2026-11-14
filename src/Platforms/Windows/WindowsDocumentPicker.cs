using Windows.Storage.Pickers;

namespace StudentPerformance;

// Системні діалоги підтримують локальні шляхи й змонтовану папку Google Drive.
public sealed class WindowsDocumentPicker : IDocumentPicker
{
    private static nint WindowHandle()
    {
        var window = (Microsoft.UI.Xaml.Window)Application.Current!.Windows[0].Handler!.PlatformView!;
        return WinRT.Interop.WindowNative.GetWindowHandle(window);
    }
    public async Task<string?> OpenAsync(string extension)
    {
        var picker = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHandle());
        picker.FileTypeFilter.Add(extension);
        return (await picker.PickSingleFileAsync())?.Path;
    }
    public async Task<string?> SaveAsync(string extension, string suggestedName)
    {
        var picker = new FileSavePicker { SuggestedFileName = suggestedName };
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHandle());
        picker.FileTypeChoices.Add(extension.ToUpperInvariant() + " документ", [extension]);
        return (await picker.PickSaveFileAsync())?.Path;
    }
}
