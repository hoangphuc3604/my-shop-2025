using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class AddOrderViewModel : INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ISessionService _sessionService;

        private List<ProductSelection> _productSelections = new();
        private List<ProductSelection> _filteredProducts = new();
        private bool _isLoading;
        private int _selectedCount;
        private int _totalPrice;
        private string _searchText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddOrderViewModel(
            IOrderService orderService,
            IProductService productService,
            ISessionService sessionService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public List<ProductSelection> ProductSelections
        {
            get => _productSelections;
            set
            {
                if (_productSelections != value)
                {
                    _productSelections = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<ProductSelection> FilteredProducts
        {
            get => _filteredProducts;
            set
            {
                if (_filteredProducts != value)
                {
                    _filteredProducts = value;
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
                }
            }
        }

        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                if (_selectedCount != value)
                {
                    _selectedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalPrice
        {
            get => _totalPrice;
            set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterProducts();
                }
            }
        }

        public async Task LoadProductsAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var token = _sessionService.GetAuthToken();

                var products = await _productService.GetProductsAsync(
                    page: 1,
                    pageSize: 1000,
                    categoryId: null,
                    minPrice: null,
                    maxPrice: null,
                    search: null,
                    sortBy: null,
                    token: token);

                ProductSelections = products
                    .OrderBy(p => p.ProductId)
                    .Select(p => new ProductSelection
                    {
                        Product = p,
                        Quantity = 0,
                        OnQuantityChangedCallback = UpdateSummary
                    })
                    .ToList();

                FilteredProducts = new List<ProductSelection>(ProductSelections);
                UpdateSummary();

                Debug.WriteLine($"[ADD_ORDER_VM] ✓ Loaded {ProductSelections.Count} products");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADD_ORDER_VM] ✗ Error loading products: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void FilterProducts()
        {
            var searchText = SearchText?.ToLower() ?? string.Empty;

            FilteredProducts = string.IsNullOrEmpty(searchText)
                ? new List<ProductSelection>(ProductSelections)
                : ProductSelections
                    .Where(p => p.Product.Name.ToLower().Contains(searchText) ||
                               p.Product.Sku.ToLower().Contains(searchText))
                    .OrderBy(p => p.Product.ProductId)
                    .ToList();
        }

        public void UpdateSummary()
        {
            SelectedCount = ProductSelections.Count(s => s.Quantity > 0);
            TotalPrice = ProductSelections.Sum(s => s.TotalPrice);
        }

        public async Task<Order?> CreateOrderAsync()
        {
            if (IsLoading)
                return null;

            var selectedProducts = ProductSelections
                .Where(s => s.Quantity > 0)
                .ToList();

            if (selectedProducts.Count == 0)
            {
                throw new InvalidOperationException("Please select at least one product before creating an order.");
            }

            IsLoading = true;

            try
            {
                var token = _sessionService.GetAuthToken();

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

                Debug.WriteLine("[ADD_ORDER_VM] Creating order...");

                var newOrder = await _orderService.CreateOrderAsync(createOrderInput, token);

                if (newOrder != null)
                {
                    Debug.WriteLine($"[ADD_ORDER_VM] ✓ Order #{newOrder.OrderId} created successfully");
                    return newOrder;
                }
                else
                {
                    Debug.WriteLine("[ADD_ORDER_VM] ✗ newOrder is null - response parsing failed");
                    throw new Exception("Failed to create order. The response from the server could not be parsed.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADD_ORDER_VM] ✗ Exception: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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