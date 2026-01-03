using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderDetailsPage : Page
    {
        private readonly IOrderService _orderService;
        private readonly ISessionService _sessionService;
        private Order? _currentOrder;
        private ObservableCollection<OrderItem> _orderItems;

        public Order? CurrentOrder
        {
            get => _currentOrder;
            set => _currentOrder = value;
        }

        public ObservableCollection<OrderItem> OrderItems
        {
            get => _orderItems;
            set => _orderItems = value;
        }

        public OrderDetailsPage()
        {
            this.InitializeComponent();
            _orderService = (App.Services.GetService(typeof(IOrderService)) as IOrderService)!;
            _sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _orderItems = new ObservableCollection<OrderItem>();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Order order)
            {
                await LoadOrderDetailsAsync(order);
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
    }
}