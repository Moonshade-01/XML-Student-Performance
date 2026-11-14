using Microsoft.Extensions.DependencyInjection;
using StudentPerformance.Core;
using StudentPerformance.Cloud;

namespace StudentPerformance;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton<QueryBuilder>();
        builder.Services.AddSingleton<XmlFileStore>();
        builder.Services.AddSingleton<HtmlTransformService>();
        builder.Services.AddSingleton<PerformanceService>();
        builder.Services.AddSingleton<GoogleDriveAuth>();
        builder.Services.AddSingleton<GoogleDriveClient>();
        builder.Services.AddSingleton<IDocumentPicker, WindowsDocumentPicker>();
        builder.Services.AddSingleton<PerformanceViewModel>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
