using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services.GraphQL;

namespace MyShop.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly GraphQLClient _graphQLClient;

        public CategoryService(GraphQLClient graphQLClient)
        {
            _graphQLClient = graphQLClient;
        }

        public async Task<List<Category>> GetCategoriesAsync(int page, int pageSize, string? search, string? token)
        {
            // Build ListParams for pagination and search
            var paramsInput = $@"
                params: {{
                    page: {page}
                    limit: {pageSize}
                    {(string.IsNullOrEmpty(search) ? "" : $"search: \"{search}\"")}
                }}";

            var query = $@"
                query {{
                    categories({paramsInput}) {{
                        items {{
                            categoryId
                            name
                            description
                        }}
                        pagination {{
                            totalCount
                            currentPage
                            totalPages
                            limit
                            hasNextPage
                            hasPrevPage
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[CATEGORY] FETCHING CATEGORIES");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[CATEGORY] Page: {page}, PageSize: {pageSize}, Search: {search ?? "none"}");
                Debug.WriteLine("[CATEGORY] Query:");
                Debug.WriteLine(query);
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<PaginatedCategoriesResponse>(query, null, token);

                Debug.WriteLine($"[CATEGORY] Response: {(response?.Categories?.Items != null ? $"✓ {response.Categories.Items.Length} categories" : "✗ No categories")}");
                Debug.WriteLine("════════════════════════════════════════");

                if (response?.Categories?.Items != null)
                {
                    return response.Categories.Items.Select(MapToCategory).ToList();
                }

                return new List<Category>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CATEGORY] ✗ Error fetching categories: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return new List<Category>();
            }
        }

        public async Task<Category?> GetCategoryByIdAsync(int categoryId, string? token)
        {
            var query = $@"
                query {{
                    category(id: {categoryId}) {{
                        categoryId
                        name
                        description
                    }}
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[CATEGORY] FETCHING CATEGORY #{categoryId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<CategoryDetailResponse>(query, null, token);

                if (response?.Category != null)
                {
                    Debug.WriteLine($"[CATEGORY] ✓ Category #{categoryId} found");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToCategory(response.Category);
                }

                Debug.WriteLine($"[CATEGORY] ✗ Category #{categoryId} not found");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CATEGORY] ✗ Error fetching category: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<int> GetTotalCategoryCountAsync(string? search, string? token)
        {
            // Fetch first page to get pagination info with total count
            var paramsInput = $@"
                params: {{
                    page: 1
                    limit: 1
                    {(string.IsNullOrEmpty(search) ? "" : $"search: \"{search}\"")}
                }}";

            var query = $@"
                query {{
                    categories({paramsInput}) {{
                        pagination {{
                            totalCount
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine($"[CATEGORY] Getting total count (search: {search ?? "none"})");

                var response = await _graphQLClient.QueryAsync<PaginatedCategoriesResponse>(query, null, token);

                var count = response?.Categories?.Pagination?.TotalCount ?? 0;
                Debug.WriteLine($"[CATEGORY] Total count: {count}");

                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CATEGORY] ✗ Error getting total count: {ex.Message}");
                return 0;
            }
        }

        private Category MapToCategory(CategoryData data)
        {
            return new Category
            {
                CategoryId = data.CategoryId,
                Name = data.Name ?? string.Empty,
                Description = data.Description ?? string.Empty
            };
        }

        public async Task<Category?> CreateCategoryAsync(string name, string? description, string? token)
        {
            var descriptionParam = description != null ? $"description: \"{description}\"" : "";
            
            var mutation = $@"
                mutation {{
                    createCategory(input: {{
                        name: ""{name}""
                        {descriptionParam}
                    }}) {{
                        categoryId
                        name
                        description
                    }}
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[CATEGORY] CREATING CATEGORY");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[CATEGORY] Name: {name}, Description: {description ?? "none"}");

                var response = await _graphQLClient.QueryAsync<CreateCategoryResponse>(mutation, null, token);

                if (response?.CreateCategory != null)
                {
                    Debug.WriteLine($"[CATEGORY] ✓ Created category #{response.CreateCategory.CategoryId}");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToCategory(response.CreateCategory);
                }

                Debug.WriteLine("[CATEGORY] ✗ Failed to create category");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CATEGORY] ✗ Error creating category: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId, string? token)
        {
            var mutation = $@"
                mutation {{
                    deleteCategory(id: ""{categoryId}"")
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[CATEGORY] DELETING CATEGORY #{categoryId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<DeleteCategoryResponse>(mutation, null, token);

                var success = response?.DeleteCategory ?? false;
                Debug.WriteLine($"[CATEGORY] {(success ? "✓ Deleted" : "✗ Failed to delete")} category #{categoryId}");
                Debug.WriteLine("════════════════════════════════════════");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CATEGORY] ✗ Error deleting category: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }
    }
}
