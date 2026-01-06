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
    public class ReportService : IReportService
    {
        private readonly GraphQLClient _graphQLClient;

        public ReportService(GraphQLClient graphQLClient)
        {
            _graphQLClient = graphQLClient;
        }

        public async Task<RevenueReport> GenerateRevenueReportAsync(DateTime? fromDate, DateTime? toDate, string? token)
        {
            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[REPORT] GENERATING REVENUE REPORT");
                Debug.WriteLine($"[REPORT] Date Range: {fromDate} to {toDate}");
                Debug.WriteLine("════════════════════════════════════════");

                var variables = new
                {
                    fromDate = fromDate?.ToString("yyyy-MM-dd"),
                    toDate = toDate?.ToString("yyyy-MM-dd")
                };

                var query = @"
                    query GenerateRevenueReport($fromDate: String, $toDate: String) {
                      generateRevenueReport(fromDate: $fromDate, toDate: $toDate) {
                        averageOrderValue
                        dailyRevenues {
                          averageOrderValue
                          date
                          orderCount
                          productQuantities {
                            productId
                            productName
                            quantity
                          }
                          totalQuantity
                          totalRevenue
                        }
                        generatedDate
                        monthlyRevenues {
                          averageOrderValue
                          month
                          monthName
                          orderCount
                          productQuantities {
                            productId
                            productName
                            quantity
                          }
                          totalQuantity
                          totalRevenue
                          year
                        }
                        productRevenues {
                          averagePricePerUnit
                          productId
                          productName
                          sku
                          totalQuantitySold
                          totalRevenue
                        }
                        totalOrders
                        totalRevenue
                        weeklyRevenues {
                          year
                          weekStartDate
                          weekNumber
                          weekEndDate
                          totalRevenue
                          totalQuantity
                          productQuantities {
                            productId
                            productName
                            quantity
                          }
                          orderCount
                          averageOrderValue
                        }
                        yearlyRevenues {
                          averageOrderValue
                          orderCount
                          productQuantities {
                            productId
                            productName
                            quantity
                          }
                          totalQuantity
                          totalRevenue
                          year
                        }
                      }
                    }
                ";

                var response = await _graphQLClient.QueryAsync<GenerateRevenueReportResponse>(query, variables, token);

                if (response?.GenerateRevenueReport == null)
                {
                    Debug.WriteLine("[REPORT] No revenue report data returned");
                    Debug.WriteLine("════════════════════════════════════════");
                    return new RevenueReport { GeneratedDate = DateTime.UtcNow };
                }

                var report = MapGraphQLToRevenueReport(response.GenerateRevenueReport);

                Debug.WriteLine($"[REPORT] ✓ Report generated successfully");
                Debug.WriteLine($"[REPORT] Total Revenue: {report.TotalRevenue}");
                Debug.WriteLine($"[REPORT] Total Orders: {report.TotalOrders}");
                Debug.WriteLine($"[REPORT] Average Order Value: {report.AverageOrderValue}");
                Debug.WriteLine($"[REPORT] Daily Revenues: {report.DailyRevenues.Count}");
                Debug.WriteLine($"[REPORT] Weekly Revenues: {report.WeeklyRevenues.Count}");
                Debug.WriteLine($"[REPORT] Monthly Revenues: {report.MonthlyRevenues.Count}");
                Debug.WriteLine($"[REPORT] Yearly Revenues: {report.YearlyRevenues.Count}");
                Debug.WriteLine("════════════════════════════════════════");

                return report;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT] ✗ Error generating report: {ex.Message}");
                Debug.WriteLine($"[REPORT] Stack Trace: {ex.StackTrace}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        public async Task<List<ProductRevenue>> GetProductRevenueAsync(DateTime? fromDate, DateTime? toDate, string? token)
        {
            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[REPORT] FETCHING PRODUCT REVENUE");
                Debug.WriteLine($"[REPORT] Date Range: {fromDate} to {toDate}");
                Debug.WriteLine("════════════════════════════════════════");

                var variables = new
                {
                    fromDate = fromDate?.ToString("yyyy-MM-dd"),
                    toDate = toDate?.ToString("yyyy-MM-dd")
                };

                var query = @"
                    query GetProductRevenue($fromDate: String, $toDate: String) {
                      getProductRevenue(fromDate: $fromDate, toDate: $toDate) {
                        averagePricePerUnit
                        productId
                        productName
                        sku
                        totalQuantitySold
                        totalRevenue
                      }
                    }
                ";

                var response = await _graphQLClient.QueryAsync<GetProductRevenueResponse>(query, variables, token);

                if (response?.GetProductRevenue == null || response.GetProductRevenue.Count == 0)
                {
                    Debug.WriteLine("[REPORT] No product revenue data returned");
                    Debug.WriteLine("════════════════════════════════════════");
                    return new List<ProductRevenue>();
                }

                var productRevenues = response.GetProductRevenue.Select(pr => new ProductRevenue
                {
                    ProductId = pr.ProductId,
                    ProductName = pr.ProductName,
                    Sku = pr.Sku,
                    TotalQuantitySold = pr.TotalQuantitySold,
                    TotalRevenue = (int)pr.TotalRevenue,
                    AveragePricePerUnit = pr.AveragePricePerUnit
                }).ToList();

                Debug.WriteLine($"[REPORT] ✓ Product revenue fetched successfully");
                Debug.WriteLine($"[REPORT] Total Products: {productRevenues.Count}");
                Debug.WriteLine("════════════════════════════════════════");

                return productRevenues;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT] ✗ Error fetching product revenue: {ex.Message}");
                Debug.WriteLine($"[REPORT] Stack Trace: {ex.StackTrace}");
                Debug.WriteLine("════════════════════════════════════════");
                throw;
            }
        }

        private RevenueReport MapGraphQLToRevenueReport(GraphQLRevenueReport graphQLReport)
        {
            // Reorganize weekly revenues into proper 7-day consecutive weeks
            var properWeeklyRevenues = ReorganizeInto7DayWeeks(graphQLReport.WeeklyRevenues);

            return new RevenueReport
            {
                GeneratedDate = graphQLReport.GeneratedDate,
                TotalRevenue = (int)graphQLReport.TotalRevenue,
                TotalOrders = graphQLReport.TotalOrders,
                AverageOrderValue = graphQLReport.AverageOrderValue,
                DailyRevenues = graphQLReport.DailyRevenues.Select(dr => new DailyRevenue
                {
                    Date = dr.Date,
                    TotalRevenue = (int)dr.TotalRevenue,
                    OrderCount = dr.OrderCount,
                    AverageOrderValue = dr.AverageOrderValue,
                    TotalQuantity = dr.TotalQuantity,
                    ProductQuantities = dr.ProductQuantities.Select(pq => new DailyProductQuantity
                    {
                        ProductId = pq.ProductId,
                        ProductName = pq.ProductName,
                        Quantity = pq.Quantity
                    }).ToList()
                }).ToList(),
                WeeklyRevenues = properWeeklyRevenues,
                MonthlyRevenues = graphQLReport.MonthlyRevenues.Select(mr => new MonthlyRevenue
                {
                    Month = mr.Month,
                    Year = mr.Year,
                    MonthName = mr.MonthName,
                    TotalRevenue = (int)mr.TotalRevenue,
                    OrderCount = mr.OrderCount,
                    AverageOrderValue = mr.AverageOrderValue,
                    TotalQuantity = mr.TotalQuantity,
                    ProductQuantities = mr.ProductQuantities.Select(pq => new PeriodProductQuantity
                    {
                        ProductId = pq.ProductId,
                        ProductName = pq.ProductName,
                        Quantity = pq.Quantity
                    }).ToList()
                }).ToList(),
                YearlyRevenues = graphQLReport.YearlyRevenues.Select(yr => new YearlyRevenue
                {
                    Year = yr.Year,
                    TotalRevenue = (int)yr.TotalRevenue,
                    OrderCount = yr.OrderCount,
                    AverageOrderValue = yr.AverageOrderValue,
                    TotalQuantity = yr.TotalQuantity,
                    ProductQuantities = yr.ProductQuantities.Select(pq => new PeriodProductQuantity
                    {
                        ProductId = pq.ProductId,
                        ProductName = pq.ProductName,
                        Quantity = pq.Quantity
                    }).ToList()
                }).ToList(),
                ProductRevenues = graphQLReport.ProductRevenues.Select(pr => new ProductRevenue
                {
                    ProductId = pr.ProductId,
                    ProductName = pr.ProductName,
                    Sku = pr.Sku,
                    TotalQuantitySold = pr.TotalQuantitySold,
                    TotalRevenue = (int)pr.TotalRevenue,
                    AveragePricePerUnit = pr.AveragePricePerUnit
                }).ToList()
            };
        }

        private List<WeeklyRevenue> ReorganizeInto7DayWeeks(List<GraphQLWeeklyRevenue> graphQLWeeklyRevenues)
        {
            if (graphQLWeeklyRevenues.Count == 0)
                return new List<WeeklyRevenue>();

            var result = new List<WeeklyRevenue>();
            var sortedWeeks = graphQLWeeklyRevenues.OrderBy(w => w.WeekStartDate).ToList();

            // Track seen start dates to avoid duplicates
            var seenStartDates = new HashSet<DateTime>();

            // Include all weeks, regardless of day count, but skip duplicates
            for (int i = 0; i < sortedWeeks.Count; i++)
            {
                var week = sortedWeeks[i];
                var daysDiff = (week.WeekEndDate - week.WeekStartDate).Days;

                // Skip duplicate weeks with the same start date
                if (seenStartDates.Contains(week.WeekStartDate))
                {
                    Debug.WriteLine($"[REPORT] Skipping duplicate week: {week.WeekStartDate:MM-dd} ~ {week.WeekEndDate:MM-dd}");
                    continue;
                }

                seenStartDates.Add(week.WeekStartDate);

                result.Add(new WeeklyRevenue
                {
                    WeekNumber = result.Count + 1,  // Sequential numbering
                    Year = week.WeekStartDate.Year,
                    WeekStartDate = week.WeekStartDate,
                    WeekEndDate = week.WeekEndDate,
                    TotalRevenue = (int)week.TotalRevenue,
                    OrderCount = week.OrderCount,
                    AverageOrderValue = week.AverageOrderValue,
                    TotalQuantity = week.TotalQuantity,
                    ProductQuantities = week.ProductQuantities.Select(pq => new PeriodProductQuantity
                    {
                        ProductId = pq.ProductId,
                        ProductName = pq.ProductName,
                        Quantity = pq.Quantity
                    }).ToList()
                });

                Debug.WriteLine($"[REPORT] Week {result.Count}: {week.WeekStartDate:MM-dd} ~ {week.WeekEndDate:MM-dd} ({daysDiff} days) | Revenue: {week.TotalRevenue}");
            }

            return result;
        }

        // Helper response classes for GraphQL
        private class GenerateRevenueReportResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("generateRevenueReport")]
            public GraphQLRevenueReport? GenerateRevenueReport { get; set; }
        }

        private class GetProductRevenueResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("getProductRevenue")]
            public List<GraphQLProductRevenue> GetProductRevenue { get; set; } = new();
        }
    }

    public class GraphQLRevenueReport
    {
        [System.Text.Json.Serialization.JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("dailyRevenues")]
        public List<GraphQLDailyRevenue> DailyRevenues { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("generatedDate")]
        public DateTime GeneratedDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("monthlyRevenues")]
        public List<GraphQLMonthlyRevenue> MonthlyRevenues { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("productRevenues")]
        public List<GraphQLProductRevenue> ProductRevenues { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("totalOrders")]
        public int TotalOrders { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("weeklyRevenues")]
        public List<GraphQLWeeklyRevenue> WeeklyRevenues { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("yearlyRevenues")]
        public List<GraphQLYearlyRevenue> YearlyRevenues { get; set; } = new();
    }

    public class GraphQLDailyRevenue
    {
        [System.Text.Json.Serialization.JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("orderCount")]
        public int OrderCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productQuantities")]
        public List<GraphQLProductQuantity> ProductQuantities { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("totalQuantity")]
        public int TotalQuantity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }
    }

    public class GraphQLWeeklyRevenue
    {
        [System.Text.Json.Serialization.JsonPropertyName("year")]
        public int Year { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("weekStartDate")]
        public DateTime WeekStartDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("weekNumber")]
        public int WeekNumber { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("weekEndDate")]
        public DateTime WeekEndDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalQuantity")]
        public int TotalQuantity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productQuantities")]
        public List<GraphQLProductQuantity> ProductQuantities { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("orderCount")]
        public int OrderCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }
    }

    public class GraphQLMonthlyRevenue
    {
        [System.Text.Json.Serialization.JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("month")]
        public int Month { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("monthName")]
        public string MonthName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("orderCount")]
        public int OrderCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productQuantities")]
        public List<GraphQLProductQuantity> ProductQuantities { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("totalQuantity")]
        public int TotalQuantity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("year")]
        public int Year { get; set; }
    }

    public class GraphQLYearlyRevenue
    {
        [System.Text.Json.Serialization.JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("orderCount")]
        public int OrderCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productQuantities")]
        public List<GraphQLProductQuantity> ProductQuantities { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("totalQuantity")]
        public int TotalQuantity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("year")]
        public int Year { get; set; }
    }

    public class GraphQLProductRevenue
    {
        [System.Text.Json.Serialization.JsonPropertyName("averagePricePerUnit")]
        public decimal AveragePricePerUnit { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("totalQuantitySold")]
        public int TotalQuantitySold { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }
    }

    public class GraphQLProductQuantity
    {
        [System.Text.Json.Serialization.JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}