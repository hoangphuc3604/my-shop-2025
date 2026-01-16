using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Services;
using MyShop.ViewModels;
using MyShop.Contracts;
using MyShop.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyShop.Views.Pages
{
    public sealed partial class PromotionPage : Page
    {
        public PromotionViewModel ViewModel { get; set; }
        private ISessionService? _sessionService;
        private IAuthorizationService? _authorizationService;

        public PromotionPage()
        {
            this.InitializeComponent();
            var viewModel = App.Services.GetService(typeof(PromotionViewModel)) as PromotionViewModel;
            ViewModel = viewModel ?? throw new InvalidOperationException("PromotionViewModel could not be resolved from services");
            _sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            _authorizationService = App.Services.GetService(typeof(IAuthorizationService)) as IAuthorizationService;
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                if (ViewModel.Promotions != null)
                    ViewModel.Promotions.CollectionChanged += Promotions_CollectionChanged;
            }
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += PromotionPage_SizeChanged;

            try
            {
                Debug.WriteLine("[PROMOTION_PAGE] OnNavigatedTo - Initializing...");
                await ViewModel.InitializeAsync();
                UpdateUIState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_PAGE] Error in OnNavigatedTo: {ex.Message}");
                await ShowErrorDialog("Initialization Error", "Failed to load promotion page. Please try again or refresh.");
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= PromotionPage_SizeChanged;
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                if (ViewModel.Promotions != null)
                    ViewModel.Promotions.CollectionChanged -= Promotions_CollectionChanged;
            }
        }

        private void PromotionPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e == null) return;

            if (e.PropertyName == nameof(PromotionViewModel.IsLoading) ||
                e.PropertyName == nameof(PromotionViewModel.TotalCount))
            {
                DispatcherQueue.TryEnqueue(UpdateUIState);
            }

            if (e.PropertyName == nameof(PromotionViewModel.Promotions))
            {
                if (sender is PromotionViewModel vm)
                {
                    vm.Promotions.CollectionChanged += Promotions_CollectionChanged;
                }
                DispatcherQueue.TryEnqueue(UpdateUIState);
            }
        }

        private void Promotions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(UpdateUIState);
        }

        private void ApplyResponsiveLayout(double width, double height)
        {
            try
            {
                var viewportSize = ResponsiveService.GetCurrentViewportSize(width, height);
                var isCompact = ResponsiveService.IsCompactLayout(width);
                var padding = ResponsiveService.GetOptimalPadding(width);

                Debug.WriteLine($"[PROMOTION_PAGE] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private void UpdateUIState()
        {
            LoadingPanel.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;

            if (!ViewModel.IsLoading && ViewModel.Promotions.Count == 0)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                PromotionsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                PromotionsListView.Visibility = Visibility.Visible;
            }
        }

        private async void OnDateRangeChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (ViewModel.LoadPromotionsCommand.CanExecute(null))
            {
                await ViewModel.LoadPromotionsCommand.ExecuteAsync(null);
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

        private async void OnViewPromotionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int promotionId)
                {
                    Debug.WriteLine($"[PROMOTION_PAGE] View promotion #{promotionId}");
                    var promotion = ViewModel.Promotions.FirstOrDefault(p => p.PromotionId == promotionId);
                    if (promotion != null)
                    {
                        await ShowInfoDialog("View Promotion", $"Viewing promotion: {promotion.Code}\n\n{promotion.Description}\n\nDiscount: {promotion.DiscountValue}%");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_PAGE] Error viewing promotion: {ex.Message}");
                await ShowErrorDialog("Error", "Failed to view promotion. Please try again.");
            }
        }

        private async void OnEditPromotionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int promotionId)
                {
                    Debug.WriteLine($"[PROMOTION_PAGE] Edit promotion #{promotionId}");
                    var promotion = ViewModel.Promotions.FirstOrDefault(p => p.PromotionId == promotionId);
                    if (promotion != null && _authorizationService?.HasPermission("UPDATE_PROMOTIONS") == true)
                    {
                        var dialog = new MyShop.Views.Dialogs.EditPromotionDialog();
                        dialog.XamlRoot = this.XamlRoot;
                        dialog.SetPromotion(promotion);
                        var (updated, error) = await dialog.ShowAndSaveAsync(ViewModel);
                        if (updated)
                        {
                            await ViewModel.LoadPromotionsCommand.ExecuteAsync(null);
                            UpdateUIState();
                        }
                        else if (!string.IsNullOrEmpty(error))
                        {
                            await ShowErrorDialog("Validation Error", error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_PAGE] Error editing promotion: {ex.Message}");
                await ShowErrorDialog("Error", "Failed to edit promotion. Please try again.");
            }
        }

        private async void OnDeletePromotionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int promotionId)
                {
                    Debug.WriteLine($"[PROMOTION_PAGE] Delete promotion #{promotionId}");
                    var promotion = ViewModel.Promotions.FirstOrDefault(p => p.PromotionId == promotionId);
                    if (promotion != null && _authorizationService?.HasPermission("DELETE_PROMOTIONS") == true)
                    {
                        var confirmed = await ShowConfirmDialog(
                            "Delete Promotion",
                            $"Are you sure you want to delete '{promotion.Code}'?\n\nThis action cannot be undone.");

                        if (confirmed)
                        {
                            var token = _sessionService?.GetAuthToken();
                            var success = await ViewModel.DeletePromotionAsync(promotionId, token);

                            if (success)
                            {
                                await ViewModel.LoadPromotionsCommand.ExecuteAsync(null);
                                UpdateUIState();
                            }
                            else
                            {
                                await ShowErrorDialog("Error", "Failed to delete promotion.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_PAGE] Error deleting promotion: {ex.Message}");
                await ShowErrorDialog("Error", "Failed to delete promotion. Please try again.");
            }
        }

        private async void OnAddPromotionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("[PROMOTION_PAGE] Add new promotion");
                if (_authorizationService?.HasPermission("CREATE_PROMOTIONS") != true)
                {
                    await ShowErrorDialog("Access Denied", "You don't have permission to create promotions.");
                    return;
                }

                var dialog = new MyShop.Views.Dialogs.AddPromotionDialog();
                dialog.XamlRoot = this.XamlRoot;
                var (created, error) = await dialog.ShowAndCreateAsync(ViewModel);
                if (created)
                {
                    await ViewModel.LoadPromotionsCommand.ExecuteAsync(null);
                    UpdateUIState();
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    await ShowErrorDialog("Validation Error", error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_PAGE] Error adding promotion: {ex.Message}");
                await ShowErrorDialog("Error", "Failed to add promotion. Please try again.");
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

        private void OnOnboardingNextClicked(TeachingTip sender, object args)
        {
            OnboardingTip.IsOpen = false;
        }

        private void OnOnboardingSkipClicked(TeachingTip sender, object args)
        {
            OnboardingTip.IsOpen = false;
        }
    }
}
