using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Data.Models;

namespace MyShop.Contracts
{
    public interface IPromotionService
    {
        Task<List<Promotion>> GetPromotionsAsync(
            int page,
            int pageSize,
            string? search,
            bool? isActive,
            string? token);

        Task<Promotion?> GetPromotionByIdAsync(int promotionId, string? token);

        Task<int> GetTotalPromotionCountAsync(
            string? search,
            bool? isActive,
            string? token);

        Task<Promotion?> CreatePromotionAsync(
            string code,
            string description,
            PromotionType discountType,
            int discountValue,
            AppliesTo appliesTo,
            int[]? appliesToIds,
            DateTime? startAt,
            DateTime? endAt,
            string? token);

        Task<Promotion?> UpdatePromotionAsync(
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
            string? token);

        Task<bool> DeletePromotionAsync(int promotionId, string? token);

        Task<PromotionCalculationResult?> ApplyPromotionAsync(
            string code,
            int orderTotal,
            List<OrderItemInput> orderItems,
            string? token);

        Task<List<Promotion>> GetActivePromotionsAsync(string? token);
    }


    public class PromotionCalculationResult
    {
        public int OriginalPrice { get; set; }
        public int DiscountAmount { get; set; }
        public int FinalPrice { get; set; }
        public Promotion? AppliedPromotion { get; set; }
        public string? Message { get; set; }
    }
}
