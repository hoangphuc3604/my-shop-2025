using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;
using System;
using System.Diagnostics;

namespace MyShop.Views.Pages
{
    public sealed partial class ConfigPage : Page
    {
        private readonly IConfigService? _configService;
        private readonly INavigationService? _navigationService;
        private bool _hasChanges = false;

        public ConfigPage()
        {
            this.InitializeComponent();
            
            _configService = App.Services.GetService(typeof(IConfigService)) as IConfigService;
            _navigationService = App.Services.GetService(typeof(INavigationService)) as INavigationService;

            LoadCurrentConfig();
        }

        private void LoadCurrentConfig()
        {
            if (_configService == null) return;

            var currentAddress = _configService.GetServerAddress();
            var defaultAddress = _configService.GetDefaultServerAddress();

            ServerAddressTextBox.Text = currentAddress;
            DefaultAddressRun.Text = defaultAddress;
            _hasChanges = false;

            Debug.WriteLine($"[CONFIG_PAGE] Loaded - Current: {currentAddress}, Default: {defaultAddress}");
        }

        private void OnServerAddressChanged(object sender, TextChangedEventArgs e)
        {
            _hasChanges = true;
            ConnectionStatusBar.IsOpen = false;
        }

        private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
        {
            if (_configService == null) return;

            var address = ServerAddressTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                ShowStatus("Please enter a server address.", InfoBarSeverity.Warning);
                return;
            }

            TestConnectionButton.IsEnabled = false;
            ShowStatus("Testing connection...", InfoBarSeverity.Informational);

            try
            {
                var isConnected = await _configService.TestConnectionAsync(address);

                if (isConnected)
                {
                    ShowStatus("✓ Connection successful!", InfoBarSeverity.Success);
                }
                else
                {
                    ShowStatus("✗ Could not connect to server. Please check the address.", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CONFIG_PAGE] Test connection error: {ex.Message}");
                ShowStatus($"✗ Error: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private void OnResetDefaultClick(object sender, RoutedEventArgs e)
        {
            if (_configService == null) return;

            ServerAddressTextBox.Text = _configService.GetDefaultServerAddress();
            _hasChanges = true;
            ShowStatus("Reset to default address.", InfoBarSeverity.Informational);
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (_configService == null) return;

            var address = ServerAddressTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                ShowStatus("Please enter a server address.", InfoBarSeverity.Warning);
                return;
            }

            // Save the address
            _configService.SetServerAddress(address);
            _hasChanges = false;

            ShowStatus("✓ Configuration saved! Please restart the app to apply changes.", InfoBarSeverity.Success);
            
            Debug.WriteLine($"[CONFIG_PAGE] Saved address: {address}");

            // Show restart dialog
            var dialog = new ContentDialog
            {
                Title = "Configuration Saved",
                Content = "The server address has been saved.\n\nWould you like to return to the login screen?",
                PrimaryButtonText = "Go to Login",
                CloseButtonText = "Stay Here",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                OnBackClick(sender, e);
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (_navigationService != null)
            {
                _navigationService.NavigateTo(NavigationTarget.Login);
            }
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            ConnectionStatusBar.Message = message;
            ConnectionStatusBar.Severity = severity;
            ConnectionStatusBar.IsOpen = true;
        }
    }
}
