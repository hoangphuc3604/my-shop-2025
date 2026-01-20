using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Services;
using MyShop.ViewModels;
using MyShop.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace MyShop.Views.Pages
{
    public sealed partial class ProductPage : Page
    {
        public ProductViewModel ViewModel { get; set; }
        private OnboardingService _onboardingService;
        
        private List<OnboardingStep> _onboardingSteps;
        private int _currentOnboardingStep = 0;

        public ProductPage()
        {
            this.InitializeComponent();
            var viewModel = App.Services.GetService(typeof(ProductViewModel)) as ProductViewModel;
            ViewModel = viewModel ?? throw new InvalidOperationException("ProductViewModel could not be resolved from services");
            _onboardingService = (App.Services.GetService(typeof(OnboardingService)) as OnboardingService)!;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += ProductPage_SizeChanged;

            try
            {
                Debug.WriteLine("[PRODUCT_PAGE] OnNavigatedTo - Initializing...");
                await ViewModel.InitializeAsync();
                UpdateUIState();
                
                // Start onboarding if first time
                if (!_onboardingService.IsProductOnboardingCompleted())
                {
                    StartOnboarding();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error in OnNavigatedTo: {ex.Message}");
                await ShowErrorDialog("Initialization Error", $"Failed to load product page: {ex.Message}");
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= ProductPage_SizeChanged;
        }

        private void StartOnboarding()
        {
            _onboardingSteps = _onboardingService.GetProductPageSteps();
            _currentOnboardingStep = 0;
            ShowCurrentOnboardingStep();
        }

        private void ShowCurrentOnboardingStep()
        {
            if (_currentOnboardingStep >= _onboardingSteps.Count)
            {
                CompleteOnboarding();
                return;
            }

            var step = _onboardingSteps[_currentOnboardingStep];
            
            OnboardingTip.Title = step.Title;
            OnboardingTip.Subtitle = step.Description;
            
            // Find the target element
            var target = FindName(step.TargetName) as FrameworkElement;
            if (target != null)
            {
                OnboardingTip.Target = target;
            }
            
            // Update button text for last step
            if (_currentOnboardingStep == _onboardingSteps.Count - 1)
            {
                OnboardingTip.ActionButtonContent = "Finish";
            }
            else
            {
                OnboardingTip.ActionButtonContent = $"Next ({_currentOnboardingStep + 1}/{_onboardingSteps.Count})";
            }
            
            OnboardingTip.IsOpen = true;
        }

        private void OnOnboardingNextClicked(TeachingTip sender, object args)
        {
            _currentOnboardingStep++;
            ShowCurrentOnboardingStep();
        }

        private void OnOnboardingSkipClicked(TeachingTip sender, object args)
        {
            CompleteOnboarding();
        }

        private void CompleteOnboarding()
        {
            OnboardingTip.IsOpen = false;
            _onboardingService.MarkProductOnboardingCompleted();
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

            UpdateBulkButtonsVisibility();
        }

        private void UpdateBulkButtonsVisibility()
        {
            var isSaleRole = ViewModel.IsSaleRole;

            if (FindName("ImportButton") is Button importButton)
            {
                importButton.Visibility = isSaleRole ? Visibility.Collapsed : Visibility.Visible;
            }

            if (FindName("DownloadTemplateButton") is Button downloadTemplateButton)
            {
                downloadTemplateButton.Visibility = isSaleRole ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private async void OnItemsPerPageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsPerPageCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out var itemsPerPage))
            {
                if (ViewModel == null)
                    ViewModel = App.Services.GetService(typeof(ProductViewModel)) as ProductViewModel;

                ViewModel.ItemsPerPage = itemsPerPage;
                Debug.WriteLine($"[PRODUCT_PAGE] Items per page changed to: {itemsPerPage}");
                
                // Reload products with new pagination
                await ViewModel.InitializeAsync();
                UpdateUIState();
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

        private async void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (ViewModel.SearchCommand.CanExecute(null))
            {
                await ViewModel.SearchCommand.ExecuteAsync(null);
                UpdateUIState();
            }
        }

        private void OnFilterToggleClick(object sender, RoutedEventArgs e)
        {
            if (FilterToggleButton.IsChecked == true)
            {
                FiltersPanel.Visibility = Visibility.Visible;
                FilterToggleIcon.Glyph = "\uE70E"; // Chevron Up
            }
            else
            {
                FiltersPanel.Visibility = Visibility.Collapsed;
                FilterToggleIcon.Glyph = "\uE70D"; // Chevron Down
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
                        var dialog = new ProductDetailDialog();
                        dialog.XamlRoot = this.XamlRoot;
                        dialog.SetProduct(product);
                        await dialog.ShowAsync();
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
                        var dialog = new EditProductDialog();
                        dialog.XamlRoot = this.XamlRoot;
                        dialog.SetProduct(product, ViewModel.Categories.Where(c => c.CategoryId > 0));

                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary && dialog.SelectedCategory != null)
                        {
                            await ViewModel.UpdateProductAsync(
                                dialog.ProductId,
                                dialog.Sku, dialog.ProductName, dialog.ImportPrice,
                                dialog.Count, dialog.Description, dialog.Images,
                                dialog.SelectedCategory.CategoryId);
                            UpdateUIState();
                        }
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
                            $"Are you sure you want to delete '{product.Name}'?\n\nThis action cannot be undone.");

                        if (confirmed)
                        {
                            var success = await ViewModel.DeleteProductAsync(productId);
                            if (success)
                            {
                                UpdateUIState();
                            }
                            else
                            {
                                await ShowErrorDialog("Error", "Failed to delete product. It may have associated orders.");
                            }
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

                var dialog = new AddProductDialog();
                dialog.XamlRoot = this.XamlRoot;
                dialog.SetCategories(ViewModel.Categories.Where(c => c.CategoryId > 0));

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.SelectedCategory != null)
                {
                    await ViewModel.CreateProductAsync(
                        dialog.Sku, dialog.ProductName, dialog.ImportPrice,
                        dialog.Count, dialog.Description, dialog.Images,
                        dialog.SelectedCategory.CategoryId);
                    UpdateUIState();
                }
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

                var dialog = new AddCategoryDialog();
                dialog.XamlRoot = this.XamlRoot;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.CreateCategoryAsync(
                        dialog.CategoryName, dialog.CategoryDescription);
                }
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

                // Create file picker
                var picker = new FileOpenPicker();
                
                // Get the window handle for WinUI 3
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".xlsx");
                picker.FileTypeFilter.Add(".xls");

                var file = await picker.PickSingleFileAsync();
                if (file == null)
                {
                    Debug.WriteLine("[PRODUCT_PAGE] Import cancelled - no file selected");
                    return;
                }

                Debug.WriteLine($"[PRODUCT_PAGE] Selected file: {file.Name}");

                // Read file and convert to Base64
                var buffer = await FileIO.ReadBufferAsync(file);
                var bytes = new byte[buffer.Length];
                using (var reader = DataReader.FromBuffer(buffer))
                {
                    reader.ReadBytes(bytes);
                }
                var fileBase64 = Convert.ToBase64String(bytes);

                // Show loading
                await ShowInfoDialog("Importing", $"Importing products from {file.Name}...\n\nPlease wait.");

                // Call ViewModel import method
                var result = await ViewModel.BulkImportProductsAsync(fileBase64);

                // Show result
                var resultMessage = $"Import completed!\n\n" +
                    $"✓ Created: {result.CreatedCount} products\n" +
                    $"✗ Failed: {result.FailedCount} products";

                if (result.Errors.Count > 0)
                {
                    resultMessage += "\n\nErrors:\n";
                    foreach (var error in result.Errors.Take(5))
                    {
                        resultMessage += $"• Row {error.Row}: {error.Message}\n";
                    }
                    if (result.Errors.Count > 5)
                    {
                        resultMessage += $"... and {result.Errors.Count - 5} more errors";
                    }
                }

                await ShowInfoDialog("Import Result", resultMessage);
                UpdateUIState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error importing: {ex.Message}");
                await ShowErrorDialog("Import Error", $"Failed to import products:\n\n{ex.Message}");
            }
        }

        private async void OnDownloadTemplateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("[PRODUCT_PAGE] Download template");

                var template = await ViewModel.DownloadTemplateAsync();

                if (template == null)
                {
                    await ShowErrorDialog("Error", "Failed to download template.");
                    return;
                }

                // Create file picker to save
                var picker = new FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = template.Filename;
                picker.FileTypeChoices.Add("Excel Files", new List<string> { ".xlsx" });

                var file = await picker.PickSaveFileAsync();
                if (file == null)
                {
                    Debug.WriteLine("[PRODUCT_PAGE] Template save cancelled");
                    return;
                }

                // Write file
                var bytes = Convert.FromBase64String(template.FileBase64);
                await FileIO.WriteBytesAsync(file, bytes);

                await ShowInfoDialog("Template Downloaded", $"Template saved to:\n{file.Path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_PAGE] Error downloading template: {ex.Message}");
                await ShowErrorDialog("Error", $"Failed to download template:\n\n{ex.Message}");
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
