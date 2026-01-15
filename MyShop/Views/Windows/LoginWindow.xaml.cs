using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;
using MyShop.Contracts;

namespace MyShop.Views.Windows;

public sealed partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }
    private readonly INavigationService? _navigationService;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _navigationService = App.Services.GetService(typeof(INavigationService)) as INavigationService;
        
        if (Content is FrameworkElement root)
        {
            root.DataContext = ViewModel;
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            ViewModel.Password = passwordBox.Password;
            System.Diagnostics.Debug.WriteLine($"PasswordBox changed. Username='{ViewModel.Username}', PasswordLength={passwordBox.Password?.Length}");
        }
    }

    private void OnConfigClick(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[LOGIN] Opening Config screen");
        _navigationService?.NavigateTo(NavigationTarget.Config);
    }
}
