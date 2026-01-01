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

        public async Task<List<Product>> GetProductsAsync(string? token)
        {
            // Match backend schema exactly
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
                Debug.WriteLine("[PRODUCT] FETCHING PRODUCTS");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[PRODUCT] Query:");
                Debug.WriteLine(query);
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<ProductsResponse>(query, null, token);

                Debug.WriteLine($"[PRODUCT] Response: {(response?.Products != null ? $"✓ {response.Products.Length} products" : "✗ No products")}");
                Debug.WriteLine("════════════════════════════════════════");

                if (response?.Products != null)
                {
                    return response.Products.Select(MapToProduct).ToList();
                }

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
                CategoryId = data.Category?.CategoryId ?? 0
            };
        }
    }
}