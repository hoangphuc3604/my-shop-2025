using System;
using System.Collections.Generic;

namespace MyShop.Data.Models
{
    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public int TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalQuantity { get; set; }
        public List<DailyProductQuantity> ProductQuantities { get; set; } = new();
    }

    public class DailyProductQuantity
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class WeeklyRevenue
    {
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalQuantity { get; set; }
        public List<PeriodProductQuantity> ProductQuantities { get; set; } = new();
    }

    public class MonthlyRevenue
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalQuantity { get; set; }
        public List<PeriodProductQuantity> ProductQuantities { get; set; } = new();
    }

    public class YearlyRevenue
    {
        public int Year { get; set; }
        public int TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalQuantity { get; set; }
        public List<PeriodProductQuantity> ProductQuantities { get; set; } = new();
    }

    public class PeriodProductQuantity
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ProductRevenue
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public int TotalRevenue { get; set; }
        public decimal AveragePricePerUnit { get; set; }
    }

    public class RevenueReport
    {
        public DateTime GeneratedDate { get; set; }
        public int TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailyRevenue> DailyRevenues { get; set; } = new();
        public List<WeeklyRevenue> WeeklyRevenues { get; set; } = new();
        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new();
        public List<YearlyRevenue> YearlyRevenues { get; set; } = new();
        public List<ProductRevenue> ProductRevenues { get; set; } = new();
    }
}