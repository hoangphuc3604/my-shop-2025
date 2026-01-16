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
        private readonly IAuthorizationService _authorizationService;

        public ProductService(GraphQLClient graphQLClient, IAuthorizationService authorizationService)
        {
            _graphQLClient = graphQLClient;
            _authorizationService = authorizationService;
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
            // Backend NOW supports: search, page, limit, sortBy, sortOrder, minPrice, maxPrice, categoryId
            
            var paramsBuilder = new List<string>
            {
                $"page: {page}",
                $"limit: {pageSize}"
            };

            // ✅ Backend supports category filtering now!
            if (categoryId.HasValue)
            {
                paramsBuilder.Add($"categoryId: {categoryId.Value}");
            }

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
                            images {{
                                url
                                altText
                                position
                                isPrimary
                            }}
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
                Debug.WriteLine($"[PRODUCT] Category: {categoryId?.ToString() ?? "all"} ✅ Backend");
                Debug.WriteLine("[PRODUCT] Query:");
                Debug.WriteLine(query);
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<PaginatedProductsResponse>(query, null, token);

                Debug.WriteLine($"[PRODUCT] Response: {(response?.Products?.Items != null ? $"✓ {response.Products.Items.Length} products" : "✗ No products")}");

                if (response?.Products?.Items != null)
                {
                    var products = response.Products.Items.Select(MapToProduct).ToList();

                    // ✅ Category filtering now handled by backend!
                    Debug.WriteLine($"[PRODUCT] Received {products.Count} products from backend");
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
            // Backend now supports all filters including categoryId
            var paramsBuilder = new List<string>
            {
                "page: 1",
                "limit: 1"
            };

            // ✅ Backend supports category filtering
            if (categoryId.HasValue)
            {
                paramsBuilder.Add($"categoryId: {categoryId.Value}");
            }

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
                Debug.WriteLine($"[PRODUCT] Getting total count (category: {categoryId?.ToString() ?? "all"}, search: {search ?? "none"}, price: {minPrice}-{maxPrice})");

                var response = await _graphQLClient.QueryAsync<PaginatedProductsResponse>(query, null, token);

                var count = response?.Products?.Pagination?.TotalCount ?? 0;

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
                "Name (Z-A)" => ("NAME", "DESC"),
                "Price (Low to High)" => ("IMPORT_PRICE", "ASC"),
                "Price (High to Low)" => ("IMPORT_PRICE", "DESC"),
                "Stock (Low to High)" => ("COUNT", "ASC"),
                "Stock (High to Low)" => ("COUNT", "DESC"),
                "Newest First" => ("CREATED_AT", "DESC"),
                "Oldest First" => ("CREATED_AT", "ASC"),
                _ => (null, "ASC")
            };
        }

        private Product MapToProduct(ProductData data)
        {
            // Map images from GraphQL response to ProductImage entities
            var images = data.Images?
                .Select(img => new ProductImage
                {
                    ProductImageId = img.ProductImageId,
                    Url = img.Url ?? string.Empty,
                    AltText = img.AltText,
                    Position = img.Position,
                    IsPrimary = img.IsPrimary
                })
                .ToList() ?? new List<ProductImage>();

            // Calculate primary image URL
            var primaryImageUrl = images
                .OrderBy(i => i.Position)
                .FirstOrDefault(i => i.IsPrimary)?.Url 
                ?? images.OrderBy(i => i.Position).FirstOrDefault()?.Url 
                ?? string.Empty;

            return new Product
            {
                ProductId = data.ProductId,
                Name = data.Name ?? string.Empty,
                Sku = data.Sku ?? string.Empty,
                ImportPrice = data.ImportPrice ?? 0,
                Count = data.Count,
                Description = data.Description ?? string.Empty,
                CategoryId = data.Category?.CategoryId ?? 0,
                Images = images,
                PrimaryImageUrl = primaryImageUrl,
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
                AppliedPromotionCode = data.AppliedPromotionCode,
                DiscountAmount = data.DiscountAmount,
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
                        ImportPrice = data.Product.ImportPrice ?? 0,
                        Count = data.Product.Count
                    }
                    : null
            };
        }

        public async Task<Product?> CreateProductAsync(
            string sku, string name, int importPrice, int count,
            string? description, List<ProductImageInput> images,
            int categoryId, string? token)
        {
            if (!_authorizationService.HasPermission("CREATE_PRODUCTS"))
            {
                throw new UnauthorizedAccessException("You do not have permission to create products");
            }

            // Build images array
            var imagesJson = string.Join(",\n", images.Select((img, idx) => $@"{{
                url: ""{img.Url}""
                altText: ""{img.AltText ?? ""}""
                position: {img.Position ?? idx}
                isPrimary: {(img.IsPrimary ? "true" : "false")}
            }}"));

            var descriptionParam = description != null ? $"description: \"{description}\"" : "";

            var mutation = $@"
                mutation {{
                    createProduct(input: {{
                        sku: ""{sku}""
                        name: ""{name}""
                        importPrice: {importPrice}
                        count: {count}
                        {descriptionParam}
                        categoryId: {categoryId}
                        images: [{imagesJson}]
                    }}) {{
                        productId
                        sku
                        name
                        importPrice
                        count
                        description
                        categoryId
                        images {{
                            productImageId
                            url
                            altText
                            position
                            isPrimary
                        }}
                        category {{
                            categoryId
                            name
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[PRODUCT] CREATING PRODUCT");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[PRODUCT] SKU: {sku}, Name: {name}, Price: {importPrice}, Count: {count}");

                var response = await _graphQLClient.QueryAsync<CreateProductResponse>(mutation, null, token);

                if (response?.CreateProduct != null)
                {
                    Debug.WriteLine($"[PRODUCT] ✓ Created product #{response.CreateProduct.ProductId}");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToProduct(response.CreateProduct);
                }

                Debug.WriteLine("[PRODUCT] ✗ Failed to create product");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error creating product: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        public async Task<Product?> UpdateProductAsync(
            int productId, string? sku, string? name, int? importPrice,
            int? count, string? description, List<ProductImageInput>? images,
            int? categoryId, string? token)
        {
            if (!_authorizationService.HasPermission("UPDATE_PRODUCTS"))
            {
                throw new UnauthorizedAccessException("You do not have permission to update products");
            }

            var inputParams = new List<string>();

            if (sku != null) inputParams.Add($"sku: \"{sku}\"");
            if (name != null) inputParams.Add($"name: \"{name}\"");
            if (importPrice.HasValue) inputParams.Add($"importPrice: {importPrice.Value}");
            if (count.HasValue) inputParams.Add($"count: {count.Value}");
            if (description != null) inputParams.Add($"description: \"{description}\"");
            if (categoryId.HasValue) inputParams.Add($"categoryId: {categoryId.Value}");

            if (images != null && images.Count > 0)
            {
                var imagesJson = string.Join(",\n", images.Select((img, idx) => $@"{{
                    url: ""{img.Url}""
                    altText: ""{img.AltText ?? ""}""
                    position: {img.Position ?? idx}
                    isPrimary: {(img.IsPrimary ? "true" : "false")}
                }}"));
                inputParams.Add($"images: [{imagesJson}]");
            }

            var inputJoined = string.Join("\n", inputParams);

            var mutation = $@"
                mutation {{
                    updateProduct(id: ""{productId}"", input: {{
                        {inputJoined}
                    }}) {{
                        productId
                        sku
                        name
                        importPrice
                        count
                        description
                        categoryId
                        images {{
                            productImageId
                            url
                            altText
                            position
                            isPrimary
                        }}
                        category {{
                            categoryId
                            name
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[PRODUCT] UPDATING PRODUCT #{productId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<UpdateProductResponse>(mutation, null, token);

                if (response?.UpdateProduct != null)
                {
                    Debug.WriteLine($"[PRODUCT] ✓ Updated product #{productId}");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToProduct(response.UpdateProduct);
                }

                Debug.WriteLine($"[PRODUCT] ✗ Failed to update product #{productId}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error updating product: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId, string? token)
        {
            if (!_authorizationService.HasPermission("DELETE_PRODUCTS"))
            {
                throw new UnauthorizedAccessException("You do not have permission to delete products");
            }

            var mutation = $@"
                mutation {{
                    deleteProduct(id: ""{productId}"")
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[PRODUCT] DELETING PRODUCT #{productId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<DeleteProductResponse>(mutation, null, token);

                var success = response?.DeleteProduct ?? false;
                Debug.WriteLine($"[PRODUCT] {(success ? "✓ Deleted" : "✗ Failed to delete")} product #{productId}");
                Debug.WriteLine("════════════════════════════════════════");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error deleting product: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        public async Task<BulkImportResult> BulkImportProductsAsync(string fileBase64, string? token)
        {
            var mutation = $@"
                mutation {{
                    bulkCreateProducts(fileBase64: ""{fileBase64}"") {{
                        createdCount
                        failedCount
                        errors {{
                            row
                            message
                            field
                        }}
                    }}
                }}";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[PRODUCT] BULK IMPORTING PRODUCTS");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[PRODUCT] File size: {fileBase64.Length} chars (Base64)");

                var response = await _graphQLClient.QueryAsync<BulkCreateProductsResponse>(mutation, null, token);

                var result = new BulkImportResult();

                if (response?.BulkCreateProducts != null)
                {
                    result.CreatedCount = response.BulkCreateProducts.CreatedCount;
                    result.FailedCount = response.BulkCreateProducts.FailedCount;
                    
                    if (response.BulkCreateProducts.Errors != null)
                    {
                        result.Errors = response.BulkCreateProducts.Errors
                            .Select(e => new BulkImportError
                            {
                                Row = e.Row,
                                Message = e.Message ?? "",
                                Field = e.Field
                            })
                            .ToList();
                    }

                    Debug.WriteLine($"[PRODUCT] ✓ Bulk import completed: {result.CreatedCount} created, {result.FailedCount} failed");
                }
                else
                {
                    Debug.WriteLine("[PRODUCT] ✗ Bulk import returned null response");
                }

                Debug.WriteLine("════════════════════════════════════════");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error bulk importing: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        public async Task<TemplateFile?> DownloadTemplateAsync(string? token)
        {
            var query = @"
                query {
                    productTemplate {
                        fileBase64
                        filename
                        mimeType
                    }
                }";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[PRODUCT] DOWNLOADING TEMPLATE");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<ProductTemplateResponse>(query, null, token);

                if (response?.ProductTemplate != null)
                {
                    Debug.WriteLine($"[PRODUCT] ✓ Downloaded template: {response.ProductTemplate.Filename}");
                    Debug.WriteLine("════════════════════════════════════════");
                    
                    return new TemplateFile
                    {
                        FileBase64 = response.ProductTemplate.FileBase64 ?? "",
                        Filename = response.ProductTemplate.Filename ?? "ProductTemplate.xlsx",
                        MimeType = response.ProductTemplate.MimeType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    };
                }

                Debug.WriteLine("[PRODUCT] ✗ Failed to download template");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT] ✗ Error downloading template: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }
    }
}