using MyShop.Data.Models;

namespace MyShop.Tests.Contracts
{
    public interface IOrderService
    {
        Task<List<Order>> GetOrdersAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? token, string? sortBy = null, string? sortOrder = null);
        Task<Order?> GetOrderByIdAsync(int orderId, string? token);
        Task<Order?> CreateOrderAsync(CreateOrderInput input, string? token);
        Task<Order?> UpdateOrderAsync(int orderId, UpdateOrderInput input, string? token);
        Task<bool> DeleteOrderAsync(int orderId, string? token);
        Task<int> GetTotalOrderCountAsync(DateTime? fromDate, DateTime? toDate, string? token);
    }

    public class CreateOrderInput
    {
        public List<OrderItemInput> OrderItems { get; set; } = new();
        public string? PromotionCode { get; set; }
    }

    public class OrderItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderInput
    {
        public string? Status { get; set; }
        public List<OrderItemUpdateInput>? OrderItems { get; set; }
        public string? PromotionCode { get; set; }
    }

    public class OrderItemUpdateInput
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
    }
}