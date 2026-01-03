using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
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
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private List<ProductSelection> _productSelections = new();
        private bool _isLoading = false;
        private ContentDialog? _currentDialog;

        public AddOrderPage()
        {
            this.InitializeComponent();
            _orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            _productService = (App.Services.GetService(typeof(IProductService)) as IProductService)!;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
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
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                var token = GetAuthToken();

                // Get all products (page 1, large page size to get all)
                var products = await _productService.GetProductsAsync(
                    page: 1,
                    pageSize: 1000,
                    categoryId: null,
                    minPrice: null,
                    maxPrice: null,
                    search: null,
                    sortBy: null,
                    token: token);

                _productSelections = products
                    .OrderBy(p => p.ProductId)
                    .Select(p => new ProductSelection
                    {
                        Product = p,
                        Quantity = 0,
                        OnQuantityChangedCallback = UpdateSummary
                    })
                    .ToList();

                ProductsDataGrid.ItemsSource = _productSelections;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
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
            if (_isLoading)
                return;

            var selectedProducts = _productSelections
                .Where(s => s.Quantity > 0)
                .ToList();

            if (selectedProducts.Count == 0)
            {
                await ShowErrorAsync("Please select at least one product before creating an order.");
                return;
            }

            await CreateOrderAsync(selectedProducts);
        }

        private async Task CreateOrderAsync(List<ProductSelection> selectedProducts)
        {
            _isLoading = true;

            try
            {
                var token = GetAuthToken();

                // Build order items from selected products
                var orderItems = selectedProducts
                    .Select(s => new OrderItemInput
                    {
                        ProductId = s.Product.ProductId,
                        Quantity = s.Quantity
                    })
                    .ToList();

                var createOrderInput = new CreateOrderInput
                {
                    OrderItems = orderItems
                };

                System.Diagnostics.Debug.WriteLine("[ADD_ORDER_PAGE] Creating order...");

                // Create order via GraphQL API
                var newOrder = await _orderService.CreateOrderAsync(createOrderInput, token);

                System.Diagnostics.Debug.WriteLine($"[ADD_ORDER_PAGE] Order creation result: {(newOrder != null ? "Success" : "Null")}");

                // Check if order was created successfully
                if (newOrder != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ADD_ORDER_PAGE] ✓ Order #{newOrder.OrderId} created successfully");
                    
                    // Show success dialog
                    await ShowSuccessAsync($"Order #{newOrder.OrderId} created successfully with {selectedProducts.Count} product(s)!");

                    // Reset _isLoading BEFORE navigating back
                    _isLoading = false;
                    
                    // Navigate back after dialog closes
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ADD_ORDER_PAGE] ✗ newOrder is null - response parsing failed");
                    await ShowErrorAsync("Failed to create order. The response from the server could not be parsed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ADD_ORDER_PAGE] ✗ Exception: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ADD_ORDER_PAGE] ✗ Stack: {ex.StackTrace}");
                await ShowErrorAsync($"Failed to create order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async Task ShowErrorAsync(string message)
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

        private async Task ShowSuccessAsync(string message)
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

        private string? GetAuthToken()
        {
            // Get token from session storage
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            var token = sessionService?.GetAuthToken();
            
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("[ADD_ORDER_PAGE] ✗ No authentication token available");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ADD_ORDER_PAGE] ✓ Authentication token retrieved");
            }
            
            return token;
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