using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Data;
using MyShop.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

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

        public OrderPage()
        {
            this.InitializeComponent();
            _dbContext = (App.Services.GetService(typeof(MyShopDbContext)) as MyShopDbContext)!;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadOrdersAsync();
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
                // Clear change tracker to ensure fresh data
                _dbContext.ChangeTracker.Clear();

                // Build the base query
                var baseQuery = BuildBaseQuery();

                // Get total count for pagination
                _totalPages = await CalculateTotalPages(baseQuery);

                // Validate current page
                ValidateCurrentPage();

                // Fetch paginated orders
                var orders = await baseQuery
                    .Include(o => o.OrderItems)
                    .OrderBy(o => o.OrderId)
                    .Skip((_currentPage - 1) * ORDERS_PER_PAGE)
                    .Take(ORDERS_PER_PAGE)
                    .ToListAsync();

                OrdersDataGrid.ItemsSource = orders;
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

        /// <summary>
        /// Builds the base query with date filters applied
        /// </summary>
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

        /// <summary>
        /// Gets the from date from the date picker
        /// </summary>
        private DateTime? GetFromDate()
        {
            return FromDatePicker.Date.HasValue
                ? FromDatePicker.Date.Value.UtcDateTime
                : null;
        }

        /// <summary>
        /// Gets the to date from the date picker (end of day)
        /// </summary>
        private DateTime? GetToDate()
        {
            if (!ToDatePicker.Date.HasValue)
                return null;

            // Add 1 day to include the entire selected day
            return ToDatePicker.Date.Value.UtcDateTime.AddDays(1);
        }

        /// <summary>
        /// Calculates the total number of pages
        /// </summary>
        private async Task<int> CalculateTotalPages(IQueryable<Order> query)
        {
            int totalOrders = await query.CountAsync();
            return totalOrders > 0 ? (int)Math.Ceiling((double)totalOrders / ORDERS_PER_PAGE) : 1;
        }

        /// <summary>
        /// Ensures the current page is within valid range
        /// </summary>
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
            // Close any existing dialog
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
            // Close any existing dialog
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

            try
            {
                var productSelectionDialog = new ProductSelectionDialog();
                productSelectionDialog.XamlRoot = this.XamlRoot;

                await productSelectionDialog.LoadProductsAsync(_dbContext);

                var result = await productSelectionDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    if (productSelectionDialog.SelectedProducts.Count > 0)
                    {
                        await CreateOrderWithProductsAsync(
                            productSelectionDialog.SelectedProducts,
                            productSelectionDialog.TotalPrice);
                    }
                    else
                    {
                        await ShowErrorDialogAsync("Please select at least one product before creating an order.");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to open product selection: {ex.Message}");
            }
        }

        private async Task CreateOrderWithProductsAsync(Dictionary<int, int> selectedProducts, int totalPrice)
        {
            _isLoading = true;
            UpdatePaginationControls();

            try
            {
                var newOrder = new Order
                {
                    CreatedTime = DateTime.UtcNow,
                    FinalPrice = totalPrice,
                    Status = "Created"
                };

                _dbContext.Orders.Add(newOrder);
                await _dbContext.SaveChangesAsync();

                // Add order items
                foreach (var kvp in selectedProducts)
                {
                    var product = await _dbContext.Products.FindAsync(kvp.Key);
                    if (product != null)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = newOrder.OrderId,
                            ProductId = kvp.Key,
                            Quantity = kvp.Value,
                            UnitSalePrice = product.ImportPrice,
                            TotalPrice = product.ImportPrice * kvp.Value
                        };
                        _dbContext.OrderItems.Add(orderItem);
                    }
                }

                await _dbContext.SaveChangesAsync();

                await ShowSuccessDialogAsync($"Order #{newOrder.OrderId} created successfully with {selectedProducts.Count} product(s)!");

                // Reset to first page and reload
                _currentPage = 1;
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to create order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
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

                // Close any existing dialog
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

                    // Reload with current page or adjust if necessary
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

            // Clear date pickers
            FromDatePicker.Date = null;
            ToDatePicker.Date = null;

            // Reset pagination
            _currentPage = 1;
            _totalPages = 1;

            // Reload all orders
            await LoadOrdersAsync();
        }

        private async void OnResetFromDateClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            // Clear from date
            FromDatePicker.Date = null;

            // Reset to first page
            _currentPage = 1;

            // Reload orders
            await LoadOrdersAsync();
        }

        private async void OnResetToDateClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            // Clear to date
            ToDatePicker.Date = null;

            // Reset to first page
            _currentPage = 1;

            // Reload orders
            await LoadOrdersAsync();
        }
    }
}