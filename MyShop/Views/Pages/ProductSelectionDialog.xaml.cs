using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.Data;
using MyShop.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;

namespace MyShop.Views.Pages
{
    public sealed partial class ProductSelectionDialog : ContentDialog
    {
        private List<ProductSelection> _productSelections = new();
        private readonly MyShopDbContext _dbContext;

        public Dictionary<int, int> SelectedProducts { get; private set; } = new();
        public int TotalPrice { get; private set; }

        public ProductSelectionDialog()
        {
            InitializeComponent();
            _dbContext = (App.Services.GetService(typeof(MyShopDbContext)) as MyShopDbContext)!;
            PrimaryButtonClick += OnPrimaryButtonClick;
        }

        public async Task LoadProductsAsync(MyShopDbContext? dbContext = null)
        {
            try
            {
                var products = await _dbContext.Products
                    .OrderBy(p => p.ProductId)
                    .ToListAsync();
                
                _productSelections = products.Select(p => new ProductSelection 
                { 
                    Product = p, 
                    Quantity = 0,
                    OnQuantityChangedCallback = UpdateSummary
                }).ToList();
                
                ProductsDataGrid.ItemsSource = _productSelections;
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load products: {ex.Message}",
                    CloseButtonText = "Close",
                    XamlRoot = XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text?.ToLower() ?? string.Empty;
            
            var filteredProducts = string.IsNullOrEmpty(searchText)
                ? _productSelections
                : _productSelections
                    .Where(p => p.Product.Name.ToLower().Contains(searchText) || 
                               p.Product.Sku.ToLower().Contains(searchText))
                    .OrderBy(p => p.Product.ProductId)
                    .ToList();

            ProductsDataGrid.ItemsSource = filteredProducts;
        }

        private void OnQuantityValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Get the data context (ProductSelection) from the NumberBox
            if (sender.DataContext is ProductSelection selection)
            {
                // Parse the new value and update the quantity
                int newValue = (int)args.NewValue;
                if (newValue >= 0 && newValue <= selection.Product.Count)
                {
                    selection.Quantity = newValue;
                }
            }
        }

        private void OnQuantityLoseFocus(object sender, RoutedEventArgs e)
        {
            // Also update when the NumberBox loses focus to catch any remaining changes
            if (sender is NumberBox numberBox && numberBox.DataContext is ProductSelection selection)
            {
                selection.Quantity = (int)numberBox.Value;
            }
        }

        private void UpdateSummary()
        {
            var selectedCount = _productSelections.Count(s => s.Quantity > 0);
            var totalPrice = _productSelections.Sum(s => s.TotalPrice);

            SelectedCountText.Text = selectedCount.ToString();
            TotalPriceText.Text = $"{totalPrice:N0} ₫";
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedProducts.Clear();
            TotalPrice = 0;

            foreach (var selection in _productSelections.Where(s => s.Quantity > 0))
            {
                SelectedProducts[selection.Product.ProductId] = selection.Quantity;
                TotalPrice += selection.TotalPrice;
            }

            if (SelectedProducts.Count == 0)
            {
                args.Cancel = true;
            }
        }
    }

    public class ProductSelection : INotifyPropertyChanged
    {
        private int _quantity;
        private Action? _onQuantityChangedCallback;

        public Product Product { get; set; } = new();

        public Action? OnQuantityChangedCallback
        {
            get => _onQuantityChangedCallback;
            set => _onQuantityChangedCallback = value;
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                    OnQuantityChangedCallback?.Invoke();
                }
            }
        }

        public int TotalPrice => Product.ImportPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}