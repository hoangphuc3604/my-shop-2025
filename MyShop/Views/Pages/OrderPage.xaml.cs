using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Diagnostics;

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

        public OrderPage()
        {
            this.InitializeComponent();
            _orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            _cachedOrders = new ObservableCollection<Order>();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {   
            base.OnNavigatedTo(e);
            
            _currentPage = 1;
            _totalPages = 1;
            await LoadOrdersAsync();
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

                Debug.WriteLine($"[ORDER_PAGE] Loading orders - Page: {_currentPage}, From: {fromDate}, To: {toDate}");

                // Get total count first
                var totalCount = await _orderService.GetTotalOrderCountAsync(fromDate, toDate, token);
                _totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / ORDERS_PER_PAGE) : 1;
                
                Debug.WriteLine($"[ORDER_PAGE] Total count: {totalCount}, Total pages: {_totalPages}");
                
                // Validate current page against new total pages
                ValidateCurrentPage();
                
                Debug.WriteLine($"[ORDER_PAGE] Validated page: {_currentPage}");

                // Get orders for current page
                var orders = await _orderService.GetOrdersAsync(_currentPage, ORDERS_PER_PAGE, fromDate, toDate, token);
                
                Debug.WriteLine($"[ORDER_PAGE] Retrieved {orders.Count} orders from service");

                // Clear the collection completely
                _cachedOrders.Clear();
                
                Debug.WriteLine($"[ORDER_PAGE] Cleared cache, adding new orders...");

                // Add new orders
                foreach (var order in orders)
                {
                    _cachedOrders.Add(order);
                    Debug.WriteLine($"[ORDER_PAGE] Added order #{order.OrderId}");
                }
                
                Debug.WriteLine($"[ORDER_PAGE] Cache now contains {_cachedOrders.Count} orders");

                // Force DataGrid to refresh
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

        private DateTime? GetFromDate()
        {
            return FromDatePicker.Date.HasValue
                ? FromDatePicker.Date.Value.UtcDateTime
                : null;
        }

        private DateTime? GetToDate()
        {
            if (!ToDatePicker.Date.HasValue)
                return null;

            return ToDatePicker.Date.Value.UtcDateTime.AddDays(1);
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
                    
                    // Show success dialog first
                    await ShowSuccessDialogAsync($"Order #{orderId} deleted successfully!");

                    // THEN reset pagination and reload
                    _currentPage = 1;
                    _totalPages = 1;
                    _isLoading = false; // Reset before reload
                    
                    // Load fresh data
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
            if (!_isLoading)
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
    }
}