namespace MyShop.Data.Models
{
    /// <summary>
    /// Represents a product image with URL and metadata
    /// </summary>
    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int Position { get; set; }
        public bool IsPrimary { get; set; }

        // Navigation property
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
