using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Services;
using MyShop.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI.Controls;

namespace MyShop.Views.Pages
{
    public sealed partial class AddOrderPage : Page
    {
        public AddOrderViewModel ViewModel { get; set; }
        private AddOrderViewModel _viewModel;
        private ContentDialog? _currentDialog;
        private bool _isInitialized;

        public AddOrderPage()
        {
            this.InitializeComponent();

            var viewModel = App.Services.GetService(typeof(AddOrderViewModel)) as AddOrderViewModel;
            ViewModel = viewModel ?? throw new InvalidOperationException("AddOrderViewModel could not be resolved from services");
            _viewModel = ViewModel;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += AddOrderPage_SizeChanged;

            if (e.NavigationMode != NavigationMode.Back)
            {
                _isInitialized = true;
                await LoadProductsAsync();
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= AddOrderPage_SizeChanged;
            _isInitialized = false;
        }

        private void AddOrderPage_SizeChanged(object sender, SizeChangedEventArgs e)
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

                Debug.WriteLine($"[ADD_ORDER_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                if (ProductsDataGrid?.Columns.Count > 0)
                {
                    if (isCompact)
                    {
                        foreach (var column in ProductsDataGrid.Columns)
                        {
                            if (column.Header?.ToString() == "Description" || column.Header?.ToString() == "Category")
                            {
                                column.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                            }
                        }
                    }
                    else
                    {
                        foreach (var column in ProductsDataGrid.Columns)
                        {
                            column.Visibility = Visibility.Visible;
                            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                        }
                    }
                }

                if (TopPanel != null) TopPanel.Padding = new Thickness(padding);
                if (SummaryPanel != null) SummaryPanel.Padding = new Thickness(padding);

                if (ButtonPanel != null)
                {
                    ButtonPanel.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
                }

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADD_ORDER_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                await _viewModel.LoadProductsAsync();
                ProductsDataGrid.ItemsSource = _viewModel.FilteredProducts;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized || _viewModel == null)
                return;

            _viewModel.SearchText = SearchBox.Text ?? string.Empty;
            ProductsDataGrid.ItemsSource = _viewModel.FilteredProducts;
        }

        private void OnQuantityValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is ProductSelection selection)
            {
                int newValue = (int)args.NewValue;
                if (newValue >= 0 && newValue <= selection.Product.Count)
                {
                    selection.Quantity = newValue;
                }
            }
        }

        private async void OnCreateOrderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var newOrder = await _viewModel.CreateOrderAsync();
                
                if (newOrder != null)
                {
                    await ShowSuccessAsync($"Order #{newOrder.OrderId} created successfully with {_viewModel.SelectedCount} product(s)!");
                    
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                await ShowErrorAsync(ex.Message);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to create order: {ex.Message}");
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            await CloseCurrentDialogAsync();

            _currentDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await _currentDialog.ShowAsync();
            _currentDialog = null;
        }

        private async Task ShowSuccessAsync(string message)
        {
            await CloseCurrentDialogAsync();

            _currentDialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await _currentDialog.ShowAsync();
            _currentDialog = null;
        }

        private async Task CloseCurrentDialogAsync()
        {
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog = null;
            }
        }
    }
}