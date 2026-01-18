using Xunit;
using Moq;
using MyShop.Tests.Contracts;
using MyShop.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Tests.ViewModels
{
    // Test version of AddOrderViewModel (copy logic, no WinUI)
    public class AddOrderViewModelForTesting
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IPromotionService _promotionService;
        private readonly ISessionService _sessionService;
        private readonly IAuthorizationService _authorizationService;

        private List<ProductSelectionForTesting> _productSelections = new();
        private List<ProductSelectionForTesting> _filteredProducts = new();
        private string _searchText = string.Empty;

        public List<ProductSelectionForTesting> ProductSelections => _productSelections;
        public List<ProductSelectionForTesting> FilteredProducts => _filteredProducts;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterProducts();
            }
        }

        public AddOrderViewModelForTesting(
            IOrderService orderService,
            IProductService productService,
            IPromotionService promotionService,
            ISessionService sessionService,
            IAuthorizationService authorizationService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        public void FilterProducts()
        {
            var searchText = SearchText?.ToLower() ?? string.Empty;
            _filteredProducts = string.IsNullOrEmpty(searchText)
                ? new List<ProductSelectionForTesting>(_productSelections)
                : _productSelections
                    .Where(p => p.Product.Name.ToLower().Contains(searchText) ||
                               p.Product.Sku.ToLower().Contains(searchText))
                    .ToList();
        }

        public void SetProducts(List<Product> products)
        {
            _productSelections = products.Select(p => new ProductSelectionForTesting { Product = p }).ToList();
            _filteredProducts = new List<ProductSelectionForTesting>(_productSelections);
        }
    }

    public class ProductSelectionForTesting
    {
        public Product Product { get; set; } = new();
        public int Quantity { get; set; }
    }

    public class AddOrderViewModelTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IAuthorizationService> _mockAuthorizationService;
        private readonly AddOrderViewModelForTesting _viewModel;

        public AddOrderViewModelTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            _mockProductService = new Mock<IProductService>();
            _mockPromotionService = new Mock<IPromotionService>();
            _mockSessionService = new Mock<ISessionService>();
            _mockAuthorizationService = new Mock<IAuthorizationService>();

            _viewModel = new AddOrderViewModelForTesting(
                _mockOrderService.Object,
                _mockProductService.Object,
                _mockPromotionService.Object,
                _mockSessionService.Object,
                _mockAuthorizationService.Object);
        }

        #region FilterProducts Tests

        [Fact]
        public void FilterProducts_WithEmptySearchText_ReturnsAllProducts()
        {
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50 }
            };
            _viewModel.SetProducts(products);
            _viewModel.SearchText = string.Empty;

            Assert.Equal(2, _viewModel.FilteredProducts.Count);
        }

        [Fact]
        public void FilterProducts_WithSearchText_FiltersProductsByName()
        {
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50 }
            };
            _viewModel.SetProducts(products);
            _viewModel.SearchText = "Laptop";

            Assert.Single(_viewModel.FilteredProducts);
            Assert.Equal("Laptop", _viewModel.FilteredProducts[0].Product.Name);
        }

        [Fact]
        public void FilterProducts_WithSearchText_FiltersProductsBySku()
        {
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Laptop", Sku = "ELEC-001", ImportPrice = 1000 },
                new Product { ProductId = 2, Name = "Mouse", Sku = "ELEC-002", ImportPrice = 50 }
            };
            _viewModel.SetProducts(products);
            _viewModel.SearchText = "ELEC-001";

            Assert.Single(_viewModel.FilteredProducts);
            Assert.Equal("ELEC-001", _viewModel.FilteredProducts[0].Product.Sku);
        }

        #endregion
    }
}