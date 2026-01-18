using MyShop.Data.Models;

namespace MyShop.Tests.Contracts
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync(int page, int pageSize, int? categoryId, double? minPrice, double? maxPrice, string? search, string? sortBy, string? token);
        Task<Product?> GetProductByIdAsync(int productId, string? token);
        Task<int> GetTotalProductCountAsync(int? categoryId, double? minPrice, double? maxPrice, string? search, string? token);
    }
}