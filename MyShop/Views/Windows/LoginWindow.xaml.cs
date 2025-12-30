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
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            ViewModel.Password = passwordBox.Password;
            System.Diagnostics.Debug.WriteLine($"PasswordBox changed. Username='{ViewModel.Username}', PasswordLength={passwordBox.Password?.Length}");
        }
    }
}
