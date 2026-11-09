namespace StudentPerformance;

public partial class App : Application
{
    private readonly IServiceProvider services;
    public App(IServiceProvider services)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        this.services = services;
    }
    protected override Window CreateWindow(IActivationState? activationState) => new(services.GetRequiredService<MainPage>())
    { Title = "Student-Performance • Lab 2, Variant 6", Width = 1280, Height = 860, MinimumWidth = 1050, MinimumHeight = 650 };
}
