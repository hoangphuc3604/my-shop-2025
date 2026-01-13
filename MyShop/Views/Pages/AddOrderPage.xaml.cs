using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using CommunityToolkit.WinUI.UI.Controls;

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
            SizeChanged += AddOrderPage_SizeChanged;

            if (e.NavigationMode != NavigationMode.Back)
            {
                await LoadProductsAsync();
            }
        }

        private void AddOrderPage_SizeChanged(object sender, SizeChangedEventArgs e)
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

                Debug.WriteLine($"[ADD_ORDER_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                // Adjust DataGrid column widths for responsive display
                if (ProductsDataGrid?.Columns.Count > 0)
                {
                    if (isCompact)
                    {
                        // Mobile: Show only essential columns, reduce width
                        foreach (var column in ProductsDataGrid.Columns)
                        {
                            if (column.Header?.ToString() == "Description" || column.Header?.ToString() == "Category")
                            {
                                column.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                            }
                        }
                    }
                    else
                    {
                        // Desktop: Show all columns with appropriate widths
                        foreach (var column in ProductsDataGrid.Columns)
                        {
                            column.Visibility = Visibility.Visible;
                            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                        }
                    }
                }

                // Adjust search box and summary panel layout
                if (TopPanel != null)
                {
                    TopPanel.Padding = new Thickness(padding);
                }

                if (SummaryPanel != null)
                {
                    SummaryPanel.Padding = new Thickness(padding);
                }

                // Adjust button sizing for compact layouts
                if (isCompact)
                {
                    // Make buttons stack or adjust sizing for mobile
                    if (ButtonPanel != null)
                    {
                        ButtonPanel.Orientation = Orientation.Vertical;
                    }
                }
                else
                {
                    // Horizontal button layout for desktop
                    if (ButtonPanel != null)
                    {
                        ButtonPanel.Orientation = Orientation.Horizontal;
                    }
                }

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADD_ORDER_PAGE] Error applying responsive layout: {ex.Message}");
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

                Debug.WriteLine("[ADD_ORDER_PAGE] Creating order...");

                var newOrder = await _orderService.CreateOrderAsync(createOrderInput, token);

                Debug.WriteLine($"[ADD_ORDER_PAGE] Order creation result: {(newOrder != null ? "Success" : "Null")}");

                if (newOrder != null)
                {
                    Debug.WriteLine($"[ADD_ORDER_PAGE] ✓ Order #{newOrder.OrderId} created successfully");
                    
                    await ShowSuccessAsync($"Order #{newOrder.OrderId} created successfully with {selectedProducts.Count} product(s)!");

                    _isLoading = false;
                    
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    Debug.WriteLine("[ADD_ORDER_PAGE] ✗ newOrder is null - response parsing failed");
                    await ShowErrorAsync("Failed to create order. The response from the server could not be parsed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADD_ORDER_PAGE] ✗ Exception: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[ADD_ORDER_PAGE] ✗ Stack: {ex.StackTrace}");
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
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            var token = sessionService?.GetAuthToken();
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("[ADD_ORDER_PAGE] ✗ No authentication token available");
            }
            else
            {
                Debug.WriteLine("[ADD_ORDER_PAGE] ✓ Authentication token retrieved");
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