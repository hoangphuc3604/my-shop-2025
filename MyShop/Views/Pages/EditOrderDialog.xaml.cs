using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Views.Pages
{
    public sealed partial class EditOrderDialog : Grid
    {
        public EditOrderDialog()
        {
            this.InitializeComponent();
        }

        public void SetOrderData(Order order)
        {
            if (order == null)
                return;

            // Set header
            OrderIdBlock.Text = $"Order #{order.OrderId}";

            // Setup status dropdown
            StatusCombo.Items.Add("Created");
            StatusCombo.Items.Add("Paid");
            StatusCombo.Items.Add("Cancelled");
            StatusCombo.SelectedIndex = StatusCombo.Items.IndexOf(order.Status);

            // Build items
            int totalPrice = 0;

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
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnSpacing = 15;

                // Product name
                var nameBlock = new TextBlock
                {
                    Text = $"{productName} (ID: {item.ProductId})",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameBlock, 0);
                itemGrid.Children.Add(nameBlock);

                // Quantity label
                var quantityLabel = new TextBlock
                {
                    Text = "Qty:",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                };
                Grid.SetColumn(quantityLabel, 1);
                itemGrid.Children.Add(quantityLabel);

                // Quantity box
                var quantityBox = new NumberBox
                {
                    Value = item.Quantity,
                    Minimum = 1,
                    Maximum = 999,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
                    Width = 70,
                    Tag = item.OrderItemId
                };
                quantityBox.ValueChanged += (s, e) => UpdateTotalPrice();
                Grid.SetColumn(quantityBox, 2);
                itemGrid.Children.Add(quantityBox);

                // Unit price
                var unitPriceBlock = new TextBlock
                {
                    Text = $"@ {item.UnitSalePrice} ₫",
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 100, 100))
                };
                Grid.SetColumn(unitPriceBlock, 3);
                itemGrid.Children.Add(unitPriceBlock);

                // Item total price
                var itemTotalBlock = new TextBlock
                {
                    Text = $"= {item.TotalPrice} ₫",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 215)),
                    Tag = item.OrderItemId,
                    Name = $"TotalPrice_{item.OrderItemId}"
                };
                Grid.SetColumn(itemTotalBlock, 4);
                itemGrid.Children.Add(itemTotalBlock);

                itemBorder.Child = itemGrid;
                ItemsStackPanel.Children.Add(itemBorder);

                totalPrice += item.TotalPrice;
            }

            TotalPriceBlock.Text = $"{totalPrice} ₫";
        }

        public string GetSelectedStatus()
        {
            return StatusCombo.SelectedItem as string ?? "Created";
        }

        public Dictionary<int, int> GetUpdatedQuantities()
        {
            var result = new Dictionary<int, int>();

            foreach (var itemBorder in ItemsStackPanel.Children.OfType<Border>())
            {
                var itemGrid = itemBorder.Child as Grid;
                if (itemGrid == null)
                    continue;

                var quantityBox = itemGrid.Children.OfType<NumberBox>().FirstOrDefault();
                if (quantityBox == null)
                    continue;

                var orderItemId = (int)quantityBox.Tag;
                var newQuantity = (int)quantityBox.Value;

                result[orderItemId] = newQuantity;
            }

            return result;
        }

        private void UpdateTotalPrice()
        {
            int totalPrice = 0;

            foreach (var itemBorder in ItemsStackPanel.Children.OfType<Border>())
            {
                var itemGrid = itemBorder.Child as Grid;
                if (itemGrid == null)
                    continue;

                var quantityBox = itemGrid.Children.OfType<NumberBox>().FirstOrDefault();
                var itemTotalBlock = itemGrid.Children.OfType<TextBlock>().LastOrDefault();

                if (quantityBox == null || itemTotalBlock == null)
                    continue;

                var orderItemId = (int)quantityBox.Tag;
                var quantity = (int)quantityBox.Value;

                // Find the unit price from the UI
                var unitPriceBlock = itemGrid.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Text.StartsWith("@"));

                if (unitPriceBlock != null && int.TryParse(unitPriceBlock.Text.Split(' ')[1], out int unitPrice))
                {
                    int itemTotal = unitPrice * quantity;
                    itemTotalBlock.Text = $"= {itemTotal} ₫";
                    totalPrice += itemTotal;
                }
            }

            TotalPriceBlock.Text = $"{totalPrice} ₫";
        }
    }
}