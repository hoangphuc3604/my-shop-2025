using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;

namespace MyShop.Views.Windows;

public sealed partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        if (Content is FrameworkElement root)
        {
            root.DataContext = ViewModel;
        }
        ViewModel.LoginSuccessful += OnLoginSuccessful;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            ViewModel.Password = passwordBox.Password;
            System.Diagnostics.Debug.WriteLine($"PasswordBox changed. Username='{ViewModel.Username}', PasswordLength={passwordBox.Password?.Length}");
        }
    }

    private void OnLoginSuccessful(object? sender, EventArgs e)
    {
        var mainWindow = App.Services.GetService(typeof(MainWindow)) as MainWindow;
        mainWindow?.Activate();
        Close();
    }
}
