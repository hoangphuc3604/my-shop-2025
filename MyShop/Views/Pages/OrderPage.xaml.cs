using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderPage : Page
    {
        private readonly IOrderService _orderService;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int ORDERS_PER_PAGE = 10;
        private bool _isLoading = false;
        private ContentDialog? _currentDialog;
        private ObservableCollection<Order> _cachedOrders;
        private string _sortCriteria = "OrderId";
        private string _sortOrder = "Asc";
        private List<Order> _allOrdersForCurrentDateRange = new List<Order>();
        private bool _isInitialized = false;

        public OrderPage()
        {
            this.InitializeComponent();
            _orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            _cachedOrders = new ObservableCollection<Order>();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {   
            base.OnNavigatedTo(e);
            SizeChanged += OrderPage_SizeChanged;
            _currentPage = 1;
            _totalPages = 1;
            _sortCriteria = "OrderId";
            _sortOrder = "Asc";
            _isInitialized = true;
            await LoadOrdersAsync();
        }

        private void OrderPage_SizeChanged(object sender, SizeChangedEventArgs e)
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

                Debug.WriteLine($"[ORDER_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadOrdersAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            UpdatePaginationControls();

            try
            {
                var fromDate = GetFromDate();
                var toDate = GetToDate();
                var token = GetAuthToken();

                Debug.WriteLine($"[ORDER_PAGE] Loading orders - From: {fromDate}, To: {toDate}");
                Debug.WriteLine($"[ORDER_PAGE] Sort: {_sortCriteria} ({_sortOrder})");

                // Get total count for pagination
                var totalCount = await _orderService.GetTotalOrderCountAsync(fromDate, toDate, token);
                _totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / ORDERS_PER_PAGE) : 1;
                
                Debug.WriteLine($"[ORDER_PAGE] Total count: {totalCount}, Total pages: {_totalPages}");
                ValidateCurrentPage();
                Debug.WriteLine($"[ORDER_PAGE] Validated page: {_currentPage}");

                // ⭐ KEY FIX: Fetch ALL orders for the date range (not just current page)
                // Use a large page size to get all orders at once
                var allOrders = await _orderService.GetOrdersAsync(1, 10000, fromDate, toDate, token);
                
                Debug.WriteLine($"[ORDER_PAGE] Retrieved {allOrders.Count} total orders from service");

                // Store all orders for reference
                _allOrdersForCurrentDateRange = allOrders;

                // Apply sorting to the complete list
                var sortedOrders = ApplySorting(allOrders);

                Debug.WriteLine($"[ORDER_PAGE] After sorting: {sortedOrders.Count} orders");

                // NOW paginate the sorted results
                var paginatedOrders = sortedOrders
                    .Skip((_currentPage - 1) * ORDERS_PER_PAGE)
                    .Take(ORDERS_PER_PAGE)
                    .ToList();

                Debug.WriteLine($"[ORDER_PAGE] Paginated to page {_currentPage}: {paginatedOrders.Count} orders");

                // Update UI with paginated results
                _cachedOrders.Clear();
                Debug.WriteLine($"[ORDER_PAGE] Cleared cache, adding paginated orders...");

                foreach (var order in paginatedOrders)
                {
                    _cachedOrders.Add(order);
                    Debug.WriteLine($"[ORDER_PAGE] Added order #{order.OrderId}");
                }
                
                Debug.WriteLine($"[ORDER_PAGE] Cache now contains {_cachedOrders.Count} orders");

                OrdersDataGrid.ItemsSource = null;
                OrdersDataGrid.ItemsSource = _cachedOrders;
                
                Debug.WriteLine($"[ORDER_PAGE] DataGrid refreshed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_PAGE] ✗ Error loading orders: {ex.Message}");
                Debug.WriteLine($"[ORDER_PAGE] ✗ Stack Trace: {ex.StackTrace}");
                await ShowErrorDialogAsync($"Failed to load orders: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                UpdatePaginationControls();
            }
        }

        /// <summary>
        /// Apply sorting to the orders based on current criteria and order
        /// </summary>
        private List<Order> ApplySorting(List<Order> orders)
        {
            IOrderedEnumerable<Order> sortedOrders = null;

            // Apply initial sort based on criteria
            switch (_sortCriteria)
            {
                case "FinalPrice":
                    sortedOrders = _sortOrder == "Asc"
                        ? orders.OrderBy(o => o.FinalPrice)
                        : orders.OrderByDescending(o => o.FinalPrice);
                    break;

                case "Status":
                    sortedOrders = _sortOrder == "Asc"
                        ? orders.OrderBy(o => o.Status)
                        : orders.OrderByDescending(o => o.Status);
                    break;

                case "OrderId":
                default:
                    sortedOrders = _sortOrder == "Asc"
                        ? orders.OrderBy(o => o.OrderId)
                        : orders.OrderByDescending(o => o.OrderId);
                    break;
            }

            Debug.WriteLine($"[ORDER_PAGE] ✓ Sorted {orders.Count} orders by {_sortCriteria} ({_sortOrder})");
            return sortedOrders.ToList();
        }

        private DateTime? GetFromDate()
        {
            return FromDatePicker.Date.HasValue ? FromDatePicker.Date.Value.DateTime : null;
        }

        private DateTime? GetToDate()
        {
            if (!ToDatePicker.Date.HasValue)
                return null;
            return ToDatePicker.Date.Value.DateTime.AddDays(1);
        }

        private string? GetAuthToken()
        {
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            var token = sessionService?.GetAuthToken();
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("[ORDER_PAGE] ✗ No authentication token available");
            }
            else
            {
                Debug.WriteLine("[ORDER_PAGE] ✓ Authentication token retrieved");
            }
            
            return token;
        }

        private void ValidateCurrentPage()
        {
            if (_currentPage > _totalPages)
            {
                Debug.WriteLine($"[ORDER_PAGE] Page {_currentPage} exceeds total pages {_totalPages}, adjusting to {_totalPages}");
                _currentPage = _totalPages;
            }

            if (_currentPage < 1)
            {
                _currentPage = 1;
            }
        }

        private void UpdatePaginationControls()
        {
            // Guard against null reference when page not fully initialized
            if (!_isInitialized || PageInfoText == null || PreviousButton == null || NextButton == null)
            {
                Debug.WriteLine($"[ORDER_PAGE] UpdatePaginationControls called before page initialized, skipping...");
                return;
            }

            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
            PreviousButton.IsEnabled = (_currentPage > 1) && !_isLoading;
            NextButton.IsEnabled = (_currentPage < _totalPages) && !_isLoading;
            Debug.WriteLine($"[ORDER_PAGE] Updated pagination: {PageInfoText.Text}");
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog = null;
            }

            _currentDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await _currentDialog.ShowAsync();
            _currentDialog = null;
        }

        private async Task ShowSuccessDialogAsync(string message)
        {
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog = null;
            }

            _currentDialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await _currentDialog.ShowAsync();
            _currentDialog = null;
        }

        private async void OnAddOrderClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            Frame.Navigate(typeof(AddOrderPage));
        }

        private async void OnViewOrderClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            try
            {
                var button = sender as Button;
                var order = button?.DataContext as Order;

                if (order == null)
                {
                    await ShowErrorDialogAsync("Unable to identify order to view.");
                    return;
                }

                Frame.Navigate(typeof(OrderDetailsPage), order);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to view order: {ex.Message}");
            }
        }

        private async void OnEditOrderClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            try
            {
                var button = sender as Button;
                var order = button?.DataContext as Order;

                if (order == null)
                {
                    await ShowErrorDialogAsync("Unable to identify order to edit.");
                    return;
                }

                if (order.Status != "Created")
                {
                    await ShowErrorDialogAsync($"Cannot edit order with status '{order.Status}'. Only orders with 'Created' status can be edited.");
                    return;
                }

                Frame.Navigate(typeof(EditOrderPage), order);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to edit order: {ex.Message}");
            }
        }

        private async void OnDeleteOrderClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            try
            {
                var button = sender as Button;
                var order = button?.DataContext as Order;

                if (order == null)
                {
                    await ShowErrorDialogAsync("Unable to identify order to delete.");
                    return;
                }

                if (_currentDialog != null)
                {
                    _currentDialog.Hide();
                    _currentDialog = null;
                }

                _currentDialog = new ContentDialog
                {
                    Title = "Confirm Delete",
                    Content = $"Are you sure you want to delete Order #{order.OrderId}?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await _currentDialog.ShowAsync();
                _currentDialog = null;

                if (result == ContentDialogResult.Primary)
                {
                    await DeleteOrderAsync(order.OrderId);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to delete order: {ex.Message}");
            }
        }

        private async Task DeleteOrderAsync(int orderId)
        {
            _isLoading = true;
            UpdatePaginationControls();

            try
            {
                var token = GetAuthToken();
                var success = await _orderService.DeleteOrderAsync(orderId, token);

                if (success)
                {
                    Debug.WriteLine($"[ORDER_PAGE] ✓ Order #{orderId} deleted successfully");
                    await ShowSuccessDialogAsync($"Order #{orderId} deleted successfully!");
                    _currentPage = 1;
                    _totalPages = 1;
                    _isLoading = false;
                    await LoadOrdersAsync();
                }
                else
                {
                    _isLoading = false;
                    await ShowErrorDialogAsync("Failed to delete order.");
                }
            }
            catch (Exception ex)
            {
                _isLoading = false;
                await ShowErrorDialogAsync($"Failed to delete order: {ex.Message}");
            }
        }

        private async void OnPreviousPageClicked(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1 && !_isLoading)
            {
                _currentPage--;
                await LoadOrdersAsync();
            }
        }

        private async void OnNextPageClicked(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages && !_isLoading)
            {
                _currentPage++;
                await LoadOrdersAsync();
            }
        }

        private async void OnDateRangeChanged(Microsoft.UI.Xaml.Controls.CalendarDatePicker sender, Microsoft.UI.Xaml.Controls.CalendarDatePickerDateChangedEventArgs e)
        {
            if (!_isLoading && _isInitialized)
            {
                _currentPage = 1;
                await LoadOrdersAsync();
            }
        }

        private async void OnClearFilterClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            FromDatePicker.Date = null;
            ToDatePicker.Date = null;

            _currentPage = 1;
            _totalPages = 1;

            await LoadOrdersAsync();
        }

        private async void OnResetFromDateClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            FromDatePicker.Date = null;
            _currentPage = 1;

            await LoadOrdersAsync();
        }

        private async void OnResetToDateClicked(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            ToDatePicker.Date = null;
            _currentPage = 1;

            await LoadOrdersAsync();
        }

        /// <summary>
        /// Handle sort criteria change
        /// </summary>
        private async void OnSortCriteriaChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized)
                return;

            var selectedItem = SortCriteriaCombo.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                _sortCriteria = selectedItem.Tag.ToString();
                Debug.WriteLine($"[ORDER_PAGE] Sort criteria changed to: {_sortCriteria}");
                
                _currentPage = 1;
                await LoadOrdersAsync();
            }
        }

        /// <summary>
        /// Handle sort order change
        /// </summary>
        private async void OnSortOrderChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized)
                return;

            var selectedItem = SortOrderCombo.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                _sortOrder = selectedItem.Tag.ToString();
                Debug.WriteLine($"[ORDER_PAGE] Sort order changed to: {_sortOrder}");
                
                _currentPage = 1;
                await LoadOrdersAsync();
            }
        }
    }
}