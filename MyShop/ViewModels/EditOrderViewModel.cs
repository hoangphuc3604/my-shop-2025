using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public class EditOrderViewModel : INotifyPropertyChanged
    {
        private readonly IOrderService _orderService;
        private readonly IPromotionService _promotionService;
        private readonly ISessionService _sessionService;

        private System.Collections.ObjectModel.ObservableCollection<Promotion> _availablePromotions = new();
        private Promotion? _selectedPromotion;
        private readonly DispatcherQueue? _uiDispatcher;

        private Order? _currentOrder;
        private string _selectedStatus = "Created";
        private ObservableCollection<ReadOnlyOrderItem> _orderItems = new();
        private bool _isLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        public EditOrderViewModel(IOrderService orderService, IPromotionService promotionService, ISessionService sessionService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _uiDispatcher = DispatcherQueue.GetForCurrentThread();
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

        public System.Collections.ObjectModel.ObservableCollection<Promotion> AvailablePromotions
        {
            get => _availablePromotions;
            set
            {
                if (_availablePromotions != value)
                {
                    _availablePromotions = value;
                    OnPropertyChanged();
                }
            }
        }

        public Promotion? SelectedPromotion
        {
            get => _selectedPromotion;
            set
            {
                try
                {
                    if (_selectedPromotion != value)
                    {
                        _selectedPromotion = value;
                        Debug.WriteLine($"[EDIT_ORDER_VM] SelectedPromotion set to: {_selectedPromotion?.Code ?? "null"}");
                        OnPropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EDIT_ORDER_VM] ✗ Error setting SelectedPromotion: {ex}");
                }
            }
        }

        public async Task LoadPromotionsAsync()
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                var promotions = await _promotionService.GetActivePromotionsAsync(token);
                Debug.WriteLine($"[EDIT_ORDER_VM] Loaded {promotions?.Count ?? 0} promotions from service");
                var ordered = promotions.OrderBy(p => p.Code).ToList();

                if (_uiDispatcher != null)
                {
                    _uiDispatcher.TryEnqueue(() =>
                    {
                        AvailablePromotions.Clear();
                        foreach (var p in ordered)
                        {
                            AvailablePromotions.Add(p);
                        }
                    });
                }
                else
                {
                    AvailablePromotions.Clear();
                    foreach (var p in ordered)
                    {
                        AvailablePromotions.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_VM] ✗ Error loading promotions: {ex}");
                AvailablePromotions.Clear();
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

                var items = fullOrder.OrderItems?.Select(oi => new ReadOnlyOrderItem(oi)).ToList() ?? new List<ReadOnlyOrderItem>();

                await LoadPromotionsAsync();

                if (_uiDispatcher != null)
                {
                    _uiDispatcher.TryEnqueue(() =>
                    {
                        try
                        {
                            CurrentOrder = fullOrder;
                            SelectedStatus = fullOrder.Status;
                            OrderItems.Clear();
                            foreach (var it in items)
                                OrderItems.Add(it);

                            if (!string.IsNullOrEmpty(fullOrder.AppliedPromotionCode))
                                SelectedPromotion = AvailablePromotions.FirstOrDefault(p => p.Code == fullOrder.AppliedPromotionCode);
                            else
                                SelectedPromotion = null;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[EDIT_ORDER_VM] ✗ UI update failed: {ex}");
                        }
                    });
                }
                else
                {
                    CurrentOrder = fullOrder;
                    SelectedStatus = fullOrder.Status;
                    OrderItems.Clear();
                    foreach (var it in items)
                        OrderItems.Add(it);

                    if (!string.IsNullOrEmpty(fullOrder.AppliedPromotionCode))
                        SelectedPromotion = AvailablePromotions.FirstOrDefault(p => p.Code == fullOrder.AppliedPromotionCode);
                    else
                        SelectedPromotion = null;
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
                    OrderItems = null,
                    PromotionCode = SelectedPromotion?.Code
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

        public void SetSelectedPromotionSilently(Promotion? promotion)
        {
            _selectedPromotion = promotion;
            Debug.WriteLine("[EDIT_ORDER_VM] (silent) SelectedPromotion set to: " + (_selectedPromotion?.Code ?? "null"));
        }

        public void NotifySelectedPromotion()
        {
            try
            {
                OnPropertyChanged(nameof(SelectedPromotion));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EDIT_ORDER_VM] ✗ NotifySelectedPromotion failed: {ex}");
            }
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