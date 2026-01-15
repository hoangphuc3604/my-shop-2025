using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Views.Dialogs
{
    public sealed partial class EditProductDialog : ContentDialog
    {
        private int _imageCount = 0;

        public int ProductId { get; private set; }
        public string Sku => SkuTextBox.Text.Trim();
        public string ProductName => NameTextBox.Text.Trim();
        public int ImportPrice => (int)(ImportPriceBox.Value is double.NaN ? 0 : ImportPriceBox.Value);
        public int Count => (int)(CountBox.Value is double.NaN ? 0 : CountBox.Value);
        public string? Description => string.IsNullOrWhiteSpace(DescriptionTextBox.Text) 
            ? null 
            : DescriptionTextBox.Text.Trim();
        public Category? SelectedCategory => CategoryComboBox.SelectedItem as Category;
        
        public List<ProductImageInput> Images
        {
            get
            {
                var images = new List<ProductImageInput>();
                int position = 0;
                
                foreach (var child in ImagesPanel.Children)
                {
                    if (child is Grid grid && grid.Children.FirstOrDefault() is TextBox textBox)
                    {
                        var url = textBox.Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            images.Add(new ProductImageInput 
                            { 
                                Url = url, 
                                IsPrimary = position == 0, 
                                Position = position 
                            });
                            position++;
                        }
                    }
                }
                
                return images;
            }
        }

        public EditProductDialog()
        {
            this.InitializeComponent();
        }

        public void SetCategories(IEnumerable<Category> categories)
        {
            CategoryComboBox.ItemsSource = categories;
        }

        public void SetProduct(Product product, IEnumerable<Category> categories)
        {
            ProductId = product.ProductId;
            SkuTextBox.Text = product.Sku;
            NameTextBox.Text = product.Name;
            ImportPriceBox.Value = product.ImportPrice;
            CountBox.Value = product.Count;
            DescriptionTextBox.Text = product.Description ?? "";

            // Set categories and select current one
            var categoryList = categories.ToList();
            CategoryComboBox.ItemsSource = categoryList;
            CategoryComboBox.SelectedItem = categoryList.FirstOrDefault(c => c.CategoryId == product.CategoryId);

            // Set images - add fields for existing images
            var images = product.Images?.OrderBy(i => i.Position).ToList() ?? new List<ProductImage>();
            
            if (images.Count == 0)
            {
                // Add default 3 empty fields if no images
                AddImageField("Image 1 URL * (Primary)", "");
                AddImageField("Image 2 URL *", "");
                AddImageField("Image 3 URL *", "");
            }
            else
            {
                // Add fields for existing images
                for (int i = 0; i < images.Count; i++)
                {
                    var header = i == 0 ? "Image 1 URL * (Primary)" : $"Image {i + 1} URL" + (i < 3 ? " *" : "");
                    AddImageField(header, images[i].Url);
                }
                
                // Ensure at least 3 fields
                while (_imageCount < 3)
                {
                    AddImageField($"Image {_imageCount + 1} URL *", "");
                }
            }
        }

        private void AddImageField(string header = null, string value = "")
        {
            _imageCount++;
            var fieldHeader = header ?? $"Image {_imageCount} URL";
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new TextBox
            {
                Header = fieldHeader,
                PlaceholderText = "https://example.com/image.jpg",
                Text = value ?? ""
            };
            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            // Add remove button for images beyond the first 3
            if (_imageCount > 3)
            {
                var removeButton = new Button
                {
                    Content = new FontIcon { Glyph = "\uE711", FontSize = 12 },
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = grid
                };
                removeButton.Click += OnRemoveImageClick;
                Grid.SetColumn(removeButton, 1);
                grid.Children.Add(removeButton);
            }

            ImagesPanel.Children.Add(grid);
        }

        private void OnAddImageClick(object sender, RoutedEventArgs e)
        {
            AddImageField();
        }

        private void OnRemoveImageClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Grid grid)
            {
                ImagesPanel.Children.Remove(grid);
                _imageCount--;
            }
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(SkuTextBox.Text))
            {
                ShowError("SKU is required.");
                args.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowError("Product name is required.");
                args.Cancel = true;
                return;
            }

            if (CategoryComboBox.SelectedItem == null)
            {
                ShowError("Please select a category.");
                args.Cancel = true;
                return;
            }

            if (ImportPriceBox.Value is double.NaN || ImportPriceBox.Value < 0)
            {
                ShowError("Please enter a valid import price.");
                args.Cancel = true;
                return;
            }

            if (CountBox.Value is double.NaN || CountBox.Value < 0)
            {
                ShowError("Please enter a valid stock quantity.");
                args.Cancel = true;
                return;
            }

            // Validate images (minimum 3 required)
            var imageUrls = Images;
            if (imageUrls.Count < 3)
            {
                ShowError("At least 3 image URLs are required.");
                args.Cancel = true;
                return;
            }

            ErrorInfoBar.IsOpen = false;
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }
    }
}
