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
using System.Text;

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

                _currentPage = 1;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to create order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                await LoadOrdersAsync();
            }
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

                await ShowOrderDetailsAsync(order);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to view order: {ex.Message}");
            }
        }

        private async Task ShowOrderDetailsAsync(Order order)
        {
            try
            {
                var fullOrder = await _dbContext.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (fullOrder == null)
                {
                    await ShowErrorDialogAsync("Order not found.");
                    return;
                }

                var orderDetailsControl = new OrderDetailsDialog();
                orderDetailsControl.SetOrderData(fullOrder);

                if (_currentDialog != null)
                {
                    _currentDialog.Hide();
                    _currentDialog = null;
                }

                _currentDialog = new ContentDialog
                {
                    Title = $"Order #{fullOrder.OrderId} Details",
                    Content = orderDetailsControl,
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot,
                    MinWidth = 500,
                    MaxWidth = 600
                };

                await _currentDialog.ShowAsync();
                _currentDialog = null;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to load order details: {ex.Message}");
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

                await ShowEditOrderDialogAsync(order);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to edit order: {ex.Message}");
            }
        }

        private async Task ShowEditOrderDialogAsync(Order order)
        {
            try
            {
                var fullOrder = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (fullOrder == null)
                {
                    await ShowErrorDialogAsync("Order not found.");
                    return;
                }

                var editControl = new EditOrderDialog();
                editControl.SetOrderData(fullOrder);

                if (_currentDialog != null)
                {
                    _currentDialog.Hide();
                    _currentDialog = null;
                }

                _currentDialog = new ContentDialog
                {
                    Title = $"Edit Order #{fullOrder.OrderId}",
                    Content = editControl,
                    PrimaryButtonText = "Save Changes",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot,
                    MinWidth = 500,
                    MaxWidth = 600
                };

                var result = await _currentDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    await UpdateOrderAsync(fullOrder, editControl);
                }

                _currentDialog = null;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to open edit dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if all products in the order have sufficient inventory
        /// </summary>
        private async Task<(bool IsAvailable, string ErrorMessage)> CheckProductAvailabilityAsync(Order order)
        {
            var insufficientProducts = new List<string>();

            foreach (var orderItem in order.OrderItems)
            {
                var product = await _dbContext.Products.FindAsync(orderItem.ProductId);
                if (product == null)
                {
                    insufficientProducts.Add($"• {orderItem.ProductId} - Product not found");
                    continue;
                }

                if (product.Count < orderItem.Quantity)
                {
                    insufficientProducts.Add($"• {product.Name} (SKU: {product.Sku})\n  Required: {orderItem.Quantity} units, Available: {product.Count} units");
                }
            }

            if (insufficientProducts.Count > 0)
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine("Cannot mark order as Paid. Insufficient inventory for the following products:\n");
                foreach (var product in insufficientProducts)
                {
                    errorMessage.AppendLine(product);
                }

                return (false, errorMessage.ToString());
            }

            return (true, string.Empty);
        }

        private async Task UpdateOrderAsync(Order order, EditOrderDialog editControl)
        {
            _isLoading = true;
            UpdatePaginationControls();

            try
            {
                var newStatus = editControl.GetSelectedStatus();
                var updatedQuantities = editControl.GetUpdatedQuantities();

                var dbOrder = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (dbOrder == null)
                {
                    await ShowErrorDialogAsync("Order not found.");
                    return;
                }

                // Track if status changed to "Paid"
                bool statusChangedToPaid = dbOrder.Status != "Paid" && newStatus == "Paid";

                // If status is changing to "Paid", check inventory first
                if (statusChangedToPaid)
                {
                    var (isAvailable, errorMessage) = await CheckProductAvailabilityAsync(dbOrder);
                    if (!isAvailable)
                    {
                        await ShowErrorDialogAsync(errorMessage);
                        return;
                    }
                }

                // Update status
                dbOrder.Status = newStatus;

                // Update quantities
                foreach (var kvp in updatedQuantities)
                {
                    var orderItem = dbOrder.OrderItems.FirstOrDefault(oi => oi.OrderItemId == kvp.Key);
                    if (orderItem != null)
                    {
                        orderItem.Quantity = kvp.Value;
                        orderItem.TotalPrice = (int)(orderItem.UnitSalePrice * kvp.Value);
                    }
                }

                // Recalculate order total price
                dbOrder.FinalPrice = dbOrder.OrderItems.Sum(oi => oi.TotalPrice);

                // If status changed to "Paid", reduce product quantities
                if (statusChangedToPaid)
                {
                    foreach (var orderItem in dbOrder.OrderItems)
                    {
                        var product = await _dbContext.Products.FindAsync(orderItem.ProductId);
                        if (product != null)
                        {
                            product.Count -= orderItem.Quantity;
                            if (product.Count < 0)
                                product.Count = 0;
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();

                await ShowSuccessDialogAsync($"Order #{order.OrderId} updated successfully!");

                _currentPage = 1;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to update order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                await LoadOrdersAsync();
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
                await LoadOrdersAsync();
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