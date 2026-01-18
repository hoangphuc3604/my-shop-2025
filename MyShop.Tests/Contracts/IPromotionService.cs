using MyShop.Data.Models;

namespace MyShop.Tests.Contracts
{
    public interface IPromotionService
    {
        Task<List<Promotion>> GetActivePromotionsAsync(string? token);
    }
}