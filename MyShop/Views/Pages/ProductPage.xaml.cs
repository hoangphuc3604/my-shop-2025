using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MyShop.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class ProductPage : Page
    {
        public ProductViewModel ViewModel { get; }

        public ProductPage()
        {
            this.InitializeComponent();
            
            // Get ViewModel from DI
            ViewModel = (App.Services.GetService(typeof(ProductViewModel)) as ProductViewModel)!;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                Debug.WriteLine("[PRODUCT_PAGE] OnNavigatedTo - Initializing...");
                await ViewModel.InitializeAsync();
                UpdateUIState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error in OnNavigatedTo: {ex.Message}");
                await ShowErrorDialog("Initialization Error", $"Failed to load product page: {ex.Message}");
            }
        }

        /// <summary>
        /// Update UI state based on ViewModel (loading, empty, content)
        /// </summary>
        private void UpdateUIState()
        {
            // Show/hide loading indicator
            LoadingPanel.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            
            // Show/hide empty state
            if (!ViewModel.IsLoading && ViewModel.Products.Count == 0)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                ProductsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                ProductsListView.Visibility = Visibility.Visible;
            }
        }

        // ===== Event Handlers =====

        private async void OnCategoryFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && ViewModel.ApplyCategoryFilterCommand.CanExecute(null))
            {
                await ViewModel.ApplyCategoryFilterCommand.ExecuteAsync(null);
                UpdateUIState();
            }
        }

        private async void OnSortChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && ViewModel.ApplySortCommand.CanExecute(null))
            {
                await ViewModel.ApplySortCommand.ExecuteAsync(null);
                UpdateUIState();
            }
        }

        private async void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Use fully qualified name to avoid namespace confusion
            if (e.Key == global::Windows.System.VirtualKey.Enter)
            {
                if (ViewModel.SearchCommand.CanExecute(null))
                {
                    await ViewModel.SearchCommand.ExecuteAsync(null);
                    UpdateUIState();
                }
            }
        }

        private async void OnViewProductClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int productId)
                {
                    Debug.WriteLine($"[PRODUCT_PAGE] View product #{productId}");
                    
                    // Find product
                    var product = ViewModel.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
                        // TODO: Open ProductDetailsDialog
                        await ShowInfoDialog("View Product", $"Viewing product: {product.Name}\n\nDetails dialog will be implemented next.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error viewing product: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to view product: {ex.Message}");
            }
        }

        private async void OnEditProductClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int productId)
                {
                    Debug.WriteLine($"[PRODUCT_PAGE] Edit product #{productId}");
                    
                    // Find product
                    var product = ViewModel.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
                        // TODO: Open EditProductDialog
                        await ShowInfoDialog("Edit Product", 
                            $"Editing product: {product.Name}\n\n" +
                            "✓ UI is ready\n" +
                            "⚠ Backend mutation not yet implemented\n\n" +
                            "Edit dialog will be added next.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error editing product: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to edit product: {ex.Message}");
            }
        }

        private async void OnDeleteProductClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int productId)
                {
                    Debug.WriteLine($"[PRODUCT_PAGE] Delete product #{productId}");
                    
                    // Find product
                    var product = ViewModel.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
                        // Show confirmation dialog
                        var confirmed = await ShowConfirmDialog(
                            "Delete Product",
                            $"Are you sure you want to delete '{product.Name}'?\n\n" +
                            "This action cannot be undone.");

                        if (confirmed)
                        {
                            await ShowInfoDialog("Delete Product",
                                "✓ UI is ready\n" +
                                "⚠ Backend mutation not yet implemented\n\n" +
                                "Product deletion will be available when backend is ready.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error deleting product: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to delete product: {ex.Message}");
            }
        }

        private async void OnAddProductClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("[PRODUCT_PAGE] Add new product");
                
                // TODO: Open AddProductDialog
                await ShowInfoDialog("Add Product",
                    "✓ UI is ready\n" +
                    "⚠ Backend mutation not yet implemented\n\n" +
                    "Add product dialog will be added next.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error adding product: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to add product: {ex.Message}");
            }
        }

        private async void OnAddCategoryClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("[PRODUCT_PAGE] Add new category");
                
                // TODO: Open AddCategoryDialog
                await ShowInfoDialog("Add Category",
                    "✓ UI is ready\n" +
                    "⚠ Backend mutation not yet implemented\n\n" +
                    "Add category dialog will be added next.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error adding category: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to add category: {ex.Message}");
            }
        }

        private async void OnImportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("[PRODUCT_PAGE] Import products");
                
                // TODO: Open ImportProductsDialog
                await ShowInfoDialog("Import Products",
                    "Import feature is planned for future implementation.\n\n" +
                    "Supported formats:\n" +
                    "• Excel (.xlsx, .xls)\n" +
                    "• Access (.accdb, .mdb)\n\n" +
                    "This feature will be available in a later update.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error importing: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to import products: {ex.Message}");
            }
        }

        // ===== Helper Methods =====

        private async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async Task ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
