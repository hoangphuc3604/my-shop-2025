using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

            // Only load order if navigating forward, not when going back
            if (e.NavigationMode != NavigationMode.Back)
            {
                if (e.Parameter is Order order)
                {
                    _currentOrder = order;
                    await LoadOrderForEditAsync(order);
                }
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
            }
            catch (Exception ex)
            {
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

                System.Diagnostics.Debug.WriteLine("[EDIT_ORDER_PAGE] Updating order status to: " + SelectedStatus);

                // Build update input with ONLY status
                var updateInput = new UpdateOrderInput
                {
                    Status = SelectedStatus,
                    OrderItems = null // UpdateOrderInput doesn't accept orderItems field
                };

                // Update order via GraphQL API
                var updatedOrder = await _orderService.UpdateOrderAsync(_currentOrder!.OrderId, updateInput, token);

                if (updatedOrder != null)
                {
                    await ShowSuccessAsync($"Order #{_currentOrder.OrderId} status changed to '{SelectedStatus}' successfully!");

                    // Navigate back
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
            // Get token from session storage
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            var token = sessionService?.GetAuthToken();
            
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("[EDIT_ORDER_PAGE] ✗ No authentication token available");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[EDIT_ORDER_PAGE] ✓ Authentication token retrieved");
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