using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Data;
using MyShop.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;

namespace MyShop.Views.Pages
{
    public sealed partial class AddOrderPage : Page
    {
        private MyShopDbContext _dbContext;
        private List<ProductSelection> _productSelections = new();

        public AddOrderPage()
        {
            this.InitializeComponent();
            _dbContext = (App.Services.GetService(typeof(MyShopDbContext)) as MyShopDbContext)!;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Only load products if navigating forward, not when going back
            if (e.NavigationMode != NavigationMode.Back)
            {
                await LoadProductsAsync();
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var products = await _dbContext.Products
                    .OrderBy(p => p.ProductId)
                    .ToListAsync();

                _productSelections = products.Select(p => new ProductSelection
                {
                    Product = p,
                    Quantity = 0,
                    OnQuantityChangedCallback = UpdateSummary
                }).ToList();

                ProductsDataGrid.ItemsSource = _productSelections;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text?.ToLower() ?? string.Empty;

            var filteredProducts = string.IsNullOrEmpty(searchText)
                ? _productSelections
                : _productSelections
                    .Where(p => p.Product.Name.ToLower().Contains(searchText) ||
                               p.Product.Sku.ToLower().Contains(searchText))
                    .OrderBy(p => p.Product.ProductId)
                    .ToList();

            ProductsDataGrid.ItemsSource = filteredProducts;
        }

        private void OnQuantityValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is ProductSelection selection)
            {
                int newValue = (int)args.NewValue;
                if (newValue >= 0 && newValue <= selection.Product.Count)
                {
                    selection.Quantity = newValue;
                }
            }
        }

        private void UpdateSummary()
        {
            var selectedCount = _productSelections.Count(s => s.Quantity > 0);
            var totalPrice = _productSelections.Sum(s => s.TotalPrice);

            SelectedCountText.Text = $"Products selected: {selectedCount}";
            TotalPriceText.Text = $"Total Price: {totalPrice.ToString("#,0 ₫")}";
        }

        private async void OnCreateOrderClicked(object sender, RoutedEventArgs e)
        {
            var selectedProducts = _productSelections
                .Where(s => s.Quantity > 0)
                .ToDictionary(s => s.Product.ProductId, s => s.Quantity);

            if (selectedProducts.Count == 0)
            {
                await ShowErrorAsync("Please select at least one product before creating an order.");
                return;
            }

            var totalPrice = _productSelections.Where(s => s.Quantity > 0).Sum(s => s.TotalPrice);

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

                await ShowSuccessAsync($"Order #{newOrder.OrderId} created successfully with {selectedProducts.Count} product(s)!");

                // Navigate back WITHOUT triggering reload (NavigationMode.Back will skip reload in OrderPage)
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to create order: {ex.Message}");
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            // Just navigate back - OrderPage won't reload due to NavigationMode.Back check
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async Task ShowSuccessAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    public class ProductSelection : INotifyPropertyChanged
    {
        private int _quantity;
        private Action? _onQuantityChangedCallback;

        public Product Product { get; set; } = new();

        public Action? OnQuantityChangedCallback
        {
            get => _onQuantityChangedCallback;
            set => _onQuantityChangedCallback = value;
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                    OnQuantityChangedCallback?.Invoke();
                }
            }
        }

        public int TotalPrice => Product.ImportPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}