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
    public class OrderService : IOrderService
    {
        private readonly GraphQLClient _graphQLClient;

        public OrderService(GraphQLClient graphQLClient)
        {
            _graphQLClient = graphQLClient;
        }

        public async Task<List<Order>> GetOrdersAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? token, string? sortBy = null, string? sortOrder = null)
        {
            var query = @"
                query Orders($params: ListParams) {
                    orders(params: $params) {
                        items {
                            status
                            createdTime
                            finalPrice
                            orderId
                            orderItems {
                                orderItemId
                                quantity
                                unitSalePrice
                                totalPrice
                                product {
                                    sku
                                    productId
                                    name
                                    importPrice
                                    count
                                }
                            }
                        }
                        pagination {
                            currentPage
                            hasNextPage
                            hasPrevPage
                            limit
                            totalCount
                            totalPages
                        }
                    }
                }
            ";

            var paramsObject = new
            {
                startDate = fromDate?.ToString("yyyy-MM-dd"),
                endDate = toDate?.ToString("yyyy-MM-dd"),
                search = (string?)null,
                page,
                limit = pageSize,
                sortBy = sortBy ?? "CREATED_TIME",
                sortOrder = sortOrder ?? "ASC"
            };

            var variables = new
            {
                @params = paramsObject
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[ORDER] FETCHING PAGINATED ORDERS");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] Page: {page}, Page Size: {pageSize}");
                if (fromDate.HasValue || toDate.HasValue)
                {
                    Debug.WriteLine($"[ORDER] Date Range: {fromDate?.ToString("yyyy-MM-dd")} to {toDate?.ToString("yyyy-MM-dd")}");
                }
                Debug.WriteLine($"[ORDER] Sort: {sortBy ?? "CREATED_TIME"} ({sortOrder ?? "ASC"})");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<PaginatedOrdersResponse>(query, variables, token);

                if (response?.Orders?.Items != null)
                {
                    var orders = response.Orders.Items.Select(MapToOrder).ToList();
                    Debug.WriteLine($"[ORDER] ✓ Retrieved {orders.Count} orders");
                    Debug.WriteLine($"[ORDER] Pagination: Page {response.Orders.Pagination.CurrentPage}/{response.Orders.Pagination.TotalPages}");
                    Debug.WriteLine("════════════════════════════════════════");
                    return orders;
                }

                Debug.WriteLine("[ORDER] ✗ No orders returned");
                Debug.WriteLine("════════════════════════════════════════");
                return new List<Order>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error fetching orders: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return new List<Order>();
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId, string? token)
        {
            var query = @"
                query Orders($params: ListParams) {
                    orders(params: $params) {
                        items {
                            status
                            createdTime
                            finalPrice
                            orderId
                            orderItems {
                                orderItemId
                                quantity
                                unitSalePrice
                                totalPrice
                                product {
                                    sku
                                    productId
                                    name
                                    importPrice
                                    count
                                }
                            }
                        }
                        pagination {
                            currentPage
                            hasNextPage
                            hasPrevPage
                            limit
                            totalCount
                            totalPages
                        }
                    }
                }
            ";

            var variables = new
            {
                @params = new
                {
                    startDate = (string?)null,
                    endDate = (string?)null,
                    search = (string?)null,
                    page = 1,
                    limit = 1000,
                    sortBy = "CREATED_TIME",
                    sortOrder = "ASC"
                }
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] FETCHING ORDER #{orderId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<PaginatedOrdersResponse>(query, variables, token);

                if (response?.Orders?.Items != null)
                {
                    var orderData = response.Orders.Items.FirstOrDefault(o => o.OrderId == orderId);
                    if (orderData != null)
                    {
                        Debug.WriteLine($"[ORDER] ✓ Order #{orderId} found");
                        Debug.WriteLine("════════════════════════════════════════");
                        return MapToOrder(orderData);
                    }
                }

                Debug.WriteLine($"[ORDER] ✗ Order #{orderId} not found");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error fetching order: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<Order?> CreateOrderAsync(CreateOrderInput input, string? token)
        {
            var query = @"
                mutation Mutation($input: CreateOrderInput!) {
                    addOrder(input: $input) {
                        createdTime
                        finalPrice
                        orderId
                        orderItems {
                            orderId
                            orderItemId
                            product {
                                count
                                productId
                                sku
                                name
                                importPrice
                            }
                            productId
                            quantity
                            totalPrice
                            unitSalePrice
                        }
                        status
                    }
                }
            ";

            var variables = new
            {
                input = new
                {
                    orderItems = input.OrderItems.Select(oi => new
                    {
                        productId = oi.ProductId,
                        quantity = oi.Quantity
                    }).ToArray()
                }
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[ORDER] CREATING ORDER");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] Items Count: {input.OrderItems.Count}");
                foreach (var item in input.OrderItems)
                {
                    Debug.WriteLine($"[ORDER]   - Product {item.ProductId}: Qty {item.Quantity}");
                }
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<CreateOrderResponse>(query, variables, token);

                if (response?.AddOrder != null)
                {
                    Debug.WriteLine($"[ORDER] ✓ Order #{response.AddOrder.OrderId} created");
                    Debug.WriteLine($"[ORDER] Final Price: {response.AddOrder.FinalPrice}");
                    Debug.WriteLine($"[ORDER] Items: {response.AddOrder.OrderItems?.Length ?? 0}");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToOrder(response.AddOrder);
                }

                Debug.WriteLine("[ORDER] ✗ Failed to create order - No response");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error creating order: {ex.Message}");
                Debug.WriteLine($"[ORDER] Stack Trace: {ex.StackTrace}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<Order?> UpdateOrderAsync(int orderId, UpdateOrderInput input, string? token)
        {
            var query = @"
                mutation Mutation($updateOrderId: ID!, $input: UpdateOrderInput!) {
                    updateOrder(id: $updateOrderId, input: $input) {
                        finalPrice
                        status
                        orderItems {
                            orderItemId
                            orderId
                            productId
                            quantity
                            totalPrice
                            unitSalePrice
                            product {
                                count
                                importPrice
                                name
                                productId
                                sku
                            }
                        }
                    }
                }
            ";

            var variables = new
            {
                updateOrderId = orderId.ToString(),
                input = new
                {
                    status = input.Status
                    // REMOVED: orderItems - backend UpdateOrderInput only accepts status
                }
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] UPDATING ORDER #{orderId}");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] New Status: {input.Status}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<UpdateOrderResponse>(query, variables, token);

                if (response?.UpdateOrder != null)
                {
                    Debug.WriteLine($"[ORDER] ✓ Order #{orderId} updated");
                    Debug.WriteLine($"[ORDER] Status: {response.UpdateOrder.Status}");
                    Debug.WriteLine($"[ORDER] Final Price: {response.UpdateOrder.FinalPrice}");
                    Debug.WriteLine($"[ORDER] Items: {response.UpdateOrder.OrderItems?.Length ?? 0}");
                    Debug.WriteLine("════════════════════════════════════════");

                    // Map the update response to Order (set OrderId from parameter since response doesn't include it)
                    return new Order
                    {
                        OrderId = orderId,
                        CreatedTime = DateTime.UtcNow, // Use current time since response doesn't include createdTime
                        FinalPrice = response.UpdateOrder.FinalPrice,
                        Status = response.UpdateOrder.Status ?? "Created",
                        OrderItems = response.UpdateOrder.OrderItems?.Select(MapToOrderItem).ToList() ?? new List<OrderItem>()
                    };
                }

                Debug.WriteLine("[ORDER] ✗ Failed to update order - No response");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error updating order: {ex.Message}");
                Debug.WriteLine($"[ORDER] Stack Trace: {ex.StackTrace}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<bool> DeleteOrderAsync(int orderId, string? token)
        {
            var query = @"
                mutation Mutation($deleteOrderId: ID!) {
                    deleteOrder(id: $deleteOrderId)
                }
            ";

            var variables = new
            {
                deleteOrderId = orderId.ToString()
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] DELETING ORDER #{orderId}");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] Order ID: {orderId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<DeleteOrderSimpleResponse>(query, variables, token);

                // The mutation returns a boolean directly
                var success = response?.DeleteOrder ?? false;
                
                Debug.WriteLine($"[ORDER] {(success ? "✓" : "✗")} Order #{orderId} {(success ? "deleted" : "deletion failed")}");
                Debug.WriteLine("════════════════════════════════════════");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error deleting order: {ex.Message}");
                Debug.WriteLine($"[ORDER] Stack Trace: {ex.StackTrace}");
                Debug.WriteLine("════════════════════════════════════════");
                return false;
            }
        }

        public async Task<int> GetTotalOrderCountAsync(DateTime? fromDate, DateTime? toDate, string? token)
        {
            var query = @"
                query Orders($params: ListParams) {
                    orders(params: $params) {
                        pagination {
                            totalCount
                        }
                    }
                }
            ";

            var variables = new
            {
                @params = new
                {
                    startDate = fromDate?.ToString("yyyy-MM-dd"),
                    endDate = toDate?.ToString("yyyy-MM-dd"),
                    search = (string?)null,
                    page = 1,
                    limit = 1,
                    sortBy = "CREATED_TIME",
                    sortOrder = "ASC"
                }
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[ORDER] COUNTING ORDERS");
                Debug.WriteLine("════════════════════════════════════════");
                if (fromDate.HasValue || toDate.HasValue)
                {
                    Debug.WriteLine($"[ORDER] Date Range: {fromDate?.ToString("yyyy-MM-dd")} to {toDate?.ToString("yyyy-MM-dd")}");
                }

                var response = await _graphQLClient.QueryAsync<PaginatedOrdersResponse>(query, variables, token);

                var count = response?.Orders?.Pagination?.TotalCount ?? 0;
                Debug.WriteLine($"[ORDER] ✓ Total orders: {count}");
                Debug.WriteLine("════════════════════════════════════════");
                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error counting orders: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return 0;
            }
        }

        private Order MapToOrder(OrderData data)
        {
            return new Order
            {
                OrderId = data.OrderId,
                CreatedTime = DateTime.Parse(data.CreatedTime ?? DateTime.UtcNow.ToString("o")),
                FinalPrice = data.FinalPrice,
                Status = data.Status ?? "Created",
                OrderItems = data.OrderItems?.Select(MapToOrderItem).ToList() ?? new List<OrderItem>()
            };
        }

        private OrderItem MapToOrderItem(OrderItemData data)
        {
            return new OrderItem
            {
                OrderItemId = data.OrderItemId,
                Quantity = data.Quantity,
                UnitSalePrice = (int)data.UnitSalePrice,
                TotalPrice = data.TotalPrice,
                Product = data.Product != null ? MapToProduct(data.Product) : null
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
                ImageUrl3 = data.ImageUrl3 ?? string.Empty
            };
        }
    }
}