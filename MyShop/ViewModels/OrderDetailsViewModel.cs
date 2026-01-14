using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class OrderDetailsViewModel : INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private readonly ISessionService _sessionService;
        private readonly OrderExportService _exportService;

        private Order? _currentOrder;
        private ObservableCollection<OrderItem> _orderItems = new();
        private bool _isLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        public OrderDetailsViewModel(
            IOrderService orderService,
            ISessionService sessionService,
            OrderExportService exportService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
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

        public ObservableCollection<OrderItem> OrderItems
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

        public async Task LoadOrderDetailsAsync(Order order)
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                Debug.WriteLine($"[ORDER_DETAILS_VM] Loading order #{order.OrderId}");

                var token = _sessionService.GetAuthToken();
                var fullOrder = await _orderService.GetOrderByIdAsync(order.OrderId, token);

                if (fullOrder == null)
                {
                    throw new Exception("Order not found.");
                }

                CurrentOrder = fullOrder;
                OrderItems.Clear();

                if (fullOrder.OrderItems != null)
                {
                    foreach (var item in fullOrder.OrderItems)
                    {
                        OrderItems.Add(item);
                    }
                    Debug.WriteLine($"[ORDER_DETAILS_VM] ✓ Loaded {OrderItems.Count} order items");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS_VM] ✗ Error loading order: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExportToFormatAsync(string format, Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            if (CurrentOrder == null)
            {
                throw new InvalidOperationException("No order to export.");
            }

            try
            {
                Debug.WriteLine($"[ORDER_DETAILS_VM] Exporting order #{CurrentOrder.OrderId} to {format.ToUpper()}...");
                await _exportService.ExportOrderAsync(CurrentOrder, xamlRoot, format);
                Debug.WriteLine($"[ORDER_DETAILS_VM] ✓ Export completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS_VM] ✗ Export failed: {ex.Message}");
                throw;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}