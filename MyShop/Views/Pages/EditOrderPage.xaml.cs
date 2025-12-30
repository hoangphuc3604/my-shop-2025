using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyShop.Data;
using MyShop.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MyShop.Views.Pages
{
    public sealed partial class EditOrderPage : Page, INotifyPropertyChanged
    {
        private MyShopDbContext _dbContext;
        private Order? _currentOrder;
        private string _selectedStatus = "Created";
        private ObservableCollection<EditableOrderItem> _orderItems;

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

        public ObservableCollection<EditableOrderItem> OrderItems
        {
            get => _orderItems;
            set => SetProperty(ref _orderItems, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public EditOrderPage()
        {
            this.InitializeComponent();
            _dbContext = (App.Services.GetService(typeof(MyShopDbContext)) as MyShopDbContext)!;
            _orderItems = new ObservableCollection<EditableOrderItem>();
            this.DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
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
            try
            {
                var fullOrder = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

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
                    OrderItems.Add(new EditableOrderItem(item));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to load order: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (_currentOrder == null)
                return;

            try
            {
                var dbOrder = await _dbContext.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == _currentOrder.OrderId);

                if (dbOrder == null)
                {
                    await ShowErrorAsync("Order not found.");
                    return;
                }

                bool statusChangedToPaid = dbOrder.Status != "Paid" && SelectedStatus == "Paid";

                if (statusChangedToPaid)
                {
                    var (isAvailable, errorMessage) = await CheckProductAvailabilityAsync(dbOrder);
                    if (!isAvailable)
                    {
                        await ShowErrorAsync(errorMessage);
                        return;
                    }
                }

                dbOrder.Status = SelectedStatus;

                foreach (var editableItem in OrderItems)
                {
                    var orderItem = dbOrder.OrderItems.FirstOrDefault(oi => oi.OrderItemId == editableItem.OrderItem.OrderItemId);
                    if (orderItem != null)
                    {
                        orderItem.Quantity = editableItem.Quantity;
                        orderItem.TotalPrice = (int)(orderItem.UnitSalePrice * editableItem.Quantity);
                    }
                }

                dbOrder.FinalPrice = dbOrder.OrderItems.Sum(oi => oi.TotalPrice);

                if (statusChangedToPaid)
                {
                    foreach (var orderItem in dbOrder.OrderItems)
                    {
                        var product = await _dbContext.Products.FindAsync(orderItem.ProductId);
                        if (product != null)
                        {
                            product.Count -= orderItem.Quantity;
                            if (product.Count < 0)
                                product.Count = 0;
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();

                await ShowSuccessAsync($"Order #{_currentOrder.OrderId} updated successfully!");

                // Navigate back WITHOUT triggering reload (NavigationMode.Back will skip reload in OrderPage)
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Failed to update order: {ex.Message}");
            }
        }

        private async Task<(bool IsAvailable, string ErrorMessage)> CheckProductAvailabilityAsync(Order order)
        {
            var insufficientProducts = new System.Collections.Generic.List<string>();

            foreach (var editableItem in OrderItems)
            {
                var product = await _dbContext.Products.FindAsync(editableItem.OrderItem.ProductId);
                if (product == null)
                {
                    insufficientProducts.Add($"• {editableItem.OrderItem.ProductId} - Product not found");
                    continue;
                }

                if (product.Count < editableItem.Quantity)
                {
                    insufficientProducts.Add($"• {product.Name} (SKU: {product.Sku})\n  Required: {editableItem.Quantity} units, Available: {product.Count} units");
                }
            }

            if (insufficientProducts.Count > 0)
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine("Cannot mark order as Paid. Insufficient inventory for the following products:\n");
                foreach (var product in insufficientProducts)
                {
                    errorMessage.AppendLine(product);
                }

                return (false, errorMessage.ToString());
            }

            return (true, string.Empty);
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            // Just navigate back - OrderPage won't reload due to NavigationMode.Back check
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

        private async Task ShowSuccessAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class EditableOrderItem : INotifyPropertyChanged
    {
        private int _quantity;

        public OrderItem OrderItem { get; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPrice)));
                }
            }
        }

        public int TotalPrice => (int)(OrderItem.UnitSalePrice * Quantity);

        public event PropertyChangedEventHandler? PropertyChanged;

        public EditableOrderItem(OrderItem orderItem)
        {
            OrderItem = orderItem;
            _quantity = orderItem.Quantity;
        }
    }
}