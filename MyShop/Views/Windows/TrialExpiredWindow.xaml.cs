using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;
using MyShop.Services;
using MyShop.ViewModels;

namespace MyShop.Views.Windows
{
    public sealed partial class TrialExpiredWindow : Window
    {
        private readonly TrialExpiredViewModel _viewModel;

        public TrialExpiredWindow(ITrialLicenseService trialService)
        {
            this.InitializeComponent();
            _viewModel = new TrialExpiredViewModel(trialService);
            _viewModel.ActivationSucceeded += OnActivationSucceeded;
            _viewModel.ActivationFailed += OnActivationFailed;

            // Set DataContext on the root element of the XAML, not on Content (UIElement)
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }
        }

        private async void OnActivateClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.ActivateWithCode();
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private async void OnActivationSucceeded(object? sender, EventArgs e)
        {
            await ShowMessage("✓ License activated successfully! Please restart the application.");
            this.Close();
        }

        private async void OnActivationFailed(object? sender, string message)
        {
            await ShowMessage(message);
        }

        private async System.Threading.Tasks.Task ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Activation",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}