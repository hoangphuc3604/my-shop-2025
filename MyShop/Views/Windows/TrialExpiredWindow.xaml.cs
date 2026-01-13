using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;
using MyShop.Services;

namespace MyShop.Views.Windows
{
    public sealed partial class TrialExpiredWindow : Window
    {
        private readonly ITrialLicenseService _trialService;
        private readonly INavigationService _navigationService;

        public TrialExpiredWindow(ITrialLicenseService trialService, INavigationService navigationService)
        {
            this.InitializeComponent();
            _trialService = trialService;
            _navigationService = navigationService;
        }

        private async void OnActivateClicked(object sender, RoutedEventArgs e)
        {
            var code = ActivationCodeBox.Text;

            if (string.IsNullOrWhiteSpace(code))
            {
                await ShowMessage("Please enter an activation code");
                return;
            }

            if (_trialService.ActivateWithCode(code))
            {
                await ShowMessage("✓ License activated successfully! Please restart the application.");
                this.Close();
            }
            else
            {
                await ShowMessage("✗ Invalid activation code. Please check and try again.");
                ActivationCodeBox.Text = string.Empty;
                ActivationCodeBox.Focus(FocusState.Programmatic);
            }
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private async Task ShowMessage(string message)
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