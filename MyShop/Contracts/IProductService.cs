using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Data.Models;

namespace MyShop.Contracts
{
    public interface IProductService
    {
        /// <summary>
        /// Get paginated list of products with filtering and sorting
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="categoryId">Filter by category ID (⚠ TODO: pending backend support - currently client-side)</param>
        /// <param name="minPrice">Minimum price filter (✅ backend supported)</param>
        /// <param name="maxPrice">Maximum price filter (✅ backend supported)</param>
        /// <param name="search">Search keyword in product name (✅ backend supported)</param>
        /// <param name="sortBy">Sort criteria (✅ backend supported)</param>
        /// <param name="token">Authentication token</param>
        Task<List<Product>> GetProductsAsync(
            int page,
            int pageSize,
            int? categoryId,
            double? minPrice,
            double? maxPrice,
            string? search,
            string? sortBy,
            string? token);

        /// <summary>
        /// Get product by ID
        /// </summary>
        Task<Product?> GetProductByIdAsync(int productId, string? token);

        /// <summary>
        /// Get total count of products matching filters (for pagination)
        /// </summary>
        Task<int> GetTotalProductCountAsync(
            int? categoryId,
            double? minPrice,
            double? maxPrice,
            string? search,
            string? token);

        /// <summary>
        /// Create a new product
        /// </summary>
        Task<Product?> CreateProductAsync(
            string sku, string name, int importPrice, int count, 
            string? description, List<ProductImageInput> images, 
            int categoryId, string? token);

        /// <summary>
        /// Update an existing product
        /// </summary>
        Task<Product?> UpdateProductAsync(
            int productId, string? sku, string? name, int? importPrice, 
            int? count, string? description, List<ProductImageInput>? images, 
            int? categoryId, string? token);

        /// <summary>
        /// Delete a product by ID
        /// </summary>
        Task<bool> DeleteProductAsync(int productId, string? token);

        /// <summary>
        /// Bulk import products from Excel file (Base64 encoded)
        /// </summary>
        Task<BulkImportResult> BulkImportProductsAsync(string fileBase64, string? token);
    }

    public class ProductImageInput
    {
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int? Position { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class BulkImportResult
    {
        public int CreatedCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkImportError> Errors { get; set; } = new();
    }

    public class BulkImportError
    {
        public int Row { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
    }
}