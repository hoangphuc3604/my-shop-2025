using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;

namespace MyShop.Views.Dialogs
{
    public sealed partial class AddProductDialog : ContentDialog
    {
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

        public AddProductDialog()
        {
            this.InitializeComponent();
        }

        public void SetCategories(IEnumerable<Category> categories)
        {
            CategoryComboBox.ItemsSource = categories;
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
