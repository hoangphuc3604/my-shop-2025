using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services.GraphQL;

namespace MyShop.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly GraphQLClient _graphQLClient;

        public DashboardService(GraphQLClient graphQLClient)
        {
            _graphQLClient = graphQLClient;
        }

        public async Task<DashboardStatsData> LoadDashboardStatsAsync(string? token)
        {
            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[DASHBOARD] LOADING DASHBOARD STATS");
                Debug.WriteLine("════════════════════════════════════════");

                var query = @"
                    query DashboardStats {
                        dashboardStats {
                            totalProducts
                            topSellingProducts {
                                name
                                productId
                                sku
                                totalSold
                            }
                            todayRevenue
                            todayOrdersCount
                            recentOrders {
                                createdTime
                                finalPrice
                                orderId
                                orderItems {
                                    product {
                                        name
                                        sku
                                    }
                                    quantity
                                    totalPrice
                                    unitSalePrice
                                }
                                status
                                user {
                                    username
                                }
                            }
                            monthlyRevenueChart {
                                date
                                revenue
                            }
                            lowStockProducts {
                                count
                                importPrice
                                name
                                productId
                                sku
                            }
                        }
                    }
                ";

                var response = await _graphQLClient.QueryAsync<DashboardStatsResponse>(query, null, token);

                if (response?.DashboardStats != null)
                {
                    Debug.WriteLine("[DASHBOARD] ✓ Dashboard loaded successfully");
                    Debug.WriteLine("════════════════════════════════════════");
                    return response.DashboardStats;
                }

                Debug.WriteLine("[DASHBOARD] ✗ No dashboard data returned");
                Debug.WriteLine("════════════════════════════════════════");
                throw new InvalidOperationException("Failed to load dashboard: No data returned");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] ✗ Error loading dashboard: {ex.Message}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        private class DashboardStatsResponse
        {
            public DashboardStatsData? DashboardStats { get; set; }
        }
    }
}