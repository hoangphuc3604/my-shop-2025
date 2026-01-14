using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using MyShop.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderDetailsPage : Page
    {
        private OrderDetailsViewModel _viewModel;

        public OrderDetailsPage()
        {
            this.InitializeComponent();
            
            var orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            var sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            var exportService = new OrderExportService();
            _viewModel = new OrderDetailsViewModel(orderService, sessionService, exportService);
            
            DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += OrderDetailsPage_SizeChanged;

            if (e.Parameter is Order order)
            {
                await LoadOrderAsync(order);
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= OrderDetailsPage_SizeChanged;
        }

        private void OrderDetailsPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
        }

        private void ApplyResponsiveLayout(double width, double height)
        {
            try
            {
                var viewportSize = ResponsiveService.GetCurrentViewportSize(width, height);
                var isCompact = ResponsiveService.IsCompactLayout(width);
                var padding = ResponsiveService.GetOptimalPadding(width);

                Debug.WriteLine($"[ORDER_DETAILS] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadOrderAsync(Order order)
        {
            try
            {
                await _viewModel.LoadOrderDetailsAsync(order);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load order details: {ex.Message}");
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void OnExportToPdfClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.ExportToFormatAsync("pdf", this.XamlRoot);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to export to PDF: {ex.Message}");
            }
        }

        private async void OnExportToXpsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.ExportToFormatAsync("xps", this.XamlRoot);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to export to XPS: {ex.Message}");
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}