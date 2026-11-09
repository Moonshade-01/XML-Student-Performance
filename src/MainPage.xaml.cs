namespace StudentPerformance;

public partial class MainPage : ContentPage
{
    private readonly PerformanceViewModel viewModel;
    public MainPage(PerformanceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }
}
