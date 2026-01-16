using System;

namespace MyShop.Data.Models
{
    public class Promotion
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PromotionType DiscountType { get; set; }
        public int DiscountValue { get; set; }
        public AppliesTo AppliesTo { get; set; }
        public int[]? AppliesToIds { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum PromotionType
    {
        PERCENTAGE,
        FIXED
    }

    public enum AppliesTo
    {
        ALL,
        PRODUCTS,
        CATEGORIES
    }
}
