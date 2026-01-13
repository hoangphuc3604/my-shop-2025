using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private readonly IDashboardService _dashboardService;
        private readonly ISessionService _sessionService;
        private readonly DashboardUIService _uiService;
        private bool _isLoading = false;
        private DashboardStatsData _currentStats;

        public DashboardPage()
        {
            this.InitializeComponent();
            _dashboardService = (App.Services.GetService(typeof(IDashboardService)) as IDashboardService)!;
            _sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _uiService = new DashboardUIService();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += DashboardPage_SizeChanged;
            await LoadDashboardAsync();
        }

        private void DashboardPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
            
            // Redraw chart when size changes
            if (_currentStats != null)
            {
                _uiService.DrawMonthlyRevenueChart(MonthlyRevenueCanvas, NoDataMessageRevenue, _currentStats);
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

                // Update padding for better spacing
                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadDashboardAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            LoadingProgressRing.IsActive = true;

            try
            {
                var token = _sessionService?.GetAuthToken();
                _currentStats = await _dashboardService.LoadDashboardStatsAsync(token);

                _uiService.UpdateSummaryCards(TotalProductsText, TodayRevenueText, TodayOrdersText, _currentStats);
                _uiService.UpdateTopSellingProducts(TopSellingProductsList, _currentStats);
                _uiService.UpdateLowStockProducts(LowStockProductsList, _currentStats);
                _uiService.UpdateRecentOrders(RecentOrdersList, _currentStats);
                _uiService.DrawMonthlyRevenueChart(MonthlyRevenueCanvas, NoDataMessageRevenue, _currentStats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] ✗ Error loading dashboard: {ex.Message}");
                await ShowErrorAsync($"Failed to load dashboard: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                LoadingProgressRing.IsActive = false;
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "Close", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }
    }
}
