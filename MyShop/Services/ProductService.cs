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
            // Build ProductListParams for backend
            // Backend NOW supports: search, page, limit, sortBy, sortOrder, minPrice, maxPrice
            // NOTE: categoryId is NOT supported by backend - we'll filter client-side
            
            var paramsBuilder = new List<string>
            {
                $"page: {page}",
                $"limit: {pageSize}"
            };

            if (!string.IsNullOrEmpty(search))
            {
                paramsBuilder.Add($"search: \"{search}\"");
            }

            // ✅ Backend supports price filtering now!
            if (minPrice.HasValue)
            {
                paramsBuilder.Add($"minPrice: {(int)minPrice.Value}");
            }

            if (maxPrice.HasValue)
            {
                paramsBuilder.Add($"maxPrice: {(int)maxPrice.Value}");
            }

            // ✅ Backend supports sorting now!
            if (!string.IsNullOrEmpty(sortBy))
            {
                var (backendSortBy, backendSortOrder) = MapSortToBackend(sortBy);
                if (backendSortBy != null)
                {
                    paramsBuilder.Add($"sortBy: {backendSortBy}");
                    paramsBuilder.Add($"sortOrder: {backendSortOrder}");
                }
            }

            var paramsInput = string.Join("\n                    ", paramsBuilder);

            var query = $@"
                query {{
                    products(params: {{
                    {paramsInput}
                    }}) {{
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
                                description
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
                Debug.WriteLine($"[PRODUCT] Price Range: {minPrice?.ToString() ?? "any"} - {maxPrice?.ToString() ?? "any"} ✅ Backend");
                Debug.WriteLine($"[PRODUCT] Sort By: {sortBy ?? "none"} ✅ Backend");
                Debug.WriteLine($"[PRODUCT] Category: {categoryId?.ToString() ?? "all"} ⚠ Client-side (TODO: backend)");
                Debug.WriteLine("[PRODUCT] Query:");
                Debug.WriteLine(query);
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<PaginatedProductsResponse>(query, null, token);

                Debug.WriteLine($"[PRODUCT] Response: {(response?.Products?.Items != null ? $"✓ {response.Products.Items.Length} products" : "✗ No products")}");

                if (response?.Products?.Items != null)
                {
                    var products = response.Products.Items.Select(MapToProduct).ToList();

                    // TODO: Remove client-side category filtering when backend supports categoryId parameter
                    // NOTE: Price filtering and sorting are now handled by backend!
                    if (categoryId.HasValue)
                    {
                        Debug.WriteLine($"[PRODUCT] Applying client-side category filter: {categoryId.Value}");
                        products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
                        Debug.WriteLine($"[PRODUCT] After category filter: {products.Count} products");
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
            // NOTE: Backend now supports price filters, but not categoryId
            var paramsBuilder = new List<string>
            {
                "page: 1",
                "limit: 1"
            };

            if (!string.IsNullOrEmpty(search))
            {
                paramsBuilder.Add($"search: \"{search}\"");
            }

            // ✅ Backend supports price filtering
            if (minPrice.HasValue)
            {
                paramsBuilder.Add($"minPrice: {(int)minPrice.Value}");
            }

            if (maxPrice.HasValue)
            {
                paramsBuilder.Add($"maxPrice: {(int)maxPrice.Value}");
            }

            var paramsInput = string.Join("\n                    ", paramsBuilder);

            var query = $@"
                query {{
                    products(params: {{
                    {paramsInput}
                    }}) {{
                        pagination {{
                            totalCount
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine($"[PRODUCT] Getting total count (search: {search ?? "none"}, price: {minPrice}-{maxPrice})");

                var response = await _graphQLClient.QueryAsync<PaginatedProductsResponse>(query, null, token);

                var count = response?.Products?.Pagination?.TotalCount ?? 0;
                
                // TODO: When backend supports categoryId filter, total count will be accurate
                // Currently categoryId filtering happens client-side after fetching
                if (categoryId.HasValue)
                {
                    Debug.WriteLine($"[PRODUCT] Note: Category filter (ID={categoryId}) not applied to count (client-side only)");
                }

                Debug.WriteLine($"[PRODUCT] Total count from backend: {count}");

                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error getting total count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Map UI sort criteria to backend enum values
        /// Returns (sortBy, sortOrder) tuple
        /// </summary>
        private (string? sortBy, string sortOrder) MapSortToBackend(string? uiSortCriteria)
        {
            if (string.IsNullOrEmpty(uiSortCriteria) || uiSortCriteria == "None")
                return (null, "ASC");

            // Map UI sort options to backend ProductSortBy enum
            return uiSortCriteria switch
            {
                "Name (A-Z)" => ("NAME", "ASC"),
                "Price (Low to High)" => ("IMPORT_PRICE", "ASC"),
                "Stock (Low to High)" => ("COUNT", "ASC"),
                "SKU" => ("PRODUCT_ID", "ASC"),
                _ => (null, "ASC")
            };
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

        private Order MapToOrder(OrderData data)
        {
            return new Order
            {
                OrderId = data.OrderId,
                CreatedTime = DateTime.TryParse(data.CreatedTime, out var dt) ? dt : DateTime.MinValue,
                FinalPrice = data.FinalPrice,
                Status = data.Status ?? string.Empty,
                OrderItems = data.OrderItems != null
                    ? data.OrderItems.Select(MapToOrderItem).ToList()
                    : new List<OrderItem>()
            };
        }

        private OrderItem MapToOrderItem(OrderItemData data)
        {
            return new OrderItem
            {
                OrderItemId = data.OrderItemId,
                ProductId = data.Product?.ProductId ?? 0,
                Quantity = data.Quantity,
                TotalPrice = data.TotalPrice,
                UnitSalePrice = data.UnitSalePrice,
                Product = data.Product != null
                    ? new Product
                    {
                        ProductId = data.Product.ProductId,
                        Sku = data.Product.Sku ?? string.Empty,
                        Name = data.Product.Name ?? string.Empty,
                        ImportPrice = data.Product.ImportPrice,
                        Count = data.Product.Count
                    }
                    : null
            };
        }
    }
}