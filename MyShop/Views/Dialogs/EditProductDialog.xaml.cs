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
                
                if (!string.IsNullOrWhiteSpace(Image1UrlTextBox.Text))
                    images.Add(new ProductImageInput { Url = Image1UrlTextBox.Text.Trim(), IsPrimary = true, Position = 0 });
                
                if (!string.IsNullOrWhiteSpace(Image2UrlTextBox.Text))
                    images.Add(new ProductImageInput { Url = Image2UrlTextBox.Text.Trim(), IsPrimary = false, Position = 1 });
                
                if (!string.IsNullOrWhiteSpace(Image3UrlTextBox.Text))
                    images.Add(new ProductImageInput { Url = Image3UrlTextBox.Text.Trim(), IsPrimary = false, Position = 2 });
                
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

            // Set images
            var images = product.Images?.OrderBy(i => i.Position).ToList() ?? new List<ProductImage>();
            if (images.Count > 0) Image1UrlTextBox.Text = images[0].Url;
            if (images.Count > 1) Image2UrlTextBox.Text = images[1].Url;
            if (images.Count > 2) Image3UrlTextBox.Text = images[2].Url;
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
            if (string.IsNullOrWhiteSpace(Image1UrlTextBox.Text) ||
                string.IsNullOrWhiteSpace(Image2UrlTextBox.Text) ||
                string.IsNullOrWhiteSpace(Image3UrlTextBox.Text))
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
