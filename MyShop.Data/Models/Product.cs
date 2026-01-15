using System.Linq;
using System.Collections.Generic;

namespace MyShop.Data.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ImportPrice { get; set; }
        public int Count { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        // Navigation properties
        public Category? Category { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        
        /// <summary>
        /// Collection of product images
        /// </summary>
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // Helper properties for UI binding (computed from Images collection)
        
        /// <summary>
        /// Gets or sets the primary image URL (first by position or marked as primary)
        /// </summary>
        public string PrimaryImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets all image URLs sorted by position
        /// </summary>
        public List<string> ImageUrls => Images
            .OrderBy(i => i.Position)
            .Select(i => i.Url)
            .ToList();
    }
}