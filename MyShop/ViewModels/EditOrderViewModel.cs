using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class EditOrderViewModel : INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private readonly ISessionService _sessionService;

        private Order? _currentOrder;
        private string _selectedStatus = "Created";
        private ObservableCollection<ReadOnlyOrderItem> _orderItems = new();
        private bool _isLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        public EditOrderViewModel(IOrderService orderService, ISessionService sessionService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public Order? CurrentOrder
        {
            get => _currentOrder;
            set
            {
                if (_currentOrder != value)
                {
                    _currentOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ReadOnlyOrderItem> OrderItems
        {
            get => _orderItems;
            set
            {
                if (_orderItems != value)
                {
                    _orderItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task LoadOrderForEditAsync(Order order)
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                var token = _sessionService.GetAuthToken();
                var fullOrder = await _orderService.GetOrderByIdAsync(order.OrderId, token);

                if (fullOrder == null)
                {
                    throw new Exception("Order not found.");
                }

                CurrentOrder = fullOrder;
                SelectedStatus = fullOrder.Status;
                OrderItems.Clear();

                foreach (var item in fullOrder.OrderItems)
                {
                    OrderItems.Add(new ReadOnlyOrderItem(item));
                }

                Debug.WriteLine($"[EDIT_ORDER_VM] ✓ Order #{order.OrderId} loaded for editing");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_VM] ✗ Error loading order: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<bool> SaveOrderStatusAsync()
        {
            if (CurrentOrder == null || IsLoading)
                return false;

            IsLoading = true;

            try
            {
                var token = _sessionService.GetAuthToken();

                Debug.WriteLine($"[EDIT_ORDER_VM] Updating order status to: {SelectedStatus}");

                var updateInput = new UpdateOrderInput
                {
                    Status = SelectedStatus,
                    OrderItems = null
                };

                var updatedOrder = await _orderService.UpdateOrderAsync(CurrentOrder.OrderId, updateInput, token);

                if (updatedOrder != null)
                {
                    Debug.WriteLine($"[EDIT_ORDER_VM] ✓ Order #{CurrentOrder.OrderId} status updated successfully");
                    return true;
                }
                else
                {
                    Debug.WriteLine("[EDIT_ORDER_VM] ✗ Failed to update order");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_VM] ✗ Exception: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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