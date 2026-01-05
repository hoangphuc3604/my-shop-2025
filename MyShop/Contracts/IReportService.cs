using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Contracts
{
    public interface IReportService
    {
        Task<RevenueReport> GenerateRevenueReportAsync(DateTime? fromDate, DateTime? toDate, string? token);
        Task<List<ProductRevenue>> GetProductRevenueAsync(DateTime? fromDate, DateTime? toDate, string? token);
    }
}