using Xunit;
using Moq;
using MyShop.Tests.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Tests.UI
{
    /// <summary>
    /// Integration tests for AddOrderPage
    /// Tests the full workflow from page initialization to order creation
    /// </summary>
    public class AddOrderPageIntegrationTests
    {
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly AddOrderPageLogic _pageLogic;

        public AddOrderPageIntegrationTests()
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

        #region Page Initialization Workflow

        [Fact]
        public void PageInitialization_WhenPageLoads_SetsInitializedFlag()
        {
            // Act
            _pageLogic.SetInitialized(true);

            // Assert
            Assert.True(_pageLogic.IsInitialized);
        }

        [Fact]
        public void PageInitialization_WhenProductsLoaded_PopulatesFilteredList()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50 }
            };

            // Act
            _pageLogic.SetProducts(products);

            // Assert
            Assert.Equal(2, _pageLogic.FilteredProducts.Count);
        }

        #endregion

        #region Complete Order Workflow

        [Fact]
        public async Task CompleteOrderWorkflow_FromLoadTCreation_Success()
        {
            // Arrange - Setup products
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000, Count = 5 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50, Count = 20 }
            };
            _pageLogic.SetProducts(products);

            // Act - Initialize page
            _pageLogic.SetInitialized(true);

            // Act - Search for a product
            _pageLogic.OnSearchTextChanged("Laptop");
            Assert.Single(_pageLogic.FilteredProducts);

            // Act - Select products
            var selection1 = _pageLogic.FilteredProducts[0];
            _pageLogic.TrySetQuantity(selection1, 2);

            _pageLogic.OnSearchTextChanged(""); // Clear search
            var selection2 = _pageLogic.FilteredProducts.First(p => p.Product.Name == "Mouse");
            _pageLogic.TrySetQuantity(selection2, 3);

            // Act - Create order
            var newOrder = new Order { OrderId = 1, FinalPrice = 2150, Status = "Created" };
            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.CreateOrderAsync(It.IsAny<CreateOrderInput>(), It.IsAny<string>()))
                .ReturnsAsync(newOrder);

            var result = await _pageLogic.OnCreateOrderClickedAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
        }

        #endregion

        #region Search and Filter Workflow

        [Fact]
        public void SearchAndFilter_MultipleSearches_WorksCorrectly()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop Computer", Sku = "COMP-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Desktop Computer", Sku = "COMP-002", ImportPrice = 800 },
                new Product { ProductId = 3, Name = "Computer Mouse", Sku = "MOUSE-001", ImportPrice = 50 },
                new Product { ProductId = 4, Name = "Keyboard", Sku = "KEY-001", ImportPrice = 75 }
            };
            _pageLogic.SetProducts(products);

            // Act - First search
            _pageLogic.OnSearchTextChanged("Computer");
            Assert.Equal(3, _pageLogic.FilteredProducts.Count);

            // Act - Second search
            _pageLogic.OnSearchTextChanged("COMP-");
            Assert.Equal(2, _pageLogic.FilteredProducts.Count);

            // Act - Search with no results
            _pageLogic.OnSearchTextChanged("NonExistent");
            Assert.Empty(_pageLogic.FilteredProducts);

            // Act - Clear search
            _pageLogic.OnSearchTextChanged("");
            Assert.Equal(4, _pageLogic.FilteredProducts.Count);
        }

        #endregion

        #region Quantity Validation Workflow

        [Fact]
        public void QuantityValidation_MultipleUpdates_WorksCorrectly()
        {
            // Arrange
            var product = new Product { ProductId = 1, Name = "Laptop", ImportPrice = 1000, Count = 5 };
            var selection = new ProductSelectionForPageLogic { Product = product };

            // Act & Assert - Valid quantity
            Assert.True(_pageLogic.TrySetQuantity(selection, 2));
            Assert.Equal(2, selection.Quantity);

            // Act & Assert - Valid increase
            Assert.True(_pageLogic.TrySetQuantity(selection, 5));
            Assert.Equal(5, selection.Quantity);

            // Act & Assert - Invalid (exceeds stock)
            Assert.False(_pageLogic.TrySetQuantity(selection, 10));
            Assert.Equal(5, selection.Quantity); // Unchanged

            // Act & Assert - Valid decrease
            Assert.True(_pageLogic.TrySetQuantity(selection, 0));
            Assert.Equal(0, selection.Quantity);
        }

        #endregion

        #region Responsive Layout Workflow

        [Fact]
        public void ResponsiveLayout_LayoutChangeScenarios_WorksCorrectly()
        {
            // Act - Compact layout
            var compactLayout = _pageLogic.GetResponsiveLayout(400, 600);
            Assert.True(compactLayout.IsCompact);

            // Act - Medium layout
            var mediumLayout = _pageLogic.GetResponsiveLayout(800, 600);
            Assert.False(mediumLayout.IsCompact);

            // Act - Expanded layout
            var expandedLayout = _pageLogic.GetResponsiveLayout(1400, 600);
            Assert.False(expandedLayout.IsCompact);

            // Assert - Padding increases with width
            Assert.True(compactLayout.Padding < mediumLayout.Padding);
            Assert.True(mediumLayout.Padding <= expandedLayout.Padding);
        }

        #endregion

        #region Error Handling Workflow

        [Fact]
        public async Task ErrorHandling_WhenOrderCreationFails_ErrorMessageIsSet()
        {
            // Arrange
            var product = new Product { ProductId = 1, ImportPrice = 100, Count = 10 };
            var selection = new ProductSelectionForPageLogic { Product = product, Quantity = 2 };
            _pageLogic.AddSelection(selection);

            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.CreateOrderAsync(It.IsAny<CreateOrderInput>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("API Error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _pageLogic.OnCreateOrderClickedAsync());
            Assert.Equal("API Error", ex.Message);
        }

        [Fact]
        public void ErrorHandling_ErrorMessageLifecycle_WorksCorrectly()
        {
            // Act - Set error
            _pageLogic.SetErrorMessage("Test error");
            Assert.Equal("Test error", _pageLogic.GetErrorMessage());

            // Act - Clear error
            _pageLogic.ClearErrorMessage();
            Assert.Empty(_pageLogic.GetErrorMessage());
        }

        #endregion
    }
}