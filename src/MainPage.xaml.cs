using StudentPerformance.Cloud;
using StudentPerformance.Core;
using StudentPerformance.Logging;

namespace StudentPerformance;

public partial class MainPage : ContentPage
{
    private readonly PerformanceViewModel viewModel;
    private readonly GoogleDriveAuth driveAuth;
    private readonly GoogleDriveClient drive;
    private readonly PerformanceService service;
    private readonly AppLogger logger;
    private readonly IDocumentPicker picker;
    public MainPage(PerformanceViewModel viewModel, GoogleDriveAuth driveAuth, GoogleDriveClient drive, PerformanceService service, AppLogger logger, IDocumentPicker picker)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        this.driveAuth = driveAuth; this.drive = drive; this.service = service;
        this.logger = logger; this.picker = picker;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }
    private async void OnGoogleDriveClicked(object? sender, EventArgs e)
    {
        if (viewModel.IsBusy) return;
        logger.Navigation("Google Drive page opened.");
        await Navigation.PushModalAsync(new GoogleDrivePage(driveAuth, drive, service, () => viewModel.Rows, viewModel.OpenCloudXmlAsync, logger));
    }
    private async void OnLogsClicked(object? sender, EventArgs e)
    {
        if (viewModel.IsBusy || Navigation.ModalStack.Count > 0) return;
        logger.Navigation("Log viewer opened.");
        await Navigation.PushModalAsync(new LogsPage(logger, picker));
    }
}
