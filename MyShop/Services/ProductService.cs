using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services.GraphQL;
using System.Diagnostics;

namespace MyShop.Services
{
    public class ProductService : IProductService
    {
        private readonly GraphQLClient _graphQLClient;

        public ProductService(GraphQLClient graphQLClient)
        {
            _graphQLClient = graphQLClient;
        }

        public async Task<List<Product>> GetProductsAsync(
            int page,
            int pageSize,
            int? categoryId,
            double? minPrice,
            double? maxPrice,
            string? search,
            string? sortBy,
            string? token)
        {
            // Build ListParams for backend
            // NOTE: Backend currently supports: search, page, limit
            // TODO: When backend adds support, include categoryId, minPrice, maxPrice, sortBy
            var paramsInput = $@"
                params: {{
                    page: {page}
                    limit: {pageSize}
                    {(string.IsNullOrEmpty(search) ? "" : $"search: \"{search}\"")}
                }}";

            var query = $@"
                query {{
                    products({paramsInput}) {{
                        items {{
                            productId
                            sku
                            name
                            description
                            importPrice
                            count
                            imageUrl1
                            imageUrl2
                            imageUrl3
                            category {{
                                categoryId
                                name
                            }}
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
                Debug.WriteLine("[PRODUCT] FETCHING PRODUCTS");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[PRODUCT] Page: {page}, PageSize: {pageSize}");
                Debug.WriteLine($"[PRODUCT] Search: {search ?? "none"}");
                Debug.WriteLine($"[PRODUCT] Category: {categoryId?.ToString() ?? "all"} (TODO: backend filter)");
                Debug.WriteLine($"[PRODUCT] Price Range: {minPrice?.ToString() ?? "any"} - {maxPrice?.ToString() ?? "any"} (TODO: backend filter)");
                Debug.WriteLine($"[PRODUCT] Sort By: {sortBy ?? "none"} (TODO: backend sort)");
                Debug.WriteLine("[PRODUCT] Query:");
                Debug.WriteLine(query);
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<PaginatedProductsResponse>(query, null, token);

                Debug.WriteLine($"[PRODUCT] Response: {(response?.Products?.Items != null ? $"✓ {response.Products.Items.Length} products" : "✗ No products")}");

                if (response?.Products?.Items != null)
                {
                    var products = response.Products.Items.Select(MapToProduct).ToList();

                    // TODO: Remove client-side filtering when backend supports these parameters
                    // NOTE: Client-side filtering affects pagination accuracy!
                    // When filters are active, we fetch more items and filter locally
                    
                    var hasClientSideFilters = categoryId.HasValue || minPrice.HasValue || maxPrice.HasValue;
                    
                    // Apply client-side category filter
                    if (categoryId.HasValue)
                    {
                        Debug.WriteLine($"[PRODUCT] Applying client-side category filter: {categoryId.Value}");
                        products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
                    }

                    // Apply client-side price range filter
                    if (minPrice.HasValue || maxPrice.HasValue)
                    {
                        Debug.WriteLine($"[PRODUCT] Applying client-side price filter: {minPrice} - {maxPrice}");
                        products = products.Where(p =>
                        {
                            if (minPrice.HasValue && p.ImportPrice < minPrice.Value) return false;
                            if (maxPrice.HasValue && p.ImportPrice > maxPrice.Value) return false;
                            return true;
                        }).ToList();
                    }

                    // TODO: Remove client-side sorting when backend supports it
                    // Apply client-side sorting
                    if (!string.IsNullOrEmpty(sortBy))
                    {
                        Debug.WriteLine($"[PRODUCT] Applying client-side sort: {sortBy}");
                        products = sortBy.ToLower() switch
                        {
                            "name" => products.OrderBy(p => p.Name).ToList(),
                            "price" => products.OrderBy(p => p.ImportPrice).ToList(),
                            "stock" => products.OrderBy(p => p.Count).ToList(),
                            "sku" => products.OrderBy(p => p.Sku).ToList(),
                            _ => products
                        };
                    }

                    Debug.WriteLine($"[PRODUCT] After client-side filters: {products.Count} products");
                    
                    if (hasClientSideFilters)
                    {
                        Debug.WriteLine("[PRODUCT] ⚠ Warning: Client-side filtering active. Pagination may be inaccurate.");
                        Debug.WriteLine("[PRODUCT] TODO: Backend should support categoryId, minPrice, maxPrice parameters");
                    }
                    
                    Debug.WriteLine("════════════════════════════════════════");

                    return products;
                }

                Debug.WriteLine("════════════════════════════════════════");
                return new List<Product>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error fetching products: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductByIdAsync(int productId, string? token)
        {
            // Query all products and filter client-side
            var query = @"
                query {
                    products {
                        productId
                        sku
                        name
                        description
                        importPrice
                        count
                        imageUrl1
                        imageUrl2
                        imageUrl3
                        category {
                            categoryId
                            name
                        }
                    }
                }
            ";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[PRODUCT] FETCHING PRODUCT #{productId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<ProductsResponse>(query, null, token);

                if (response?.Products != null)
                {
                    var productData = response.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (productData != null)
                    {
                        Debug.WriteLine($"[PRODUCT] ✓ Product #{productId} found");
                        Debug.WriteLine("════════════════════════════════════════");
                        return MapToProduct(productData);
                    }
                }

                Debug.WriteLine($"[PRODUCT] ✗ Product #{productId} not found");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error fetching product: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<int> GetTotalProductCountAsync(
            int? categoryId,
            double? minPrice,
            double? maxPrice,
            string? search,
            string? token)
        {
            // Fetch first page to get pagination info with total count
            // NOTE: Backend search is supported, but category/price filters are TODO
            var paramsInput = $@"
                params: {{
                    page: 1
                    limit: 1
                    {(string.IsNullOrEmpty(search) ? "" : $"search: \"{search}\"")}
                }}";

            var query = $@"
                query {{
                    products({paramsInput}) {{
                        pagination {{
                            totalCount
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine($"[PRODUCT] Getting total count (search: {search ?? "none"})");

                var response = await _graphQLClient.QueryAsync<PaginatedProductsResponse>(query, null, token);

                var count = response?.Products?.Pagination?.TotalCount ?? 0;
                
                // TODO: When backend supports category/price filters, remove this note
                // Currently we can only get search-filtered count from backend
                // Category and price filtering happens client-side, so total count may be inaccurate
                Debug.WriteLine($"[PRODUCT] Total count from backend: {count}");
                Debug.WriteLine("[PRODUCT] Note: Category/Price filters not applied to count (TODO: backend support)");

                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error getting total count: {ex.Message}");
                return 0;
            }
        }

        private Product MapToProduct(ProductData data)
        {
            return new Product
            {
                ProductId = data.ProductId,
                Name = data.Name ?? string.Empty,
                Sku = data.Sku ?? string.Empty,
                ImportPrice = data.ImportPrice,
                Count = data.Count,
                Description = data.Description ?? string.Empty,
                ImageUrl1 = data.ImageUrl1 ?? string.Empty,
                ImageUrl2 = data.ImageUrl2 ?? string.Empty,
                ImageUrl3 = data.ImageUrl3 ?? string.Empty,
                CategoryId = data.Category?.CategoryId ?? 0,
                // Populate Category navigation property for UI binding
                Category = data.Category != null ? new Category
                {
                    CategoryId = data.Category.CategoryId,
                    Name = data.Category.Name ?? string.Empty,
                    Description = data.Category.Description ?? string.Empty
                } : null
            };
        }
    }
}