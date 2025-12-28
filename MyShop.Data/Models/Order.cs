namespace MyShop.Data.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime CreatedTime { get; set; }
        public int FinalPrice { get; set; }

        // Navigation property
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}