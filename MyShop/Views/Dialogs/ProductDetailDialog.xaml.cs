using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MyShop.Views.Dialogs
{
    public sealed partial class ProductDetailDialog : ContentDialog
    {
        private Product? _product;
        private List<string> _imageUrls = new();
        private int _currentImageIndex = 0;

        public ProductDetailDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Set the product to display in the dialog
        /// </summary>
        public void SetProduct(Product product)
        {
            _product = product;

            // Set product name
            ProductNameText.Text = product.Name;

            // Set SKU
            SkuText.Text = $"SKU: {product.Sku}";

            // Set Category
            CategoryText.Text = product.Category?.Name ?? "Uncategorized";

            // Set Price
            PriceText.Text = product.ImportPrice.ToString("N0");

            // Set Stock with status
            SetStockStatus(product.Count);

            // Set Description
            SetDescription(product.Description);

            // Setup images
            SetupImages(product);
        }

        private void SetStockStatus(int count)
        {
            StockCountText.Text = count.ToString();

            if (count == 0)
            {
                StockStatusBadge.Background = new SolidColorBrush(
                    Microsoft.UI.Colors.Red);
                StockStatusText.Text = "Out of Stock";
                StockStatusText.Foreground = new SolidColorBrush(
                    Microsoft.UI.Colors.White);
            }
            else if (count <= 10)
            {
                StockStatusBadge.Background = new SolidColorBrush(
                    Microsoft.UI.Colors.Orange);
                StockStatusText.Text = "Low Stock";
                StockStatusText.Foreground = new SolidColorBrush(
                    Microsoft.UI.Colors.White);
            }
            else
            {
                StockStatusBadge.Background = new SolidColorBrush(
                    Microsoft.UI.Colors.ForestGreen);
                StockStatusText.Text = "In Stock";
                StockStatusText.Foreground = new SolidColorBrush(
                    Microsoft.UI.Colors.White);
            }
        }

        private void SetDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                DescriptionText.Visibility = Visibility.Collapsed;
                NoDescriptionText.Visibility = Visibility.Visible;
            }
            else
            {
                DescriptionText.Text = description;
                DescriptionText.Visibility = Visibility.Visible;
                NoDescriptionText.Visibility = Visibility.Collapsed;
            }
        }

        private void SetupImages(Product product)
        {
            // Collect all available image URLs
            _imageUrls.Clear();

            // Try to get images from the Images collection first
            if (product.Images != null && product.Images.Any())
            {
                _imageUrls = product.Images
                    .OrderBy(i => i.Position)
                    .Select(i => i.Url)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();
            }

            // Fallback to PrimaryImageUrl if no images in collection
            if (!_imageUrls.Any() && !string.IsNullOrWhiteSpace(product.PrimaryImageUrl))
            {
                _imageUrls.Add(product.PrimaryImageUrl);
            }

            // Set up the UI based on available images
            if (_imageUrls.Any())
            {
                NoImagePlaceholder.Visibility = Visibility.Collapsed;
                _currentImageIndex = 0;
                DisplayImage(_currentImageIndex);

                // Show image counter if multiple images
                if (_imageUrls.Count > 1)
                {
                    ImageCounterBadge.Visibility = Visibility.Visible;
                    UpdateImageCounter();
                    SetupThumbnails();
                }
                else
                {
                    ImageCounterBadge.Visibility = Visibility.Collapsed;
                    ThumbnailsPanel.Children.Clear();
                }
            }
            else
            {
                NoImagePlaceholder.Visibility = Visibility.Visible;
                MainProductImage.Source = null;
                ImageCounterBadge.Visibility = Visibility.Collapsed;
                ThumbnailsPanel.Children.Clear();
            }
        }

        private void DisplayImage(int index)
        {
            if (index >= 0 && index < _imageUrls.Count)
            {
                try
                {
                    var imageUrl = _imageUrls[index];
                    MainProductImage.Source = new BitmapImage(new Uri(imageUrl));
                    UpdateThumbnailSelection(index);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProductDetailDialog] Error loading image: {ex.Message}");
                    NoImagePlaceholder.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateImageCounter()
        {
            ImageCounterText.Text = $"{_currentImageIndex + 1} / {_imageUrls.Count}";
        }

        private void SetupThumbnails()
        {
            ThumbnailsPanel.Children.Clear();

            for (int i = 0; i < _imageUrls.Count; i++)
            {
                var thumbnailBorder = CreateThumbnail(i);
                ThumbnailsPanel.Children.Add(thumbnailBorder);
            }
        }

        private Border CreateThumbnail(int index)
        {
            var imageUrl = _imageUrls[index];

            var image = new Image
            {
                Width = 60,
                Height = 60,
                Stretch = Stretch.UniformToFill
            };

            try
            {
                image.Source = new BitmapImage(new Uri(imageUrl));
            }
            catch
            {
                // Use placeholder for failed thumbnails
            }

            var border = new Border
            {
                Width = 64,
                Height = 64,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(2),
                Tag = index,
                Child = image,
                Background = Application.Current.Resources["LayerFillColorAltBrush"] as Brush
            };

            // Set initial border color
            UpdateThumbnailBorder(border, index == _currentImageIndex);

            // Add click handler
            border.PointerPressed += (s, e) =>
            {
                if (border.Tag is int clickedIndex)
                {
                    _currentImageIndex = clickedIndex;
                    DisplayImage(_currentImageIndex);
                    UpdateImageCounter();
                }
            };

            // Add hover effect
            border.PointerEntered += (s, e) =>
            {
                border.Opacity = 0.8;
            };

            border.PointerExited += (s, e) =>
            {
                border.Opacity = 1.0;
            };

            return border;
        }

        private void UpdateThumbnailBorder(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.BorderBrush = Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush;
            }
            else
            {
                border.BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush;
            }
        }

        private void UpdateThumbnailSelection(int selectedIndex)
        {
            foreach (var child in ThumbnailsPanel.Children)
            {
                if (child is Border border && border.Tag is int index)
                {
                    UpdateThumbnailBorder(border, index == selectedIndex);
                }
            }
        }
    }
}
