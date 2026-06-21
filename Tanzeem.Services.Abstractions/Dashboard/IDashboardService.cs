using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Dashboard;

namespace Tanzeem.Services.Abstractions.Dashboard
{
    public interface IDashboardService
    {
        public Task<DashboardBoxesDto> GetDashboardSummary();
        public Task<List<TopMovingItemsDto>> GetTopMovingItemsAsync();
        public Task<List<CategoryDistributionDto>> GetCategoryDistribution();

        public Task<List<MonthlyMovementDto>> GetMonthlyStockMovementAsync();

        public Task<List<StockValueDto>> GetStockValueTrendAsync();
    }
}
