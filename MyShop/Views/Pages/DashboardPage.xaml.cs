using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
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
            await LoadDashboardAsync();
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
                var dashboardStats = await _dashboardService.LoadDashboardStatsAsync(token);

                _uiService.UpdateSummaryCards(TotalProductsText, TodayRevenueText, TodayOrdersText, dashboardStats);
                _uiService.UpdateTopSellingProducts(TopSellingProductsList, dashboardStats);
                _uiService.UpdateLowStockProducts(LowStockProductsList, dashboardStats);
                _uiService.UpdateRecentOrders(RecentOrdersList, dashboardStats);
                _uiService.DrawMonthlyRevenueChart(MonthlyRevenueCanvas, NoDataMessageRevenue, dashboardStats);
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
