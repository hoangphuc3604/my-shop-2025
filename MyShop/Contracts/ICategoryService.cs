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

        /// <summary>
        /// Create a new category
        /// </summary>
        Task<Category?> CreateCategoryAsync(string name, string? description, string? token);

        /// <summary>
        /// Delete a category by ID
        /// </summary>
        Task<bool> DeleteCategoryAsync(int categoryId, string? token);
    }
}
