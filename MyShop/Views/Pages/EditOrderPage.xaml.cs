using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using MyShop.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class EditOrderPage : Page
    {
        private EditOrderViewModel _viewModel;
        private ContentDialog? _currentDialog;

        public EditOrderPage()
        {
            this.InitializeComponent();
            
            var orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            var promotionService = (App.Services.GetService(typeof(IPromotionService)) as IPromotionService)!;
            var sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _viewModel = new EditOrderViewModel(orderService, promotionService, sessionService);
            
            DataContext = _viewModel;
            PromotionComboBox.DropDownOpened += PromotionComboBox_DropDownOpened;
        }

        private void PromotionComboBox_DropDownOpened(object sender, object e)
        {
            try
            {
                Debug.WriteLine("[EDIT_ORDER_PAGE] Promotion ComboBox opened");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_PAGE] Promotion ComboBox open handler error: {ex}");
            }
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += EditOrderPage_SizeChanged;

            if (e.NavigationMode != NavigationMode.Back && e.Parameter is Order order)
            {
                await LoadOrderAsync(order);
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= EditOrderPage_SizeChanged;
        }

        private void EditOrderPage_SizeChanged(object sender, SizeChangedEventArgs e)
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

                Debug.WriteLine($"[EDIT_ORDER_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadOrderAsync(Order order)
        {
            try
            {
                await _viewModel.LoadOrderForEditAsync(order);
                try
                {
                    _viewModel.NotifySelectedPromotion();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EDIT_ORDER_PAGE] ✗ NotifySelectedPromotion error: {ex}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load order: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var success = await _viewModel.SaveOrderStatusAsync();
                
                if (success)
                {
                    await ShowSuccessAsync($"Order #{_viewModel.CurrentOrder!.OrderId} status changed to '{_viewModel.SelectedStatus}' successfully!");
                    
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    await ShowErrorAsync("Failed to update order. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to update order: {ex.Message}");
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