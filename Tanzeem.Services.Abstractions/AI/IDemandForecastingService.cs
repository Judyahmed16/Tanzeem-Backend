using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.DemandForecast;

namespace Tanzeem.Services.Abstractions.AI
{
    public interface IDemandForecastingService
    {
        public Task UpdateAllForecastsAsync();
        public Task<PaginationResponseDto<AIDemandForecastResponseDto>> GetAllPredictionsAsync(int page, int pageSize);
        public Task<IEnumerable<TopCategoriesByForecastDto>> GetTopCategoriesByForecast();
        public Task<DemandDashboardDto> GetCounts();
        public Task UpdateForecastForBranchAsync(int branchId);
    }
}
