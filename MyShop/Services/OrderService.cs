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

        public async Task<List<Order>> GetOrdersAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? token)
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
                    search = (string?)null,
                    page,
                    limit = pageSize
                }
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[ORDER] FETCHING PAGINATED ORDERS");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] Page: {page}, Page Size: {pageSize}");
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
                    search = (string?)null,
                    page = 1,
                    limit = 1000
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
                mutation {
                    createOrder(input: {
                        orderItems: [" + string.Join(",", input.OrderItems.Select(oi => 
                            $"{{ productId: {oi.ProductId}, quantity: {oi.Quantity} }}")) + @"]
                    }) {
                        orderId
                        createdTime
                        finalPrice
                        status
                        orderItems {
                            orderItemId
                            quantity
                            unitSalePrice
                            totalPrice
                        }
                    }
                }
            ";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[ORDER] CREATING ORDER");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<CreateOrderResponse>(query, null, token);

                if (response?.CreateOrder != null)
                {
                    Debug.WriteLine($"[ORDER] ✓ Order #{response.CreateOrder.OrderId} created");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToOrder(response.CreateOrder);
                }

                Debug.WriteLine("[ORDER] ✗ Failed to create order");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error creating order: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<Order?> UpdateOrderAsync(int orderId, UpdateOrderInput input, string? token)
        {
            var statusPart = !string.IsNullOrEmpty(input.Status) 
                ? $"status: \"{input.Status}\"" 
                : "";

            var itemsPart = input.OrderItems != null && input.OrderItems.Count > 0
                ? $"orderItems: [{string.Join(",", input.OrderItems.Select(oi => $"{{ orderItemId: {oi.OrderItemId}, quantity: {oi.Quantity} }}"))}]"
                : "";

            var inputParts = new List<string>();
            if (!string.IsNullOrEmpty(statusPart))
                inputParts.Add(statusPart);
            if (!string.IsNullOrEmpty(itemsPart))
                inputParts.Add(itemsPart);

            var query = @"
                mutation {
                    updateOrder(orderId: " + orderId + @", input: {
                        " + string.Join(", ", inputParts) + @"
                    }) {
                        orderId
                        createdTime
                        finalPrice
                        status
                        orderItems {
                            orderItemId
                            quantity
                            unitSalePrice
                            totalPrice
                        }
                    }
                }
            ";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] UPDATING ORDER #{orderId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<UpdateOrderResponse>(query, null, token);

                if (response?.UpdateOrder != null)
                {
                    Debug.WriteLine($"[ORDER] ✓ Order #{orderId} updated");
                    Debug.WriteLine("════════════════════════════════════════");
                    return MapToOrder(response.UpdateOrder);
                }

                Debug.WriteLine("[ORDER] ✗ Failed to update order");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error updating order: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }
        }

        public async Task<bool> DeleteOrderAsync(int orderId, string? token)
        {
            var query = @"
                mutation {
                    deleteOrder(orderId: " + orderId + @") {
                        success
                        message
                    }
                }
            ";

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER] DELETING ORDER #{orderId}");
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _graphQLClient.QueryAsync<DeleteOrderResponse>(query, null, token);

                var success = response?.DeleteOrder?.Success ?? false;
                Debug.WriteLine($"[ORDER] {(success ? "✓" : "✗")} Order #{orderId} {(success ? "deleted" : "deletion failed")}");
                Debug.WriteLine("════════════════════════════════════════");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER] ✗ Error deleting order: {ex.Message}");
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
                    search = (string?)null,
                    page = 1,
                    limit = 1
                }
            };

            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[ORDER] COUNTING ORDERS");
                Debug.WriteLine("════════════════════════════════════════");

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
                CreatedTime = DateTime.Parse(data.CreatedTime ?? DateTime.UtcNow.ToString("o"), 
                    null, System.Globalization.DateTimeStyles.RoundtripKind),
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
                Product = null
            };
        }
    }
}