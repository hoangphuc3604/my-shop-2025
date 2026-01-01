using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Data.Models;

namespace MyShop.Contracts
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync(string? token);
        Task<Product?> GetProductByIdAsync(int productId, string? token);
    }
}