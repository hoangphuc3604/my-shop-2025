using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShop.Views.Windows;
using MyShop.ViewModels;

namespace MyShop;

public partial class App : Application
{
    private Window? _window;
    private IServiceProvider? _serviceProvider;

    public App()
    {
        InitializeComponent();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();
        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = _serviceProvider?.GetService<MainWindow>();
        _window?.Activate();
    }
}
