using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class OrderViewModel : INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private readonly ISessionService _sessionService;
        private readonly IAuthorizationService _authorizationService;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _ordersPerPage = 10;
        private bool _isLoading = false;
        private string _sortCriteria = "CREATED_TIME";
        private string _sortOrder = "ASC";
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private ObservableCollection<Order> _orders;
        private bool _canCreateOrders;
        private bool _canUpdateOrders;
        private bool _canDeleteOrders;

        public OrderViewModel(IOrderService orderService, ISessionService sessionService, IAuthorizationService authorizationService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _orders = new ObservableCollection<Order>();

            InitializePermissions();
        }

        private void InitializePermissions()
        {
            var role = _authorizationService.GetRole();
            CanCreateOrders = _authorizationService.HasPermission("CREATE_ORDERS");
            CanUpdateOrders = role == "ADMIN" || role == "SALE";
            CanDeleteOrders = _authorizationService.HasPermission("DELETE_ORDERS");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set
            {
                _orders = value;
                OnPropertyChanged();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfo));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfo));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                }
            }
        }

        public int OrdersPerPage
        {
            get => _ordersPerPage;
            set
            {
                if (_ordersPerPage != value)
                {
                    _ordersPerPage = value;
                    OnPropertyChanged();
                }
            }
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
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                }
            }
        }

        public bool CanCreateOrders
        {
            get => _canCreateOrders;
            set
            {
                if (_canCreateOrders != value)
                {
                    _canCreateOrders = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanUpdateOrders
        {
            get => _canUpdateOrders;
            set
            {
                if (_canUpdateOrders != value)
                {
                    _canUpdateOrders = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanDeleteOrders
        {
            get => _canDeleteOrders;
            set
            {
                if (_canDeleteOrders != value)
                {
                    _canDeleteOrders = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SortCriteria
        {
            get => _sortCriteria;
            set
            {
                if (_sortCriteria != value)
                {
                    _sortCriteria = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SortOrder
        {
            get => _sortOrder;
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
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

        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";
        public bool CanNavigatePrevious => CurrentPage > 1 && !IsLoading;
        public bool CanNavigateNext => CurrentPage < TotalPages && !IsLoading;

        public async Task LoadOrdersAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var toDateAdjusted = ToDate.HasValue ? ToDate.Value.AddDays(1) : (DateTime?)null;
                var token = _sessionService.GetAuthToken();
                var username = _sessionService.GetSavedUsername();

                Debug.WriteLine($"[ORDER_VM] Loading orders for user: {username ?? "No user"}");
                Debug.WriteLine($"[ORDER_VM] Token present: {!string.IsNullOrEmpty(token)}");
                Debug.WriteLine($"[ORDER_VM] Loading orders - From: {FromDate}, To: {toDateAdjusted}");
                Debug.WriteLine($"[ORDER_VM] Sort: {SortCriteria} ({SortOrder}), Items per page: {OrdersPerPage}");

                var totalCount = await _orderService.GetTotalOrderCountAsync(FromDate, toDateAdjusted, token);
                TotalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / OrdersPerPage) : 1;
                
                Debug.WriteLine($"[ORDER_VM] Total count: {totalCount}, Total pages: {TotalPages}");
                ValidateCurrentPage();

                var allOrders = await _orderService.GetOrdersAsync(CurrentPage, OrdersPerPage, FromDate, toDateAdjusted, token, SortCriteria, SortOrder);
                
                Debug.WriteLine($"[ORDER_VM] Retrieved {allOrders.Count} orders from service");

                Orders.Clear();
                foreach (var order in allOrders)
                {
                    Orders.Add(order);
                }
                
                Debug.WriteLine($"[ORDER_VM] ✓ Orders collection now contains {Orders.Count} orders");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_VM] ✗ Error loading orders: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var token = _sessionService.GetAuthToken();
                var success = await _orderService.DeleteOrderAsync(orderId, token);

                if (success)
                {
                    Debug.WriteLine($"[ORDER_VM] ✓ Order #{orderId} deleted successfully");
                    CurrentPage = 1;
                    await LoadOrdersAsync();
                }
                else
                {
                    throw new Exception("Failed to delete order.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_VM] ✗ Error deleting order: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task NavigateToPreviousPageAsync()
        {
            if (CanNavigatePrevious)
            {
                CurrentPage--;
                await LoadOrdersAsync();
            }
        }

        public async Task NavigateToNextPageAsync()
        {
            if (CanNavigateNext)
            {
                CurrentPage++;
                await LoadOrdersAsync();
            }
        }

        public async Task ResetFiltersAsync()
        {
            FromDate = null;
            ToDate = null;
            CurrentPage = 1;
            await LoadOrdersAsync();
        }

        public async Task ChangeSortCriteriaAsync(string criteria)
        {
            SortCriteria = criteria;
            CurrentPage = 1;
            await LoadOrdersAsync();
        }

        public async Task ChangeSortOrderAsync(string order)
        {
            SortOrder = order;
            CurrentPage = 1;
            await LoadOrdersAsync();
        }

        public async Task ChangeItemsPerPageAsync(int itemsPerPage)
        {
            OrdersPerPage = itemsPerPage;
            CurrentPage = 1;
            await LoadOrdersAsync();
        }

        public async Task ChangeDateRangeAsync()
        {
            CurrentPage = 1;
            await LoadOrdersAsync();
        }

        private void ValidateCurrentPage()
        {
            if (CurrentPage > TotalPages)
            {
                Debug.WriteLine($"[ORDER_VM] Page {CurrentPage} exceeds total pages {TotalPages}, adjusting to {TotalPages}");
                CurrentPage = TotalPages;
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}