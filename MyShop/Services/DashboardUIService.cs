using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MyShop.Data.Models;
using Windows.UI;
using Microsoft.UI;

namespace MyShop.Services
{
    public class DashboardUIService
    {
        public void UpdateSummaryCards(TextBlock totalProductsText, TextBlock todayRevenueText, TextBlock todayOrdersText, DashboardStatsData stats)
        {
            totalProductsText.Text = stats.TotalProducts.ToString();
            todayRevenueText.Text = $"{stats.TodayRevenue:#,0} ₫";
            todayOrdersText.Text = stats.TodayOrdersCount.ToString();
        }

        public void UpdateTopSellingProducts(ItemsControl topSellingProductsList, DashboardStatsData stats)
        {
            topSellingProductsList.Items.Clear();

            var topProducts = stats.TopSellingProducts
                .OrderByDescending(p => p.TotalSold)
                .Take(5)
                .ToList();

            foreach (var product in topProducts)
            {
                var container = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 8) };

                var titleBlock = new TextBlock
                {
                    Text = $"{product.Name}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 13
                };

                var detailBlock = new TextBlock
                {
                    Text = $"SKU: {product.Sku} | Sold: {product.TotalSold}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
                };

                container.Children.Add(titleBlock);
                container.Children.Add(detailBlock);
                topSellingProductsList.Items.Add(container);
            }

            if (topProducts.Count == 0)
            {
                topSellingProductsList.Items.Add(new TextBlock { Text = "No sales data available" });
            }
        }

        public void UpdateLowStockProducts(ItemsControl lowStockProductsList, DashboardStatsData stats)
        {
            lowStockProductsList.Items.Clear();

            var lowStockProducts = stats.LowStockProducts
                .Where(p => p.Count < 5)
                .OrderBy(p => p.Count)
                .Take(5)
                .ToList();

            foreach (var product in lowStockProducts)
            {
                var container = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 8) };

                var titleBlock = new TextBlock
                {
                    Text = $"{product.Name}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 13
                };

                var detailBlock = new TextBlock
                {
                    Text = $"SKU: {product.Sku} | Stock: {product.Count} | Price: {product.ImportPrice:#,0} ₫",
                    FontSize = 11,
                    Foreground = product.Count < 2
                        ? new SolidColorBrush(Colors.Red)
                        : new SolidColorBrush(Color.FromArgb(255, 255, 165, 0))
                };

                container.Children.Add(titleBlock);
                container.Children.Add(detailBlock);
                lowStockProductsList.Items.Add(container);
            }

            if (lowStockProducts.Count == 0)
            {
                lowStockProductsList.Items.Add(new TextBlock { Text = "All products have sufficient stock" });
            }
        }

        public void UpdateRecentOrders(ItemsControl recentOrdersList, DashboardStatsData stats)
        {
            recentOrdersList.Items.Clear();

            var recentOrders = stats.RecentOrders
                .OrderByDescending(o => o.CreatedTime)
                .ToList();

            if (recentOrders.Count == 0)
            {
                var emptyMessage = new TextBlock
                {
                    Text = "No recent orders",
                    Padding = new Thickness(20),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
                    TextAlignment = TextAlignment.Center
                };
                recentOrdersList.Items.Add(emptyMessage);
                return;
            }

            foreach (var order in recentOrders)
            {
                var container = new Border
                {
                    Padding = new Thickness(20, 16, 20, 16),
                    Margin = new Thickness(0, 0, 0, 12),
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250)),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230))
                };

                var mainGrid = new Grid
                {
                    ColumnSpacing = 16,
                    RowSpacing = 8
                };

                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Left Column - Order Details
                var leftPanel = new StackPanel { Spacing = 8 };

                var orderHeader = new StackPanel { Spacing = 4, Orientation = Orientation.Horizontal };
                var orderIdBlock = new TextBlock
                {
                    Text = $"Order #{order.OrderId}",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215))
                };

                try
                {
                    if (DateTime.TryParse(order.CreatedTime, out var createdDate))
                    {
                        var dateBlock = new TextBlock
                        {
                            Text = $"• {createdDate:MMM dd, yyyy HH:mm}",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(10, 0, 0, 0)
                        };
                        orderHeader.Children.Add(orderIdBlock);
                        orderHeader.Children.Add(dateBlock);
                    }
                    else
                    {
                        orderHeader.Children.Add(orderIdBlock);
                    }
                }
                catch
                {
                    orderHeader.Children.Add(orderIdBlock);
                }

                leftPanel.Children.Add(orderHeader);

                // Customer and Status Row
                var customerStatusPanel = new StackPanel { Spacing = 8, Orientation = Orientation.Horizontal };

                var customerBlock = new TextBlock
                {
                    Text = $"👤 {order.User?.Username ?? "N/A"}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 64, 64, 64))
                };

                var statusBadge = new Border
                {
                    Padding = new Thickness(10, 4, 10, 4),
                    CornerRadius = new CornerRadius(4),
                    Background = GetStatusBackground(order.Status),
                    Child = new TextBlock
                    {
                        Text = order.Status,
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = GetStatusForeground(order.Status)
                    }
                };

                customerStatusPanel.Children.Add(customerBlock);
                customerStatusPanel.Children.Add(statusBadge);
                leftPanel.Children.Add(customerStatusPanel);

                // Items List
                var itemsPanel = new StackPanel { Spacing = 4 };
                var itemsHeaderBlock = new TextBlock
                {
                    Text = "Items:",
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 64, 64, 64))
                };
                itemsPanel.Children.Add(itemsHeaderBlock);

                foreach (var item in order.OrderItems)
                {
                    var itemBlock = new TextBlock
                    {
                        Text = $"  • {item.Product?.Name ?? "Unknown"} (×{item.Quantity}) - {item.TotalPrice:#,0} ₫",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 96, 96)),
                        TextWrapping = TextWrapping.Wrap
                    };
                    itemsPanel.Children.Add(itemBlock);
                }

                leftPanel.Children.Add(itemsPanel);

                Grid.SetColumn(leftPanel, 0);
                mainGrid.Children.Add(leftPanel);

                // Right Column - Total Price
                var pricePanel = new StackPanel
                {
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var priceLabel = new TextBlock
                {
                    Text = "Total",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
                    TextAlignment = TextAlignment.Right
                };

                var priceValue = new TextBlock
                {
                    Text = $"{order.FinalPrice:#,0} ₫",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 180, 0)),
                    TextAlignment = TextAlignment.Right
                };

                pricePanel.Children.Add(priceLabel);
                pricePanel.Children.Add(priceValue);

                Grid.SetColumn(pricePanel, 1);
                mainGrid.Children.Add(pricePanel);

                container.Child = mainGrid;
                recentOrdersList.Items.Add(container);
            }
        }

        private SolidColorBrush GetStatusBackground(string status)
        {
            return status?.ToLower() switch
            {
                "paid" => new SolidColorBrush(Color.FromArgb(255, 220, 245, 220)),       // Light Green
                "created" => new SolidColorBrush(Color.FromArgb(255, 255, 245, 220)),    // Light Orange
                "cancelled" => new SolidColorBrush(Color.FromArgb(255, 255, 220, 220)),  // Light Red
                "shipped" => new SolidColorBrush(Color.FromArgb(255, 220, 240, 255)),    // Light Blue
                _ => new SolidColorBrush(Color.FromArgb(255, 240, 240, 240))             // Light Gray
            };
        }

        private SolidColorBrush GetStatusForeground(string status)
        {
            return status?.ToLower() switch
            {
                "paid" => new SolidColorBrush(Color.FromArgb(255, 0, 140, 0)),       // Dark Green
                "created" => new SolidColorBrush(Color.FromArgb(255, 200, 100, 0)),  // Dark Orange
                "cancelled" => new SolidColorBrush(Color.FromArgb(255, 180, 0, 0)),  // Dark Red
                "shipped" => new SolidColorBrush(Color.FromArgb(255, 0, 100, 200)),  // Dark Blue
                _ => new SolidColorBrush(Color.FromArgb(255, 80, 80, 80))            // Dark Gray
            };
        }

        public void DrawMonthlyRevenueChart(Canvas canvas, TextBlock noDataMessageRevenue, DashboardStatsData stats)
        {
            try
            {
                canvas.Children.Clear();

                var chartData = stats.MonthlyRevenueChart
                    .OrderBy(d => DateTime.Parse(d.Date))
                    .ToList();

                if (chartData.Count == 0)
                {
                    noDataMessageRevenue.Visibility = Visibility.Visible;
                    return;
                }

                noDataMessageRevenue.Visibility = Visibility.Collapsed;

                var width = canvas.ActualWidth;
                var height = canvas.ActualHeight;
                var leftPadding = 70;
                var rightPadding = 20;
                var topPadding = 20;
                var bottomPadding = 50;
                var chartWidth = width - leftPadding - rightPadding;
                var chartHeight = height - topPadding - bottomPadding;

                var maxRevenue = chartData.Max(d => d.Revenue);

                // Draw axes
                DrawLine(canvas, leftPadding, height - bottomPadding, width - rightPadding, height - bottomPadding, Colors.White, 1);
                DrawLine(canvas, leftPadding, topPadding, leftPadding, height - bottomPadding, Colors.White, 1);

                // Draw bars
                var barCount = chartData.Count;
                var barWidth = chartWidth / barCount;
                var spacing = barWidth * 0.15;
                var actualBarWidth = barWidth - spacing;

                for (int i = 0; i < barCount; i++)
                {
                    var item = chartData[i];
                    var date = DateTime.Parse(item.Date);
                    var revenue = item.Revenue;
                    var label = date.ToString("MM-dd");

                    var x = leftPadding + (i * barWidth) + (spacing / 2);
                    var barHeight = (revenue / (double)maxRevenue) * chartHeight;
                    var y = height - bottomPadding - barHeight;

                    // Draw bar
                    DrawRectangle(canvas, x, y, actualBarWidth, barHeight, Colors.DodgerBlue);

                    // Draw label
                    var labelWidth = label.Length * 6;
                    DrawText(canvas, x + (actualBarWidth / 2) - (labelWidth / 2), height - bottomPadding + 10, label, Colors.White);

                    // Draw value
                    var valueText = $"{revenue:#,0}";
                    var valueWidth = valueText.Length * 5;
                    DrawText(canvas, x + (actualBarWidth / 2) - (valueWidth / 2), y - 20, valueText, Colors.White);
                }

                // Draw Y-axis labels
                for (int i = 0; i <= 5; i++)
                {
                    var value = (maxRevenue / 5.0) * i;
                    var y = height - bottomPadding - (i / 5.0 * chartHeight);
                    var text = $"{(int)value:#,0}";
                    DrawText(canvas, 5, y - 8, text, Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] Error drawing revenue chart: {ex.Message}");
            }
        }

        private void DrawLine(Canvas canvas, double x1, double y1, double x2, double y2, Color color, double thickness)
        {
            var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = new SolidColorBrush(color), StrokeThickness = thickness };
            canvas.Children.Add(line);
        }

        private void DrawRectangle(Canvas canvas, double x, double y, double width, double height, Color color)
        {
            var rect = new Rectangle { Width = width, Height = height, Fill = new SolidColorBrush(color) };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);
        }

        private void DrawText(Canvas canvas, double x, double y, string text, Color color)
        {
            var textBlock = new TextBlock { Text = text, Foreground = new SolidColorBrush(color), FontSize = 11 };
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(textBlock);
        }
    }
}