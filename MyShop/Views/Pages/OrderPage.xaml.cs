using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using MyShop.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderPage : Page
    {
        private OrderViewModel _viewModel;
        private OnboardingService _onboardingService;
        private ContentDialog? _currentDialog;
        private bool _isInitialized = false;
        
        private List<OnboardingStep> _onboardingSteps;
        private int _currentOnboardingStep = 0;

        public OrderPage()
        {
            this.InitializeComponent();
            
            var orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            var sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _onboardingService = (App.Services.GetService(typeof(OnboardingService)) as OnboardingService)!;
            
            _viewModel = new OrderViewModel(orderService, sessionService);
            
            DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += OrderPage_SizeChanged;
            
            _isInitialized = true;
            await _viewModel.LoadOrdersAsync();
            
            // Start onboarding if first time
            if (!_onboardingService.IsOnboardingCompleted())
            {
                StartOnboarding();
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= OrderPage_SizeChanged;
        }

        private void StartOnboarding()
        {
            _onboardingSteps = _onboardingService.GetOrderPageSteps();
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
            _onboardingService.MarkOnboardingCompleted();
        }

        private void OrderPage_SizeChanged(object sender, SizeChangedEventArgs e)
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

                Debug.WriteLine($"[ORDER_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task ShowErrorDialogAsync(string message)
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

        private async Task ShowSuccessDialogAsync(string message)
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

        private async Task<ContentDialogResult> ShowConfirmDialogAsync(string title, string content)
        {
            await CloseCurrentDialogAsync();

            _currentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await _currentDialog.ShowAsync();
            _currentDialog = null;
            return result;
        }

        private async Task CloseCurrentDialogAsync()
        {
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog = null;
            }
        }

        private void OnAddOrderClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddOrderPage));
        }

        private async void OnViewOrderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var order = (sender as Button)?.DataContext as Order;
                if (order == null)
                {
                    await ShowErrorDialogAsync("Unable to identify order to view.");
                    return;
                }

                Frame.Navigate(typeof(OrderDetailsPage), order);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to view order: {ex.Message}");
            }
        }

        private async void OnEditOrderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var order = (sender as Button)?.DataContext as Order;
                if (order == null)
                {
                    await ShowErrorDialogAsync("Unable to identify order to edit.");
                    return;
                }

                if (order.Status != "Created")
                {
                    await ShowErrorDialogAsync($"Cannot edit order with status '{order.Status}'. Only orders with 'Created' status can be edited.");
                    return;
                }

                Frame.Navigate(typeof(EditOrderPage), order);
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to edit order: {ex.Message}");
            }
        }

        private async void OnDeleteOrderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var order = (sender as Button)?.DataContext as Order;
                if (order == null)
                {
                    await ShowErrorDialogAsync("Unable to identify order to delete.");
                    return;
                }

                var result = await ShowConfirmDialogAsync(
                    "Confirm Delete",
                    $"Are you sure you want to delete Order #{order.OrderId}?"
                );

                if (result == ContentDialogResult.Primary)
                {
                    await _viewModel.DeleteOrderAsync(order.OrderId);
                    await ShowSuccessDialogAsync($"Order #{order.OrderId} deleted successfully!");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Failed to delete order: {ex.Message}");
            }
        }

        private async void OnPreviousPageClicked(object sender, RoutedEventArgs e)
        {
            await _viewModel.NavigateToPreviousPageAsync();
        }

        private async void OnNextPageClicked(object sender, RoutedEventArgs e)
        {
            await _viewModel.NavigateToNextPageAsync();
        }

        private async void OnDateRangeChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            _viewModel.FromDate = FromDatePicker.Date?.DateTime;
            _viewModel.ToDate = ToDatePicker.Date?.DateTime;
            await _viewModel.ChangeDateRangeAsync();
        }

        private async void OnClearFilterClicked(object sender, RoutedEventArgs e)
        {
            FromDatePicker.Date = null;
            ToDatePicker.Date = null;
            await _viewModel.ResetFiltersAsync();
        }

        private async void OnResetFromDateClicked(object sender, RoutedEventArgs e)
        {
            FromDatePicker.Date = null;
            _viewModel.FromDate = null;
            await _viewModel.ChangeDateRangeAsync();
        }

        private async void OnResetToDateClicked(object sender, RoutedEventArgs e)
        {
            ToDatePicker.Date = null;
            _viewModel.ToDate = null;
            await _viewModel.ChangeDateRangeAsync();
        }

        private async void OnSortCriteriaChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            var selectedItem = SortCriteriaCombo.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag is string criteria)
            {
                await _viewModel.ChangeSortCriteriaAsync(criteria);
            }
        }

        private async void OnSortOrderChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            var selectedItem = SortOrderCombo.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag is string order)
            {
                await _viewModel.ChangeSortOrderAsync(order);
            }
        }

        private async void OnItemsPerPageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            var selectedItem = ItemsPerPageCombo.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag is string tag && int.TryParse(tag, out var itemsPerPage))
            {
                await _viewModel.ChangeItemsPerPageAsync(itemsPerPage);
            }
        }
    }
}