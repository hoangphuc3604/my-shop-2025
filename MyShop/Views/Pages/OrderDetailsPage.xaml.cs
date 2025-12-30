using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Data;
using MyShop.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class OrderDetailsPage : Page
    {
        private MyShopDbContext _dbContext;
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
            _dbContext = (App.Services.GetService(typeof(MyShopDbContext)) as MyShopDbContext)!;
            _orderItems = new ObservableCollection<OrderItem>();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
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
                var fullOrder = await _dbContext.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (fullOrder == null)
                {
                    await ShowErrorAsync("Order not found.");
                    return;
                }

                CurrentOrder = fullOrder;
                OrderItems.Clear();
                foreach (var item in fullOrder.OrderItems)
                {
                    OrderItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load order details: {ex.Message}");
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