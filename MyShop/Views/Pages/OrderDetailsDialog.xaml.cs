using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderDetailsDialog : Grid
    {
        public OrderDetailsDialog()
        {
            this.InitializeComponent();
        }

        public void SetOrderData(Order order)
        {
            if (order == null)
                return;

            // Set header information
            OrderIdBlock.Text = $"Order ID: {order.OrderId}";
            StatusBlock.Text = $"Status: {order.Status}";
            CreatedDateBlock.Text = $"Created: {order.CreatedTime:yyyy-MM-dd HH:mm:ss}";

            // Set total price
            TotalPriceBlock.Text = $"{order.FinalPrice} ₫";

            // Build items list
            var itemsList = new List<Border>();

            if (order.OrderItems.Count == 0)
            {
                var noItemsBlock = new TextBlock
                {
                    Text = "No items in this order.",
                    FontSize = 12,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(200, 128, 128, 128))
                };
                ItemsControl.Items.Add(noItemsBlock);
            }
            else
            {
                foreach (var item in order.OrderItems)
                {
                    var productName = item.Product?.Name ?? "Unknown Product";

                    var itemBorder = new Border
                    {
                        Padding = new Thickness(12),
                        BorderThickness = new Thickness(1),
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(100, 200, 200, 200)),
                        CornerRadius = new CornerRadius(4),
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(10, 0, 0, 0))
                    };

                    var itemGrid = new Grid();
                    itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    itemGrid.RowSpacing = 5;

                    var nameBlock = new TextBlock
                    {
                        Text = $"• {productName} (ID: {item.ProductId})",
                        FontSize = 12,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    };
                    Grid.SetRow(nameBlock, 0);
                    itemGrid.Children.Add(nameBlock);

                    var quantityBlock = new TextBlock
                    {
                        Text = $"Quantity: {item.Quantity}",
                        FontSize = 11,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 80, 80, 80))
                    };
                    Grid.SetRow(quantityBlock, 1);
                    itemGrid.Children.Add(quantityBlock);

                    var priceBlock = new TextBlock
                    {
                        Text = $"Unit Price: {item.UnitSalePrice} ₫",
                        FontSize = 11,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 80, 80, 80))
                    };
                    Grid.SetRow(priceBlock, 2);
                    itemGrid.Children.Add(priceBlock);

                    var totalBlock = new TextBlock
                    {
                        Text = $"Total: {item.TotalPrice} ₫",
                        FontSize = 12,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 215))
                    };
                    Grid.SetRow(totalBlock, 3);
                    itemGrid.Children.Add(totalBlock);

                    itemBorder.Child = itemGrid;
                    ItemsControl.Items.Add(itemBorder);
                }
            }
        }
    }
}