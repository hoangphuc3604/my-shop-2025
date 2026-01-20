using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public partial class ProductViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ISessionService _sessionService;
        private readonly IAuthorizationService _authorizationService;

        // Collections
        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private ObservableCollection<Category> _categories = new();

        // Selected items
        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private Category? _selectedCategory;

        // Search and filters
        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        // Price filters (use 0 to represent "no filter")
        [ObservableProperty]
        private double _minPrice = 0;

        [ObservableProperty]
        private double _maxPrice = 0;

        // Convert to nullable for service calls
        private double? MinPriceFilter => MinPrice > 0 ? MinPrice : null;
        private double? MaxPriceFilter => MaxPrice > 0 ? MaxPrice : null;

        [ObservableProperty]
        private string? _selectedSortCriteria;

        public List<string> SortOptions { get; } = new List<string>
        {
            "None",
            "Name (A-Z)",
            "Name (Z-A)",
            "Price (Low to High)",
            "Price (High to Low)",
            "Stock (Low to High)",
            "Stock (High to Low)",
            "Newest First",
            "Oldest First"
        };

        // Pagination
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalCount = 0;

        [ObservableProperty]
        private bool _hasPreviousPage;

        [ObservableProperty]
        private bool _hasNextPage;

        // UI State
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        // Permissions
        [ObservableProperty]
        private bool _canCreateProducts;

        [ObservableProperty]
        private bool _canUpdateProducts;

        [ObservableProperty]
        private bool _canDeleteProducts;

        [ObservableProperty]
        private bool _canCreateCategories;

        [ObservableProperty]
        private bool _isSaleRole;

        public ProductViewModel(
            IProductService productService,
            ICategoryService categoryService,
            ISessionService sessionService,
            IAuthorizationService authorizationService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _sessionService = sessionService;
            _authorizationService = authorizationService;

            SelectedSortCriteria = "None";
            InitializePermissions();
        }

        private void InitializePermissions()
        {
            CanCreateProducts = _authorizationService.HasPermission("CREATE_PRODUCTS");
            CanUpdateProducts = _authorizationService.HasPermission("UPDATE_PRODUCTS");
            CanDeleteProducts = _authorizationService.HasPermission("DELETE_PRODUCTS");
            CanCreateCategories = _authorizationService.HasPermission("CREATE_CATEGORIES");
            IsSaleRole = _authorizationService.GetRole() == "SALE";
        }

        /// <summary>
        /// Initialize and load data when page is first shown
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await LoadProductsAsync();
        }

        /// <summary>
        /// Load categories for filter dropdown
        /// </summary>
        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                var categories = await _categoryService.GetCategoriesAsync(1, 100, null, token);

                Categories.Clear();
                
                // Add "All Categories" option
                Categories.Add(new Category { CategoryId = 0, Name = "All Categories" });
                
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Set default selection
                if (SelectedCategory == null && Categories.Any())
                {
                    SelectedCategory = Categories.First();
                }

                Debug.WriteLine($"[PRODUCT_VM] Loaded {categories.Count} categories");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_VM] Error loading categories: {ex.Message}");
                StatusMessage = "Failed to load categories";
            }
        }

        /// <summary>
        /// Load products with current filters and pagination
        /// </summary>
        [RelayCommand]
        private async Task LoadProductsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Loading products...";

                var token = _sessionService.GetAuthToken();
                
                // Get category ID for filter (0 means all categories)
                var categoryId = SelectedCategory?.CategoryId > 0 ? SelectedCategory.CategoryId : (int?)null;

                Debug.WriteLine($"[PRODUCT_VM] Loading products - Page: {CurrentPage}, Category: {categoryId?.ToString() ?? "all"}, Sort: {SelectedSortCriteria ?? "none"}");

                // Load products - pass raw sort criteria, ProductService will map to backend enum
                var products = await _productService.GetProductsAsync(
                    CurrentPage,
                    PageSize,
                    categoryId,
                    MinPriceFilter,
                    MaxPriceFilter,
                    string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim(),
                    SelectedSortCriteria,  // Pass raw UI string, ProductService will map to backend
                    token);

                // Load total count for pagination
                var totalCount = await _productService.GetTotalProductCountAsync(
                    categoryId,
                    MinPriceFilter,
                    MaxPriceFilter,
                    string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim(),
                    token);

                // Update products collection
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                // Update pagination state
                TotalCount = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                
                if (TotalPages < 1) TotalPages = 1;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                // Calculate pagination flags (actual notify happens in finally after IsLoading = false)
                HasPreviousPage = CurrentPage > 1;
                HasNextPage = CurrentPage < TotalPages;

                StatusMessage = $"Showing {Products.Count} of {TotalCount} products";
                
                Debug.WriteLine($"[PRODUCT_VM] Loaded {Products.Count} products, Total: {TotalCount}");
                Debug.WriteLine($"[PRODUCT_VM] Pagination: Page {CurrentPage}/{TotalPages}, HasPrev={HasPreviousPage}, HasNext={HasNextPage}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_VM] Error loading products: {ex.Message}");
                StatusMessage = $"Error: {ex.Message}";
                Products.Clear();
            }
            finally
            {
                IsLoading = false;
                UpdatePaginationState();
            }
        }

        /// <summary>
        /// Go to previous page
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadProductsAsync();
            }
        }

        private bool CanGoPrevious() => HasPreviousPage && !IsLoading;

        /// <summary>
        /// Go to next page
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadProductsAsync();
            }
        }

        private bool CanGoNext() => HasNextPage && !IsLoading;

        /// <summary>
        /// Apply search filter
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1; // Reset to first page when searching
            await LoadProductsAsync();
        }

        /// <summary>
        /// Apply category filter
        /// </summary>
        [RelayCommand]
        private async Task ApplyCategoryFilterAsync()
        {
            CurrentPage = 1;
            await LoadProductsAsync();
        }

        /// <summary>
        /// Apply price range filter
        /// </summary>
        [RelayCommand]
        private async Task ApplyPriceFilterAsync()
        {
            CurrentPage = 1;
            await LoadProductsAsync();
        }

        /// <summary>
        /// Apply sorting
        /// </summary>
        [RelayCommand]
        private async Task ApplySortAsync()
        {
            await LoadProductsAsync();
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            SearchKeyword = string.Empty;
            MinPrice = 0;
            MaxPrice = 0;
            SelectedSortCriteria = "None";
            
            if (Categories.Any())
            {
                SelectedCategory = Categories.First(); // "All Categories"
            }

            CurrentPage = 1;
            await LoadProductsAsync();
        }

        /// <summary>
        /// Refresh products list
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadProductsAsync();
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        public async Task<bool> CreateProductAsync(
            string sku, string name, int importPrice, int count,
            string? description, List<ProductImageInput> images, int categoryId)
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                var result = await _productService.CreateProductAsync(
                    sku, name, importPrice, count, description, images, categoryId, token);
                
                if (result != null)
                {
                    await LoadProductsAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_VM] Error creating product: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        public async Task<bool> UpdateProductAsync(
            int productId, string sku, string name, int importPrice, int count,
            string? description, List<ProductImageInput> images, int categoryId)
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                var result = await _productService.UpdateProductAsync(
                    productId, sku, name, importPrice, count, description, images, categoryId, token);
                
                if (result != null)
                {
                    await LoadProductsAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_VM] Error updating product: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                var success = await _productService.DeleteProductAsync(productId, token);
                
                if (success)
                {
                    await LoadProductsAsync();
                }
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_VM] Error deleting product: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        public async Task<bool> CreateCategoryAsync(string name, string? description)
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                await _categoryService.CreateCategoryAsync(name, description, token);
                await LoadCategoriesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRODUCT_VM] Error creating category: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Bulk import products from Excel
        /// </summary>
        public async Task<BulkImportResult> BulkImportProductsAsync(string fileBase64)
        {
            var token = _sessionService.GetAuthToken();
            var result = await _productService.BulkImportProductsAsync(fileBase64, token);
            await LoadProductsAsync();
            return result;
        }

        /// <summary>
        /// Download Excel template
        /// </summary>
        public async Task<TemplateFile?> DownloadTemplateAsync()
        {
            var token = _sessionService.GetAuthToken();
            return await _productService.DownloadTemplateAsync(token);
        }

        /// <summary>
        /// Update pagination button states
        /// </summary>
        private void UpdatePaginationState()
        {
            HasPreviousPage = CurrentPage > 1;
            HasNextPage = CurrentPage < TotalPages;

            // Update command can execute states
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }

        // Add this property to ProductViewModel to match usage in ProductPage.xaml.cs
        public int ItemsPerPage
        {
            get => PageSize;
            set => PageSize = value;
        }
    }
}
