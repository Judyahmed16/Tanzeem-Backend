using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Services.Abstractions.AI;

namespace Tanzeem.Presentation.AI
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class DemandForecastingController (IDemandForecastingService _demandForecastingService): ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetPredictions([FromQuery(Name = "page_size")] int pageSize, [FromQuery(Name = "page")] int page = 1)
        {
            var result = await _demandForecastingService.GetAllPredictionsAsync(page, pageSize);
            return Ok(result);
        }
        [HttpGet("Get_Top_Categories_By_Forecast")]
        public async Task<IActionResult> GetTopCategoriesByForecast()
        {
            var result = await _demandForecastingService.GetTopCategoriesByForecast();
            return Ok(result);
        }
        [HttpGet("Get_mini_dashboard")]
        public async Task<IActionResult> GetMiniDashboard()
        {
            var result = await _demandForecastingService.GetCounts();
            return Ok(result);
        }

        [HttpPost("Refresh_Current_Branch")]
        public async Task<IActionResult> RefreshCurrentBranchForecast()
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (!int.TryParse(branchClaim, out var branchId) || branchId <= 0)
                return Unauthorized("User is not assigned to any branch.");

            await _demandForecastingService.UpdateForecastForBranchAsync(branchId);
            return Ok(new { message = "Demand forecast refresh completed.", branchId });
        }
    }
}
