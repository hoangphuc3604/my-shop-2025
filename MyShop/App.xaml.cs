using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShop.Contracts;
using MyShop.Services;
using MyShop.Services.GraphQL;
using MyShop.Views.Windows;
using MyShop.ViewModels;
using System.Diagnostics;

namespace MyShop;

public partial class App : Application
{
    private Window? _window;
    private IServiceProvider? _serviceProvider;
    private INavigationService? _navigationService;

    public static IServiceProvider Services => ((App)Current)._serviceProvider!;
    public static Window? MainWindow => ((App)Current)._window;

    public App()
    {
        InitializeComponent();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // Config Service must be registered first to get server address
            services.AddSingleton<IConfigService, ConfigService>();

            // Build temporary provider to get ConfigService
            var tempProvider = services.BuildServiceProvider();
            var configService = tempProvider.GetService<IConfigService>();
            var graphqlEndpoint = configService?.GetServerAddress() ?? configuration["GraphQL:Endpoint"] ?? "http://localhost:4000";
            
            Debug.WriteLine($"[APP] Using GraphQL endpoint: {graphqlEndpoint}");

            // GraphQL Client and API Services
            services.AddSingleton(new GraphQLClient(graphqlEndpoint));
            services.AddSingleton<IOrderService, OrderService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<IAccountService, AuthenticationService>();
            services.AddSingleton<IPromotionService, PromotionService>();
            services.AddSingleton<IReportService, ReportService>();
            services.AddSingleton<IDashboardService, DashboardService>();

            // UI Services
            services.AddSingleton<DashboardUIService>();
            services.AddSingleton<OnboardingService>();

            // Navigation and Session Services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();


            //Trial License Service
            services.AddSingleton<ITrialLicenseService, TrialLicenseService>();
            services.AddTransient<TrialExpiredWindow>();

            // ViewModels and Views
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ProductViewModel>();
            services.AddTransient<OrderViewModel>();
            services.AddTransient<PromotionViewModel>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<MainViewModel>();

            // Order ViewModels
            services.AddTransient<AddOrderViewModel>();
            services.AddTransient<EditOrderViewModel>();
            services.AddTransient<OrderDetailsViewModel>();

            // Trial ViewModel
            services.AddTransient<TrialExpiredViewModel>();

            // Dashboard and Report ViewModels
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ReportViewModel>();

            // Export Service
            services.AddSingleton<OrderExportService>();

            _serviceProvider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Configuration error: {ex.Message}");
        }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            if (_serviceProvider != null)
            {
                // ⭐ CHECK TRIAL STATUS FIRST
                var trialService = _serviceProvider.GetService<ITrialLicenseService>();
                
                // 🆕 UNCOMMENT THIS LINE TO TEST EXPIRED TRIAL
                /*trialService!.SetTrialExpirationToNow();*/
                
                var trialStatus = trialService!.GetTrialStatus();
                var remainingDays = trialService.GetRemainingTrialDays();
                Debug.WriteLine($"[APP] Trial Status: {trialStatus}, Remaining: {remainingDays} days");

                // If trial expired and not activated, show activation window
                if (trialStatus == TrialStatus.Expired)
                {
                    Debug.WriteLine("[APP] Trial expired - showing activation window");
                    _window = _serviceProvider.GetService<TrialExpiredWindow>();
                    _window?.Activate();
                    return;
                }

                // Show trial warning if few days left
                if (trialStatus == TrialStatus.Active && remainingDays <= 3)
                {
                    Debug.WriteLine($"[APP] ⚠ Only {remainingDays} days left in trial");
                }

                // Normal login flow
                var graphqlEndpoint = _serviceProvider.GetService<IConfiguration>()?["GraphQL:Endpoint"] 
                    ?? "http://localhost:4000";
                Debug.WriteLine("[APP] Testing GraphQL connection...");
                var isConnected = await GraphQLConnectionTest.TestConnectionAsync(graphqlEndpoint);
                
                if (!isConnected)
                {
                    Debug.WriteLine("[APP] WARNING: Cannot connect to GraphQL backend!");
                }

                _navigationService = _serviceProvider.GetService<INavigationService>();
                _navigationService!.NavigationRequested += OnNavigationRequested;

                var sessionService = _serviceProvider.GetService<ISessionService>();
                
                if (sessionService!.HasValidSession() && sessionService.HasValidToken())
                {
                    Debug.WriteLine("[APP] ✓ Valid session and token found, loading main window");
                    _window = _serviceProvider.GetService<MainWindow>();
                }
                else
                {
                    Debug.WriteLine("[APP] No valid session, showing login");
                    _window = _serviceProvider.GetService<LoginWindow>();
                }
                _window?.Activate();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[APP] Launch error: {ex.Message}");
        }
    }

    private void OnNavigationRequested(object? sender, NavigationEventArgs e)
    {
        switch (e.Target)
        {
            case NavigationTarget.Main:
                NavigateToMain();
                break;
            case NavigationTarget.Login:
                NavigateToLogin();
                break;
            case NavigationTarget.Config:
                NavigateToConfig();
                break;
        }
    }

    private void NavigateToMain()
    {
        var mainWindow = _serviceProvider?.GetService<MainWindow>();
        mainWindow?.Activate();
        _window?.Close();
        _window = mainWindow;
    }

    private void NavigateToLogin()
    {
        var loginWindow = _serviceProvider?.GetService<LoginWindow>();
        loginWindow?.Activate();
        _window?.Close();
        _window = loginWindow;
    }

    private void NavigateToConfig()
    {
        Debug.WriteLine("[APP] Navigating to Config");
        // Create a new window to host ConfigPage
        var configWindow = new Window();
        configWindow.Title = "MyShop - Configuration";
        configWindow.Content = new MyShop.Views.Pages.ConfigPage();
        configWindow.Activate();
        
        // Close login window when config opens
        _window?.Close();
        _window = configWindow;
    }
}
