using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class ReportViewModel : INotifyPropertyChanged
    {
        private readonly IReportService _reportService;
        private readonly IProductService _productService;
        private readonly ISessionService _sessionService;

        private bool _isLoading;
        private RevenueReport _currentReport;
        private List<Product> _allProducts = new();
        private int _selectedProductId = -1;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _selectedTimePeriod = "Day";
        private string _totalRevenue = "0 ₫";
        private string _totalOrders = "0";
        private string _averageOrderValue = "0.00 ₫";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReportViewModel(
            IReportService reportService,
            IProductService productService,
            ISessionService sessionService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
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

        public RevenueReport CurrentReport
        {
            get => _currentReport;
            set
            {
                if (_currentReport != value)
                {
                    _currentReport = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Product> AllProducts
        {
            get => _allProducts;
            set
            {
                if (_allProducts != value)
                {
                    _allProducts = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedProductId
        {
            get => _selectedProductId;
            set
            {
                if (_selectedProductId != value)
                {
                    _selectedProductId = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate != value)
                {
                    _toDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedTimePeriod
        {
            get => _selectedTimePeriod;
            set
            {
                if (_selectedTimePeriod != value)
                {
                    _selectedTimePeriod = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                if (_totalRevenue != value)
                {
                    _totalRevenue = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TotalOrders
        {
            get => _totalOrders;
            set
            {
                if (_totalOrders != value)
                {
                    _totalOrders = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AverageOrderValue
        {
            get => _averageOrderValue;
            set
            {
                if (_averageOrderValue != value)
                {
                    _averageOrderValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task LoadProductsAsync()
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                AllProducts = await _productService.GetProductsAsync(1, 1000, null, null, null, null, null, token);
                Debug.WriteLine($"[REPORT_VM] ✓ Loaded {AllProducts.Count} products");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT_VM] ✗ Error loading products: {ex.Message}");
                throw;
            }
        }

        public async Task GenerateReportAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var toDateAdjusted = ToDate?.AddDays(1);
                var token = _sessionService.GetAuthToken();

                Debug.WriteLine($"[REPORT_VM] Generating report - From: {FromDate}, To: {toDateAdjusted}");

                CurrentReport = await _reportService.GenerateRevenueReportAsync(FromDate, toDateAdjusted, token);

                TotalRevenue = $"{CurrentReport.TotalRevenue:#,0} ₫";
                TotalOrders = CurrentReport.TotalOrders.ToString();
                AverageOrderValue = $"{CurrentReport.AverageOrderValue:#,0.00} ₫";

                Debug.WriteLine("[REPORT_VM] ✓ Report generated successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT_VM] ✗ Error generating report: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public int GetProductQuantity(DailyRevenue daily, int productId)
        {
            if (productId < 0)
                return daily.TotalQuantity;
            var productQuantity = daily.ProductQuantities.FirstOrDefault(pq => pq.ProductId == productId);
            return productQuantity?.Quantity ?? 0;
        }

        public int GetProductQuantity(WeeklyRevenue weekly, int productId)
        {
            if (productId < 0)
                return weekly.TotalQuantity;
            var productQuantity = weekly.ProductQuantities.FirstOrDefault(pq => pq.ProductId == productId);
            return productQuantity?.Quantity ?? 0;
        }

        public int GetProductQuantity(MonthlyRevenue monthly, int productId)
        {
            if (productId < 0)
                return monthly.TotalQuantity;
            var productQuantity = monthly.ProductQuantities.FirstOrDefault(pq => pq.ProductId == productId);
            return productQuantity?.Quantity ?? 0;
        }

        public int GetProductQuantity(YearlyRevenue yearly, int productId)
        {
            if (productId < 0)
                return yearly.TotalQuantity;
            var productQuantity = yearly.ProductQuantities.FirstOrDefault(pq => pq.ProductId == productId);
            return productQuantity?.Quantity ?? 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}