using Microsoft.UI.Xaml.Controls;
using System;

namespace MyShop.Views.Dialogs
{
    public sealed partial class AddCategoryDialog : ContentDialog
    {
        public string CategoryName => NameTextBox.Text.Trim();
        public string? CategoryDescription => string.IsNullOrWhiteSpace(DescriptionTextBox.Text) 
            ? null 
            : DescriptionTextBox.Text.Trim();

        public AddCategoryDialog()
        {
            this.InitializeComponent();
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ErrorInfoBar.Message = "Category name is required.";
                ErrorInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            if (NameTextBox.Text.Trim().Length > 200)
            {
                ErrorInfoBar.Message = "Category name must be 200 characters or less.";
                ErrorInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            ErrorInfoBar.IsOpen = false;
        }
    }
}
