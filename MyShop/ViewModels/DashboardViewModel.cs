using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IDashboardService _dashboardService;
        private readonly ISessionService _sessionService;

        private bool _isLoading;
        private DashboardStatsData _stats;
        private string _totalProducts = "0";
        private string _todayOrders = "0";
        private string _todayRevenue = "0 ₫";

        public event PropertyChangedEventHandler? PropertyChanged;

        public DashboardViewModel(IDashboardService dashboardService, ISessionService sessionService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public DashboardStatsData Stats
        {
            get => _stats;
            set
            {
                if (_stats != value)
                {
                    _stats = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TotalProducts
        {
            get => _totalProducts;
            set
            {
                if (_totalProducts != value)
                {
                    _totalProducts = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TodayOrders
        {
            get => _todayOrders;
            set
            {
                if (_todayOrders != value)
                {
                    _todayOrders = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TodayRevenue
        {
            get => _todayRevenue;
            set
            {
                if (_todayRevenue != value)
                {
                    _todayRevenue = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task LoadDashboardAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var token = _sessionService.GetAuthToken();
                Stats = await _dashboardService.LoadDashboardStatsAsync(token);

                TotalProducts = Stats.TotalProducts.ToString();
                TodayOrders = Stats.TodayOrdersCount.ToString();
                TodayRevenue = $"{Stats.TodayRevenue:#,0} ₫";

                Debug.WriteLine("[DASHBOARD_VM] ✓ Dashboard loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD_VM] ✗ Error loading dashboard: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}