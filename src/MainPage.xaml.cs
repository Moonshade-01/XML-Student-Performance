using StudentPerformance.Cloud;
using StudentPerformance.Core;

namespace StudentPerformance;

public partial class MainPage : ContentPage
{
    private readonly PerformanceViewModel viewModel;
    private readonly GoogleDriveAuth driveAuth;
    private readonly GoogleDriveClient drive;
    private readonly PerformanceService service;
    public MainPage(PerformanceViewModel viewModel, GoogleDriveAuth driveAuth, GoogleDriveClient drive, PerformanceService service)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        this.driveAuth = driveAuth; this.drive = drive; this.service = service;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }
    private async void OnGoogleDriveClicked(object? sender, EventArgs e)
    {
        if (viewModel.IsBusy) return;
        await Navigation.PushModalAsync(new GoogleDrivePage(driveAuth, drive, service, viewModel.Rows, viewModel.OpenCloudXmlAsync));
    }
}
