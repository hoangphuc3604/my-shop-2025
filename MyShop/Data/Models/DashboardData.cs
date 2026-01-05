using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyShop.Data.Models
{
    public class DashboardStatsData
    {
        [JsonPropertyName("totalProducts")]
        public int TotalProducts { get; set; }

        [JsonPropertyName("topSellingProducts")]
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new();

        [JsonPropertyName("todayRevenue")]
        public int TodayRevenue { get; set; }

        [JsonPropertyName("todayOrdersCount")]
        public int TodayOrdersCount { get; set; }

        [JsonPropertyName("recentOrders")]
        public List<RecentOrder> RecentOrders { get; set; } = new();

        [JsonPropertyName("monthlyRevenueChart")]
        public List<DashboardMonthlyRevenue> MonthlyRevenueChart { get; set; } = new();

        [JsonPropertyName("lowStockProducts")]
        public List<LowStockProduct> LowStockProducts { get; set; } = new();
    }

    public class TopSellingProduct
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("totalSold")]
        public int TotalSold { get; set; }
    }

    public class RecentOrder
    {
        [JsonPropertyName("createdTime")]
        public string CreatedTime { get; set; } = string.Empty;

        [JsonPropertyName("finalPrice")]
        public int FinalPrice { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("orderItems")]
        public List<RecentOrderItem> OrderItems { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public OrderUser? User { get; set; }
    }

    public class RecentOrderItem
    {
        [JsonPropertyName("product")]
        public OrderProduct? Product { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("totalPrice")]
        public int TotalPrice { get; set; }

        [JsonPropertyName("unitSalePrice")]
        public int UnitSalePrice { get; set; }
    }

    public class OrderProduct
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;
    }

    public class OrderUser
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    public class DashboardMonthlyRevenue
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("revenue")]
        public int Revenue { get; set; }
    }

    public class LowStockProduct
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("importPrice")]
        public int ImportPrice { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;
    }
}