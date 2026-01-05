using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Contracts;
using MyShop.Data.Models;

namespace MyShop.Services
{
    public class ReportService : IReportService
    {
        private readonly IOrderService _orderService;

        public ReportService(IOrderService orderService)
        {
            _orderService = orderService;
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

                var report = new RevenueReport
                {
                    GeneratedDate = DateTime.UtcNow
                };

                // Fetch all orders in the date range
                var orders = await FetchAllOrdersAsync(fromDate, toDate, token);

                if (orders.Count == 0)
                {
                    Debug.WriteLine("[REPORT] No orders found in the date range");
                    Debug.WriteLine("════════════════════════════════════════");
                    return report;
                }

                // Calculate totals
                report.TotalRevenue = orders.Sum(o => o.FinalPrice);
                report.TotalOrders = orders.Count;
                report.AverageOrderValue = report.TotalOrders > 0 ? (decimal)report.TotalRevenue / report.TotalOrders : 0;

                // Generate daily revenues
                report.DailyRevenues = GenerateDailyRevenues(orders, fromDate, toDate);

                // Generate weekly revenues
                report.WeeklyRevenues = GenerateWeeklyRevenues(orders, fromDate, toDate);

                // Generate monthly revenues
                report.MonthlyRevenues = GenerateMonthlyRevenues(orders, fromDate, toDate);

                // Generate yearly revenues
                report.YearlyRevenues = GenerateYearlyRevenues(orders, fromDate, toDate);

                // Generate product revenues
                report.ProductRevenues = GenerateProductRevenues(orders);

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
                Debug.WriteLine("════════════════════════════════════════");
                return new RevenueReport { GeneratedDate = DateTime.UtcNow };
            }
        }

        public async Task<List<ProductRevenue>> GetProductRevenueAsync(DateTime? fromDate, DateTime? toDate, string? token)
        {
            var orders = await FetchAllOrdersAsync(fromDate, toDate, token);
            return GenerateProductRevenues(orders);
        }

        private async Task<List<Order>> FetchAllOrdersAsync(DateTime? fromDate, DateTime? toDate, string? token)
        {
            var allOrders = new List<Order>();
            int page = 1;
            const int pageSize = 100;

            while (true)
            {
                var orders = await _orderService.GetOrdersAsync(page, pageSize, fromDate, toDate, token);
                if (orders.Count == 0)
                    break;

                allOrders.AddRange(orders);
                page++;

                // Safety limit
                if (page > 100)
                    break;
            }

            return allOrders;
        }

        private List<DailyRevenue> GenerateDailyRevenues(List<Order> orders, DateTime? fromDate, DateTime? toDate)
        {
            return orders
                .Where(o => (!fromDate.HasValue || o.CreatedTime.Date >= fromDate.Value.Date) &&
                            (!toDate.HasValue || o.CreatedTime.Date < toDate.Value.Date))
                .GroupBy(o => o.CreatedTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(o => o.FinalPrice),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Count() > 0 ? (decimal)g.Sum(o => o.FinalPrice) / g.Count() : 0,
                    TotalQuantity = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity),
                    ProductQuantities = g
                        .SelectMany(o => o.OrderItems)
                        .GroupBy(oi => new { oi.Product?.ProductId, oi.Product?.Name })
                        .Select(pg => new DailyProductQuantity
                        {
                            ProductId = pg.Key.ProductId ?? 0,
                            ProductName = pg.Key.Name ?? "Unknown",
                            Quantity = pg.Sum(oi => oi.Quantity)
                        })
                        .ToList()
                })
                .ToList();
        }

        private List<WeeklyRevenue> GenerateWeeklyRevenues(List<Order> orders, DateTime? fromDate, DateTime? toDate)
        {
            var filtered = orders
                .Where(o => (!fromDate.HasValue || o.CreatedTime.Date >= fromDate.Value.Date) &&
                            (!toDate.HasValue || o.CreatedTime.Date < toDate.Value.Date))
                .ToList();

            if (filtered.Count == 0)
                return new List<WeeklyRevenue>();

            var weeklyRevenues = new List<WeeklyRevenue>();
            var calendar = System.Globalization.CultureInfo.CurrentCulture.Calendar;

            // Group by week (Monday-Sunday)
            var weekGroups = filtered
                .GroupBy(o =>
                {
                    var date = o.CreatedTime.Date;
                    var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
                    if (daysFromMonday < 0) daysFromMonday += 7;
                    var monday = date.AddDays(-daysFromMonday);
                    return monday;
                })
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var weekGroup in weekGroups)
            {
                var mondayOfWeek = weekGroup.Key;
                var ordersInWeek = weekGroup.ToList();
                var minDate = ordersInWeek.Min(o => o.CreatedTime.Date);
                var maxDate = ordersInWeek.Max(o => o.CreatedTime.Date);

                var weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    mondayOfWeek,
                    System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);

                weeklyRevenues.Add(new WeeklyRevenue
                {
                    WeekNumber = weekNumber,
                    Year = mondayOfWeek.Year,
                    WeekStartDate = minDate,
                    WeekEndDate = maxDate,
                    TotalRevenue = ordersInWeek.Sum(o => o.FinalPrice),
                    OrderCount = ordersInWeek.Count(),
                    AverageOrderValue = ordersInWeek.Count() > 0 ? (decimal)ordersInWeek.Sum(o => o.FinalPrice) / ordersInWeek.Count() : 0,
                    TotalQuantity = ordersInWeek.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity),
                    ProductQuantities = ordersInWeek
                        .SelectMany(o => o.OrderItems)
                        .GroupBy(oi => new { oi.Product?.ProductId, oi.Product?.Name })
                        .Select(pg => new PeriodProductQuantity
                        {
                            ProductId = pg.Key.ProductId ?? 0,
                            ProductName = pg.Key.Name ?? "Unknown",
                            Quantity = pg.Sum(oi => oi.Quantity)
                        })
                        .ToList()
                });
            }

            return weeklyRevenues;
        }

        private List<MonthlyRevenue> GenerateMonthlyRevenues(List<Order> orders, DateTime? fromDate, DateTime? toDate)
        {
            var filtered = orders
                .Where(o => (!fromDate.HasValue || o.CreatedTime.Date >= fromDate.Value.Date) &&
                            (!toDate.HasValue || o.CreatedTime.Date < toDate.Value.Date))
                .ToList();

            return filtered
                .GroupBy(o => new { o.CreatedTime.Year, o.CreatedTime.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyRevenue
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                    TotalRevenue = g.Sum(o => o.FinalPrice),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Count() > 0 ? (decimal)g.Sum(o => o.FinalPrice) / g.Count() : 0,
                    TotalQuantity = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity),
                    ProductQuantities = g
                        .SelectMany(o => o.OrderItems)
                        .GroupBy(oi => new { oi.Product?.ProductId, oi.Product?.Name })
                        .Select(pg => new PeriodProductQuantity
                        {
                            ProductId = pg.Key.ProductId ?? 0,
                            ProductName = pg.Key.Name ?? "Unknown",
                            Quantity = pg.Sum(oi => oi.Quantity)
                        })
                        .ToList()
                })
                .ToList();
        }

        private List<YearlyRevenue> GenerateYearlyRevenues(List<Order> orders, DateTime? fromDate, DateTime? toDate)
        {
            var filtered = orders
                .Where(o => (!fromDate.HasValue || o.CreatedTime.Date >= fromDate.Value.Date) &&
                            (!toDate.HasValue || o.CreatedTime.Date < toDate.Value.Date))
                .ToList();

            return filtered
                .GroupBy(o => o.CreatedTime.Year)
                .OrderBy(g => g.Key)
                .Select(g => new YearlyRevenue
                {
                    Year = g.Key,
                    TotalRevenue = g.Sum(o => o.FinalPrice),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Count() > 0 ? (decimal)g.Sum(o => o.FinalPrice) / g.Count() : 0,
                    TotalQuantity = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity),
                    ProductQuantities = g
                        .SelectMany(o => o.OrderItems)
                        .GroupBy(oi => new { oi.Product?.ProductId, oi.Product?.Name })
                        .Select(pg => new PeriodProductQuantity
                        {
                            ProductId = pg.Key.ProductId ?? 0,
                            ProductName = pg.Key.Name ?? "Unknown",
                            Quantity = pg.Sum(oi => oi.Quantity)
                        })
                        .ToList()
                })
                .ToList();
        }

        private List<ProductRevenue> GenerateProductRevenues(List<Order> orders)
        {
            return orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => new
                {
                    oi.Product?.ProductId,
                    oi.Product?.Name,
                    oi.Product?.Sku
                })
                .Select(g => new ProductRevenue
                {
                    ProductId = g.Key.ProductId ?? 0,
                    ProductName = g.Key.Name ?? "Unknown",
                    Sku = g.Key.Sku ?? "Unknown",
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice),
                    AveragePricePerUnit = g.Sum(oi => oi.Quantity) > 0
                        ? (decimal)g.Sum(oi => oi.TotalPrice) / g.Sum(oi => oi.Quantity)
                        : 0
                })
                .OrderByDescending(pr => pr.TotalRevenue)
                .ToList();
        }
    }
}