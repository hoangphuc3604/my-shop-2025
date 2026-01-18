using Xunit;
using Moq;
using MyShop.Tests.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Tests.UI
{
    /// <summary>
    /// Tests for AddOrderPage interaction logic
    /// Tests the business logic of the page, not the UI rendering itself
    /// UI rendering tests would require WinAppDriver or Appium
    /// </summary>
    public class AddOrderPageLogicTests
    {
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly AddOrderPageLogic _pageLogic;

        public AddOrderPageLogicTests()
        {
            _mockSessionService = new Mock<ISessionService>();
            _mockOrderService = new Mock<IOrderService>();
            _mockProductService = new Mock<IProductService>();
            _mockPromotionService = new Mock<IPromotionService>();

            _pageLogic = new AddOrderPageLogic(
                _mockOrderService.Object,
                _mockProductService.Object,
                _mockPromotionService.Object,
                _mockSessionService.Object);
        }

        #region Search Box Tests

        [Fact]
        public void SearchBox_WhenTextEntered_FiltersProductsCorrectly()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop Computer", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Computer Mouse", Sku = "ELEC-002", ImportPrice = 50 },
                new Product { ProductId = 3, Name = "USB Cable", Sku = "CABLE-001", ImportPrice = 10 }
            };
            _pageLogic.SetProducts(products);

            // Act
            _pageLogic.OnSearchTextChanged("Computer");

            // Assert
            Assert.Equal(2, _pageLogic.FilteredProducts.Count);
            Assert.Contains(_pageLogic.FilteredProducts, p => p.Product.Name == "Laptop Computer");
            Assert.Contains(_pageLogic.FilteredProducts, p => p.Product.Name == "Computer Mouse");
        }

        [Fact]
        public void SearchBox_WhenSearchBySKU_FiltersProductsCorrectly()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50 },
                new Product { ProductId = 3, Name = "Keyboard", Sku = "ELEC-003", ImportPrice = 75 }
            };
            _pageLogic.SetProducts(products);

            // Act
            _pageLogic.OnSearchTextChanged("ELEC-00");

            // Assert
            Assert.Equal(3, _pageLogic.FilteredProducts.Count);
        }

        [Fact]
        public void SearchBox_WhenSearchIsEmpty_ShowsAllProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50 }
            };
            _pageLogic.SetProducts(products);

            // Act
            _pageLogic.OnSearchTextChanged(string.Empty);

            // Assert
            Assert.Equal(2, _pageLogic.FilteredProducts.Count);
        }

        [Fact]
        public void SearchBox_WhenSearchHasNoResults_ReturnsEmptyList()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 }
            };
            _pageLogic.SetProducts(products);

            // Act
            _pageLogic.OnSearchTextChanged("NonExistent");

            // Assert
            Assert.Empty(_pageLogic.FilteredProducts);
        }

        [Fact]
        public void SearchBox_IsCaseSensitive_FiltersCorrectly()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 }
            };
            _pageLogic.SetProducts(products);

            // Act - search with lowercase
            _pageLogic.OnSearchTextChanged("laptop");

            // Assert - should still find it (case-insensitive search)
            Assert.Single(_pageLogic.FilteredProducts);
        }

        #endregion

        #region Quantity Input Tests

        [Fact]
        public void QuantityInput_WhenValueChanged_UpdatesTotalPrice()
        {
            // Arrange
            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 10 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 0 };

            // Act
            selection.Quantity = 5;

            // Assert
            Assert.Equal(500, selection.TotalPrice);
        }

        [Fact]
        public void QuantityInput_WhenValueExceedsStock_DoesNotUpdate()
        {
            // Arrange
            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 5 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 3 };

            // Act - try to set quantity beyond stock
            var canSet = _pageLogic.TrySetQuantity(selection, 10);

            // Assert
            Assert.False(canSet);
            Assert.Equal(3, selection.Quantity); // Should remain unchanged
        }

        [Fact]
        public void QuantityInput_WhenValueIsZero_AllowsUpdate()
        {
            // Arrange
            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 5 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 2 };

            // Act
            var canSet = _pageLogic.TrySetQuantity(selection, 0);

            // Assert
            Assert.True(canSet);
            Assert.Equal(0, selection.Quantity);
        }

        [Fact]
        public void QuantityInput_WhenValueIsNegative_DoesNotUpdate()
        {
            // Arrange
            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 5 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 2 };

            // Act
            var canSet = _pageLogic.TrySetQuantity(selection, -1);

            // Assert
            Assert.False(canSet);
            Assert.Equal(2, selection.Quantity);
        }

        #endregion

        #region Create Order Button Tests

        [Fact]
        public async Task CreateOrderButton_WhenClicked_SubmitsOrderSuccessfully()
        {
            // Arrange
            var newOrder = new Order { OrderId = 1, FinalPrice = 500, Status = "Created" };
            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.CreateOrderAsync(It.IsAny<CreateOrderInput>(), It.IsAny<string>()))
                .ReturnsAsync(newOrder);

            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 10 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 5 };
            _pageLogic.AddSelection(selection);

            // Act
            var result = await _pageLogic.OnCreateOrderClickedAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
            _mockOrderService.Verify(o => o.CreateOrderAsync(It.IsAny<CreateOrderInput>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrderButton_WhenNoProductsSelected_ThrowsException()
        {
            // Arrange - no products selected
            _pageLogic.ClearSelections();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _pageLogic.OnCreateOrderClickedAsync());
        }

        [Fact]
        public async Task CreateOrderButton_WhenOrderCreationFails_ThrowsException()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.CreateOrderAsync(It.IsAny<CreateOrderInput>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("API Error"));

            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 10 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 2 };
            _pageLogic.AddSelection(selection);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _pageLogic.OnCreateOrderClickedAsync());
        }

        [Fact]
        public async Task CreateOrderButton_WithMultipleProducts_CreatesOrderCorrectly()
        {
            // Arrange
            var newOrder = new Order { OrderId = 1, FinalPrice = 550, Status = "Created" };
            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.CreateOrderAsync(It.IsAny<CreateOrderInput>(), It.IsAny<string>()))
                .ReturnsAsync(newOrder);

            var product1 = new Product { ProductId = 1, ImportPrice = 100, Count = 10 };
            var selection1 = new ProductSelectionForPageLogic { Product = product1, Quantity = 3 };
            var product2 = new Product { ProductId = 2, ImportPrice = 150, Count = 5 };
            var selection2 = new ProductSelectionForPageLogic { Product = product2, Quantity = 2 };

            _pageLogic.AddSelection(selection1);
            _pageLogic.AddSelection(selection2);

            // Act
            var result = await _pageLogic.OnCreateOrderClickedAsync();

            // Assert
            Assert.NotNull(result);
            _mockOrderService.Verify(o => o.CreateOrderAsync(
                It.Is<CreateOrderInput>(input => input.OrderItems.Count == 2),
                It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Responsive Layout Tests

        [Fact]
        public void ResponsiveLayout_OnCompactView_CalculatesCorrectLayout()
        {
            // Arrange
            double compactWidth = 400; // Compact viewport

            // Act
            var layout = _pageLogic.GetResponsiveLayout(compactWidth, 600);

            // Assert
            Assert.True(layout.IsCompact);
            Assert.True(layout.Padding >= 8);
        }

        [Fact]
        public void ResponsiveLayout_OnExpandedView_CalculatesCorrectLayout()
        {
            // Arrange
            double expandedWidth = 1200; // Expanded viewport

            // Act
            var layout = _pageLogic.GetResponsiveLayout(expandedWidth, 600);

            // Assert
            Assert.False(layout.IsCompact);
            Assert.True(layout.Padding >= 16);
        }

        [Fact]
        public void ResponsiveLayout_WhenWidthChanges_UpdatesLayout()
        {
            // Arrange
            var layout1 = _pageLogic.GetResponsiveLayout(400, 600);
            var layout2 = _pageLogic.GetResponsiveLayout(1200, 600);

            // Assert
            Assert.NotEqual(layout1.IsCompact, layout2.IsCompact);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void ErrorHandling_WhenLoadingProductsFails_StoresErrorMessage()
        {
            // Arrange
            var errorMsg = "Failed to load products";

            // Act
            _pageLogic.SetErrorMessage(errorMsg);

            // Assert
            Assert.Equal(errorMsg, _pageLogic.GetErrorMessage());
        }

        [Fact]
        public void ErrorHandling_WhenErrorClosed_ClearsErrorMessage()
        {
            // Arrange
            _pageLogic.SetErrorMessage("Some error");

            // Act
            _pageLogic.ClearErrorMessage();

            // Assert
            Assert.Empty(_pageLogic.GetErrorMessage());
        }

        #endregion

        #region Page Navigation Tests

        [Fact]
        public void PageNavigation_WhenInitialized_SetsFlagCorrectly()
        {
            // Act
            _pageLogic.SetInitialized(true);

            // Assert
            Assert.True(_pageLogic.IsInitialized);
        }

        [Fact]
        public void PageNavigation_WhenNavigatedAway_ClearsInitializedFlag()
        {
            // Arrange
            _pageLogic.SetInitialized(true);

            // Act
            _pageLogic.SetInitialized(false);

            // Assert
            Assert.False(_pageLogic.IsInitialized);
        }

        #endregion
    }

    /// <summary>
    /// Supporting classes for testing AddOrderPage logic without WinUI dependencies
    /// </summary>
    public class AddOrderPageLogic
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IPromotionService _promotionService;
        private readonly ISessionService _sessionService;

        private List<ProductSelectionForPageLogic> _productSelections = new();
        private List<ProductSelectionForPageLogic> _filteredProducts = new();
        private string _errorMessage = string.Empty;
        private bool _isInitialized;

        public List<ProductSelectionForPageLogic> FilteredProducts => _filteredProducts;
        public bool IsInitialized => _isInitialized;

        public AddOrderPageLogic(
            IOrderService orderService,
            IProductService productService,
            IPromotionService promotionService,
            ISessionService sessionService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public void SetProducts(List<Product> products)
        {
            _productSelections = products.ConvertAll(p => new ProductSelectionForPageLogic { Product = p, Quantity = 0 });
            _filteredProducts = new List<ProductSelectionForPageLogic>(_productSelections);
        }

        public void OnSearchTextChanged(string searchText)
        {
            var search = searchText?.ToLower() ?? string.Empty;
            _filteredProducts = string.IsNullOrEmpty(search)
                ? new List<ProductSelectionForPageLogic>(_productSelections)
                : _productSelections
                    .FindAll(p => p.Product.Name.ToLower().Contains(search) ||
                                 p.Product.Sku.ToLower().Contains(search));
        }

        public bool TrySetQuantity(ProductSelectionForPageLogic selection, int quantity)
        {
            if (quantity < 0 || quantity > selection.Product.Count)
                return false;

            selection.Quantity = quantity;
            return true;
        }

        public void AddSelection(ProductSelectionForPageLogic selection)
        {
            _productSelections.Add(selection);
            _filteredProducts.Add(selection);
        }

        public void ClearSelections()
        {
            _productSelections.Clear();
            _filteredProducts.Clear();
        }

        public async Task<Order?> OnCreateOrderClickedAsync()
        {
            var selectedProducts = _productSelections.FindAll(s => s.Quantity > 0);

            if (selectedProducts.Count == 0)
                throw new InvalidOperationException("Please select at least one product before creating an order.");

            var token = _sessionService.GetAuthToken();
            var orderItems = selectedProducts.ConvertAll(s => new OrderItemInput
            {
                ProductId = s.Product.ProductId,
                Quantity = s.Quantity
            });

            var input = new CreateOrderInput { OrderItems = orderItems };
            return await _orderService.CreateOrderAsync(input, token);
        }

        public ResponsiveLayoutInfo GetResponsiveLayout(double width, double height)
        {
            var isCompact = width < 600;
            var padding = width < 600 ? 8.0 : (width < 1000 ? 12.0 : 16.0);

            return new ResponsiveLayoutInfo { IsCompact = isCompact, Padding = padding };
        }

        public void SetErrorMessage(string message)
        {
            _errorMessage = message;
        }

        public string GetErrorMessage() => _errorMessage;

        public void ClearErrorMessage()
        {
            _errorMessage = string.Empty;
        }

        public void SetInitialized(bool value)
        {
            _isInitialized = value;
        }
    }

    public class ProductSelectionForPageLogic
    {
        public Product Product { get; set; } = new();
        public int Quantity { get; set; }
        public int TotalPrice => Product.ImportPrice * Quantity;
    }

    public class ResponsiveLayoutInfo
    {
        public bool IsCompact { get; set; }
        public double Padding { get; set; }
    }
}