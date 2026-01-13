using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Services;
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
            ViewModel = (App.Services.GetService(typeof(ProductViewModel)) as ProductViewModel)!;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += ProductPage_SizeChanged;

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

        private void ProductPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
        }

        private void ApplyResponsiveLayout(double width, double height)
        {
            try
            {
                var viewportSize = ResponsiveService.GetCurrentViewportSize(width, height);
                var isCompact = ResponsiveService.IsCompactLayout(width);
                var padding = ResponsiveService.GetOptimalPadding(width);

                Debug.WriteLine($"[PRODUCT_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                // Update padding only - layout is already responsive in XAML
                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private void UpdateUIState()
        {
            LoadingPanel.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            
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
                    var product = ViewModel.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
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
                    var product = ViewModel.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
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
                    var product = ViewModel.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
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
