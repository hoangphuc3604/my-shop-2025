namespace MyShop.Data.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime CreatedTime { get; set; }
        public int FinalPrice { get; set; }
        public string Status { get; set; } = "Created";

        // Navigation property
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}