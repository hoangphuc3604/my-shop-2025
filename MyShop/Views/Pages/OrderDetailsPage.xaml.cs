using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderDetailsPage : Page, INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private readonly ISessionService _sessionService;
        private readonly OrderExportService _exportService;
        private Order? _currentOrder;
        private ObservableCollection<OrderItem> _orderItems;

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public OrderDetailsPage()
        {
            this.InitializeComponent();
            _orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            _sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _exportService = new OrderExportService();
            _orderItems = new ObservableCollection<OrderItem>();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += OrderDetailsPage_SizeChanged;

            if (e.Parameter is Order order)
            {
                await LoadOrderDetailsAsync(order);
            }
        }

        private void OrderDetailsPage_SizeChanged(object sender, SizeChangedEventArgs e)
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

                Debug.WriteLine($"[ORDER_DETAILS] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                // Update padding
                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadOrderDetailsAsync(Order order)
        {
            try
            {
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[ORDER_DETAILS] LOADING ORDER #{order.OrderId}");
                Debug.WriteLine("════════════════════════════════════════");

                var token = GetAuthToken();
                var fullOrder = await _orderService.GetOrderByIdAsync(order.OrderId, token);

                if (fullOrder == null)
                {
                    await ShowErrorAsync("Order not found.");
                    Debug.WriteLine("[ORDER_DETAILS] ✗ Order not found");
                    return;
                }

                CurrentOrder = fullOrder;
                OrderItems.Clear();
                
                if (fullOrder.OrderItems != null)
                {
                    foreach (var item in fullOrder.OrderItems)
                    {
                        OrderItems.Add(item);
                    }
                    Debug.WriteLine($"[ORDER_DETAILS] ✓ Loaded {OrderItems.Count} order items");
                }

                Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS] ✗ Error loading order: {ex.Message}");
                await ShowErrorAsync($"Failed to load order details: {ex.Message}");
            }
        }

        private string? GetAuthToken()
        {
            var token = _sessionService?.GetAuthToken();
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("[ORDER_DETAILS] ✗ No authentication token available");
            }
            else
            {
                Debug.WriteLine("[ORDER_DETAILS] ✓ Authentication token retrieved");
            }
            
            return token;
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void OnExportToPdfClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentOrder == null)
            {
                await ShowErrorAsync("No order to export.");
                return;
            }

            try
            {
                Debug.WriteLine($"[ORDER_DETAILS] Exporting order #{CurrentOrder.OrderId} to PDF...");
                await _exportService.ExportOrderAsync(CurrentOrder, this.XamlRoot, "pdf");
                Debug.WriteLine($"[ORDER_DETAILS] ✓ Export completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS] ✗ Export failed: {ex.Message}");
            }
        }

        private async void OnExportToXpsClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentOrder == null)
            {
                await ShowErrorAsync("No order to export.");
                return;
            }

            try
            {
                Debug.WriteLine($"[ORDER_DETAILS] Exporting order #{CurrentOrder.OrderId} to XPS...");
                await _exportService.ExportOrderAsync(CurrentOrder, this.XamlRoot, "xps");
                Debug.WriteLine($"[ORDER_DETAILS] ✓ Export completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ORDER_DETAILS] ✗ Export failed: {ex.Message}");
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}