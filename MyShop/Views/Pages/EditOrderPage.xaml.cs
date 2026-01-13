using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyShop.Views.Pages
{
    public sealed partial class EditOrderPage : Page, INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private Order? _currentOrder;
        private string _selectedStatus = "Created";
        private ObservableCollection<ReadOnlyOrderItem> _orderItems;
        private bool _isLoading = false;
        private ContentDialog? _currentDialog;

        public Order? CurrentOrder
        {
            get => _currentOrder;
            set => SetProperty(ref _currentOrder, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        public ObservableCollection<ReadOnlyOrderItem> OrderItems
        {
            get => _orderItems;
            set => SetProperty(ref _orderItems, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public EditOrderPage()
        {
            this.InitializeComponent();
            _orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            _orderItems = new ObservableCollection<ReadOnlyOrderItem>();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += EditOrderPage_SizeChanged;

            if (e.NavigationMode != NavigationMode.Back)
            {
                if (e.Parameter is Order order)
                {
                    _currentOrder = order;
                    await LoadOrderForEditAsync(order);
                }
            }
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

                // Update padding
                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_PAGE] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadOrderForEditAsync(Order order)
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                var token = GetAuthToken();
                var fullOrder = await _orderService.GetOrderByIdAsync(order.OrderId, token);

                if (fullOrder == null)
                {
                    await ShowErrorAsync("Order not found.");
                    return;
                }

                CurrentOrder = fullOrder;
                SelectedStatus = fullOrder.Status;
                OrderItems.Clear();

                foreach (var item in fullOrder.OrderItems)
                {
                    OrderItems.Add(new ReadOnlyOrderItem(item));
                }

                Debug.WriteLine($"[EDIT_ORDER_PAGE] ✓ Order #{order.OrderId} loaded for editing");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_PAGE] ✗ Error loading order: {ex.Message}");
                await ShowErrorAsync($"Failed to load order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (_currentOrder == null || _isLoading)
                return;

            await SaveOrderStatusAsync();
        }

        private async Task SaveOrderStatusAsync()
        {
            _isLoading = true;

            try
            {
                var token = GetAuthToken();

                Debug.WriteLine("[EDIT_ORDER_PAGE] Updating order status to: " + SelectedStatus);

                var updateInput = new UpdateOrderInput
                {
                    Status = SelectedStatus,
                    OrderItems = null
                };

                var updatedOrder = await _orderService.UpdateOrderAsync(_currentOrder!.OrderId, updateInput, token);

                if (updatedOrder != null)
                {
                    Debug.WriteLine($"[EDIT_ORDER_PAGE] ✓ Order #{_currentOrder.OrderId} status updated successfully");
                    await ShowSuccessAsync($"Order #{_currentOrder.OrderId} status changed to '{SelectedStatus}' successfully!");

                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    Debug.WriteLine("[EDIT_ORDER_PAGE] ✗ Failed to update order");
                    await ShowErrorAsync("Failed to update order. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_PAGE] ✗ Exception: {ex.Message}");
                await ShowErrorAsync($"Failed to update order: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
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
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog = null;
            }

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
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
                _currentDialog = null;
            }

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

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string? GetAuthToken()
        {
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            var token = sessionService?.GetAuthToken();
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("[EDIT_ORDER_PAGE] ✗ No authentication token available");
            }
            else
            {
                Debug.WriteLine("[EDIT_ORDER_PAGE] ✓ Authentication token retrieved");
            }
            
            return token;
        }
    }

    /// <summary>
    /// Read-only order item - no quantity editing allowed
    /// </summary>
    public class ReadOnlyOrderItem
    {
        public OrderItem OrderItem { get; }

        public int Quantity => OrderItem.Quantity;

        public int TotalPrice => (int)(OrderItem.UnitSalePrice * OrderItem.Quantity);

        public ReadOnlyOrderItem(OrderItem orderItem)
        {
            OrderItem = orderItem;
        }
    }
}