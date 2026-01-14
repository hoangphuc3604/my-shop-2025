using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Services;
using MyShop.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private DashboardViewModel _viewModel;
        private readonly DashboardUIService _uiService;

        public DashboardPage()
        {
            this.InitializeComponent();
            
            var dashboardService = (App.Services.GetService(typeof(IDashboardService)) as IDashboardService)!;
            var sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _viewModel = new DashboardViewModel(dashboardService, sessionService);
            _uiService = new DashboardUIService();
            
            DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += DashboardPage_SizeChanged;
            await LoadDashboardAsync();
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= DashboardPage_SizeChanged;
        }

        private void DashboardPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
            
            // Redraw chart when size changes
            if (_viewModel.Stats != null)
            {
                _uiService.DrawMonthlyRevenueChart(MonthlyRevenueCanvas, NoDataMessageRevenue, _viewModel.Stats);
            }
        }

        private void ApplyResponsiveLayout(double width, double height)
        {
            try
            {
                var viewportSize = ResponsiveService.GetCurrentViewportSize(width, height);
                var isCompact = ResponsiveService.IsCompactLayout(width);
                var padding = ResponsiveService.GetOptimalPadding(width);

                Debug.WriteLine($"[DASHBOARD] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                await _viewModel.LoadDashboardAsync();
                
                // Update UI elements that can't be bound directly
                _uiService.UpdateTopSellingProducts(TopSellingProductsList, _viewModel.Stats);
                _uiService.UpdateLowStockProducts(LowStockProductsList, _viewModel.Stats);
                _uiService.UpdateRecentOrders(RecentOrdersList, _viewModel.Stats);
                _uiService.DrawMonthlyRevenueChart(MonthlyRevenueCanvas, NoDataMessageRevenue, _viewModel.Stats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] ✗ Error loading dashboard: {ex.Message}");
                await ShowErrorAsync($"Failed to load dashboard: {ex.Message}");
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
