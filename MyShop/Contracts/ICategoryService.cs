using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Data.Models;

namespace MyShop.Contracts
{
    public interface ICategoryService
    {
        /// <summary>
        /// Get paginated list of categories
        /// </summary>
        Task<List<Category>> GetCategoriesAsync(int page, int pageSize, string? search, string? token);

        /// <summary>
        /// Get category by ID
        /// </summary>
        Task<Category?> GetCategoryByIdAsync(int categoryId, string? token);

        /// <summary>
        /// Get total count of categories (for pagination)
        /// </summary>
        Task<int> GetTotalCategoryCountAsync(string? search, string? token);

        // TODO: Add mutations when backend is ready
        // Task<Category?> CreateCategoryAsync(CreateCategoryInput input, string? token);
        // Task<Category?> UpdateCategoryAsync(int categoryId, UpdateCategoryInput input, string? token);
        // Task<bool> DeleteCategoryAsync(int categoryId, string? token);
    }
}
