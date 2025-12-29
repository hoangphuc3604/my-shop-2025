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
        private MyShopDbContext? _dbContext;

        public OrderPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                if (_dbContext == null)
                {
                    var optionsBuilder = new DbContextOptionsBuilder<MyShopDbContext>();
                    var connectionString = "Host=localhost;Port=5432;Database=myshop_db;Username=postgres;Password=password";
                    optionsBuilder.UseNpgsql(connectionString);
                    _dbContext = new MyShopDbContext(optionsBuilder.Options);
                }

                var orders = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .OrderBy(o => o.OrderId)
                    .ToListAsync();

                OrdersDataGrid.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Can't load orders: {ex.Message}",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void OnAddOrderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var productSelectionDialog = new ProductSelectionDialog();
            productSelectionDialog.XamlRoot = this.XamlRoot;
            
            await productSelectionDialog.LoadProductsAsync(_dbContext);
            
            var result = await productSelectionDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                if (productSelectionDialog.SelectedProducts.Count > 0)
                {
                    await CreateOrderWithProducts(
                        productSelectionDialog.SelectedProducts, 
                        productSelectionDialog.TotalPrice);
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "No Products Selected",
                        Content = "Please select at least one product before creating an order.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async Task CreateOrderWithProducts(Dictionary<int, int> selectedProducts, int totalPrice)
        {
            try
            {
                if (_dbContext == null)
                {
                    var optionsBuilder = new DbContextOptionsBuilder<MyShopDbContext>();
                    var connectionString = "Host=localhost;Port=5432;Database=myshop_db;Username=postgres;Password=password";
                    optionsBuilder.UseNpgsql(connectionString);
                    _dbContext = new MyShopDbContext(optionsBuilder.Options);
                }

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

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = $"Order #{newOrder.OrderId} created successfully with {selectedProducts.Count} product(s)!",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();

                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to create order: {ex.Message}",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void OnDeleteOrderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var dataContext = button?.DataContext as Order;

                if (dataContext == null)
                {
                    throw new Exception("Unable to identify order to delete");
                }

                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Delete",
                    Content = $"Are you sure you want to delete Order #{dataContext.OrderId}?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    if (_dbContext == null)
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<MyShopDbContext>();
                        var connectionString = "Host=localhost;Port=5432;Database=myshop_db;Username=postgres;Password=password";
                        optionsBuilder.UseNpgsql(connectionString);
                        _dbContext = new MyShopDbContext(optionsBuilder.Options);
                    }

                    var orderToDelete = await _dbContext.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.OrderId == dataContext.OrderId);

                    if (orderToDelete != null)
                    {
                        _dbContext.Orders.Remove(orderToDelete);
                        await _dbContext.SaveChangesAsync();

                        var successDialog = new ContentDialog
                        {
                            Title = "Success",
                            Content = $"Order #{dataContext.OrderId} deleted successfully!",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await successDialog.ShowAsync();

                        await LoadOrdersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to delete order: {ex.Message}",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void OnSortAscendingClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                if (_dbContext == null)
                {
                    var optionsBuilder = new DbContextOptionsBuilder<MyShopDbContext>();
                    var connectionString = "Host=localhost;Port=5432;Database=myshop_db;Username=postgres;Password=password";
                    optionsBuilder.UseNpgsql(connectionString);
                    _dbContext = new MyShopDbContext(optionsBuilder.Options);
                }

                var orders = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .OrderBy(o => o.OrderId)
                    .ToListAsync();

                OrdersDataGrid.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to sort orders: {ex.Message}",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}