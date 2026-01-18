using Xunit;
using Moq;
using MyShop.Tests.Contracts;
using MyShop.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Tests.ViewModels
{
    public class OrderDetailsViewModelForTesting
    {
        private readonly IOrderService _orderService;
        private readonly IPromotionService _promotionService;
        private readonly ISessionService _sessionService;

        public Order? CurrentOrder { get; private set; }
        public List<OrderItem> OrderItems { get; } = new();

        public OrderDetailsViewModelForTesting(
            IOrderService orderService,
            IPromotionService promotionService,
            ISessionService sessionService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public int GetSubtotal()
        {
            int total = 0;
            foreach (var item in OrderItems)
            {
                total += item.TotalPrice;
            }
            return total;
        }

        public async Task LoadOrderDetailsAsync(Order order)
        {
            var token = _sessionService.GetAuthToken();
            var fullOrder = await _orderService.GetOrderByIdAsync(order.OrderId, token);

            if (fullOrder == null)
                throw new Exception("Order not found.");

            CurrentOrder = fullOrder;
            OrderItems.Clear();

            if (fullOrder.OrderItems != null)
            {
                foreach (var item in fullOrder.OrderItems)
                {
                    OrderItems.Add(item);
                }
            }
        }
    }

    public class OrderDetailsViewModelTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly OrderDetailsViewModelForTesting _viewModel;

        public OrderDetailsViewModelTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            _mockPromotionService = new Mock<IPromotionService>();
            _mockSessionService = new Mock<ISessionService>();

            _viewModel = new OrderDetailsViewModelForTesting(
                _mockOrderService.Object,
                _mockPromotionService.Object,
                _mockSessionService.Object);
        }

        [Fact]
        public async Task LoadOrderDetailsAsync_WhenOrderExists_LoadsOrderSuccessfully()
        {
            var order = new Order
            {
                OrderId = 1,
                Status = "Paid",
                FinalPrice = 500,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderItemId = 1, Quantity = 2, TotalPrice = 500 }
                }
            };

            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.GetOrderByIdAsync(1, "token")).ReturnsAsync(order);

            await _viewModel.LoadOrderDetailsAsync(order);

            Assert.NotNull(_viewModel.CurrentOrder);
            Assert.Equal(1, _viewModel.CurrentOrder.OrderId);
            Assert.Single(_viewModel.OrderItems);
        }

        [Fact]
        public async Task LoadOrderDetailsAsync_WhenOrderNotFound_ThrowsException()
        {
            var order = new Order { OrderId = 999 };
            _mockSessionService.Setup(s => s.GetAuthToken()).Returns("token");
            _mockOrderService.Setup(o => o.GetOrderByIdAsync(999, "token")).ReturnsAsync((Order)null);

            await Assert.ThrowsAsync<Exception>(() => _viewModel.LoadOrderDetailsAsync(order));
        }

        [Fact]
        public void Subtotal_WhenOrderItemsExist_CalculatesCorrectly()
        {
            _viewModel.OrderItems.Add(new OrderItem { TotalPrice = 100 });
            _viewModel.OrderItems.Add(new OrderItem { TotalPrice = 200 });

            var subtotal = _viewModel.GetSubtotal();

            Assert.Equal(300, subtotal);
        }

        [Fact]
        public void Subtotal_WhenNoOrderItems_ReturnsZero()
        {
            var subtotal = _viewModel.GetSubtotal();
            Assert.Equal(0, subtotal);
        }
    }
}