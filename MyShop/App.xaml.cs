using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShop.Contracts;
using MyShop.Services;
using MyShop.Views.Windows;
using MyShop.ViewModels;

namespace MyShop;

public partial class App : Application
{
    private Window? _window;
    private IServiceProvider? _serviceProvider;
    private INavigationService? _navigationService;

    public static IServiceProvider Services => ((App)Current)._serviceProvider!;

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
            services.AddDatabaseServices(configuration);

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginWindow>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainViewModel>();

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
                await DatabaseSeeder.SeedDemoDataAsync(_serviceProvider);

                _navigationService = _serviceProvider.GetService<INavigationService>();
                _navigationService!.NavigationRequested += OnNavigationRequested;

                _window = _serviceProvider.GetService<LoginWindow>();
                _window?.Activate();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Launch error: {ex.Message}");
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
}
