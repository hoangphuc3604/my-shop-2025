using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Data;
using MyShop.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderPage : Page
    {
        private MyShopDbContext _dbContext;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int ORDERS_PER_PAGE = 10;
        private bool _isLoading = false;
        private ContentDialog? _currentDialog;
        private ObservableCollection<Order> _cachedOrders;
        private bool _dataLoaded = false;

        public OrderPage()
        {
            this.InitializeComponent();
            _dbContext = (App.Services.GetService(typeof(MyShopDbContext)) as MyShopDbContext)!;
            _cachedOrders = new ObservableCollection<Order>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Load data only on first navigation, or if navigating forward (not back)
            if (!_dataLoaded || e.NavigationMode != NavigationMode.Back)
            {
                await LoadOrdersAsync();
                _dataLoaded = true;
            }
            else
            {
                // Restore cached data when navigating back
                OrdersDataGrid.ItemsSource = _cachedOrders;
            }
        }

        /// <summary>
        /// Loads orders based on current filters and pagination settings
        /// </summary>
        private async Task LoadOrdersAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                _dbContext.ChangeTracker.Clear();

                var baseQuery = BuildBaseQuery();
                _totalPages = await CalculateTotalPages(baseQuery);
                ValidateCurrentPage();

                var orders = await baseQuery
                    .Include(o => o.OrderItems)
                    .OrderBy(o => o.OrderId)
                    .Skip((_currentPage - 1) * ORDERS_PER_PAGE)
                    .Take(ORDERS_PER_PAGE)
                    .ToListAsync();

                _cachedOrders.Clear();
                foreach (var order in orders)
                {
                    _cachedOrders.Add(order);
                }
                
                OrdersDataGrid.ItemsSource = _cachedOrders;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to load orders: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                UpdatePaginationControls();
            }
        }

        private IQueryable<Order> BuildBaseQuery()
        {
            var query = _dbContext.Orders.AsNoTracking();

            var fromDate = GetFromDate();
            var toDate = GetToDate();

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.CreatedTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.CreatedTime < toDate.Value);
            }

            return query;
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

        private async Task<int> CalculateTotalPages(IQueryable<Order> query)
        {
            int totalOrders = await query.CountAsync();
            return totalOrders > 0 ? (int)Math.Ceiling((double)totalOrders / ORDERS_PER_PAGE) : 1;
        }

        private void ValidateCurrentPage()
        {
            if (_currentPage > _totalPages)
                _currentPage = _totalPages;

            if (_currentPage < 1)
                _currentPage = 1;
        }

        private void UpdatePaginationControls()
        {
            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
            PreviousButton.IsEnabled = (_currentPage > 1) && !_isLoading;
            NextButton.IsEnabled = (_currentPage < _totalPages) && !_isLoading;
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

        private async void OnAddOrderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            Frame.Navigate(typeof(AddOrderPage));
        }

        private async void OnViewOrderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

        private async void OnEditOrderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

        private async void OnDeleteOrderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
                var orderToDelete = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (orderToDelete != null)
                {
                    _dbContext.Orders.Remove(orderToDelete);
                    await _dbContext.SaveChangesAsync();

                    await ShowSuccessDialogAsync($"Order #{orderId} deleted successfully!");

                    if (_currentPage > _totalPages - 1)
                    {
                        _currentPage = Math.Max(1, _currentPage - 1);
                    }

                    await LoadOrdersAsync();
                }
                else
                {
                    await ShowErrorDialogAsync("Order not found.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to delete order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void OnPreviousPageClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_currentPage > 1 && !_isLoading)
            {
                _currentPage--;
                await LoadOrdersAsync();
            }
        }

        private async void OnNextPageClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_currentPage < _totalPages && !_isLoading)
            {
                _currentPage++;
                await LoadOrdersAsync();
            }
        }

        private async void OnDateRangeChanged(object sender, Microsoft.UI.Xaml.Controls.CalendarDatePickerDateChangedEventArgs e)
        {
            if (!_isLoading)
            {
                _currentPage = 1;
                await LoadOrdersAsync();
            }
        }

        private async void OnClearFilterClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            FromDatePicker.Date = null;
            ToDatePicker.Date = null;

            _currentPage = 1;
            _totalPages = 1;

            await LoadOrdersAsync();
        }

        private async void OnResetFromDateClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            FromDatePicker.Date = null;

            _currentPage = 1;

            await LoadOrdersAsync();
        }

        private async void OnResetToDateClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            ToDatePicker.Date = null;

            _currentPage = 1;

            await LoadOrdersAsync();
        }
    }
}