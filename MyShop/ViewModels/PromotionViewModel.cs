using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.ViewModels
{
    public partial class PromotionViewModel : ObservableObject
    {
        private readonly IPromotionService _promotionService;
        private readonly ISessionService _sessionService;
        private readonly IAuthorizationService _authorizationService;

        // Collections
        [ObservableProperty]
        private ObservableCollection<Promotion> _promotions = new();

        // Selected item
        [ObservableProperty]
        private Promotion? _selectedPromotion;

        // Search
        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        // Date Filter
        [ObservableProperty]
        private DateTimeOffset? _startDate;

        [ObservableProperty]
        private DateTimeOffset? _endDate;

        // Pagination
        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalCount = 0;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // Permissions
        [ObservableProperty]
        private bool _canCreatePromotions;

        [ObservableProperty]
        private bool _canUpdatePromotions;

        [ObservableProperty]
        private bool _canDeletePromotions;

        // Commands
        public IAsyncRelayCommand LoadPromotionsCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        public PromotionViewModel(
            IPromotionService promotionService,
            ISessionService sessionService,
            IAuthorizationService authorizationService)
        {
            _promotionService = promotionService;
            _sessionService = sessionService;
            _authorizationService = authorizationService;

            LoadPromotionsCommand = new AsyncRelayCommand(LoadPromotionsAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);

            InitializePermissions();
        }

        private void InitializePermissions()
        {
            CanCreatePromotions = _authorizationService.HasPermission("CREATE_PROMOTIONS");
            CanUpdatePromotions = _authorizationService.HasPermission("UPDATE_PROMOTIONS");
            CanDeletePromotions = _authorizationService.HasPermission("DELETE_PROMOTIONS");
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading promotions...";

                await LoadPromotionsAsync();

                StatusMessage = $"Loaded {TotalCount} promotions";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_VM] Error initializing: {ex.Message}");
                StatusMessage = "Failed to load promotions. Please try again.";
                Promotions.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPromotionsAsync()
        {
            try
            {
                var token = _sessionService.GetAuthToken();
                var promotions = await _promotionService.GetPromotionsAsync(
                    CurrentPage,
                    PageSize,
                    SearchKeyword,
                    null, // isActive filter
                    token);

                Promotions.Clear();
                foreach (var promotion in promotions)
                {
                    Promotions.Add(promotion);
                }

                // Update total count for pagination info
                TotalCount = await _promotionService.GetTotalPromotionCountAsync(SearchKeyword, null, token);
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                Debug.WriteLine($"[PROMOTION_VM] Loaded {promotions.Count} promotions, total: {TotalCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_VM] Error loading promotions: {ex.Message}");
                StatusMessage = "Failed to load promotions. Please refresh.";
                Promotions.Clear();
                TotalCount = 0;
                TotalPages = 1;
                return;
            }
        }

        private async Task SearchAsync()
        {
            CurrentPage = 1; // Reset to first page
            await LoadPromotionsAsync();
        }

        private async Task ClearFiltersAsync()
        {
            SearchKeyword = string.Empty;
            StartDate = null;
            EndDate = null;
            CurrentPage = 1;
            await LoadPromotionsAsync();
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadPromotionsAsync();
            }
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadPromotionsAsync();
            }
        }

        private async Task RefreshAsync()
        {
            await LoadPromotionsAsync();
        }

        public async Task<bool> DeletePromotionAsync(int promotionId, string? token)
        {
            try
            {
                return await _promotionService.DeletePromotionAsync(promotionId, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_VM] Error deleting promotion: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreatePromotionAsync(
            string code,
            string description,
            PromotionType discountType,
            int discountValue,
            AppliesTo appliesTo,
            int[]? appliesToIds,
            DateTime? startAt,
            DateTime? endAt,
            string? token)
        {
            try
            {
                var created = await _promotionService.CreatePromotionAsync(
                    code, description, discountType, discountValue, appliesTo,
                    appliesToIds, startAt, endAt, token);

                if (created != null)
                {
                    await LoadPromotionsAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_VM] Error creating promotion: {ex.Message}");
                return false;
            }
        }

        public async Task<Promotion?> UpdatePromotionAsync(
            int promotionId,
            string? code,
            string? description,
            PromotionType? discountType,
            int? discountValue,
            AppliesTo? appliesTo,
            int[]? appliesToIds,
            DateTime? startAt,
            DateTime? endAt,
            bool? isActive,
            string? token)
        {
            try
            {
                var updated = await _promotionService.UpdatePromotionAsync(
                    promotionId, code, description, discountType, discountValue,
                    appliesTo, appliesToIds, startAt, endAt, isActive,
                    token);

                if (updated != null)
                    await LoadPromotionsAsync();

                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_VM] Error updating promotion: {ex.Message}");
                return null;
            }
        }

        // Properties for pagination
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
