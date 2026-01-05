using System.Threading.Tasks;
using MyShop.Data.Models;

namespace MyShop.Contracts
{
    public interface IDashboardService
    {
        /// <summary>
        /// Loads dashboard statistics including products, orders, and revenue data.
        /// </summary>
        Task<DashboardStatsData> LoadDashboardStatsAsync(string? token);
    }
}