using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Services.Abstractions.Dashboard;

namespace Tanzeem.Presentation.Dashboard
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class DashboardController(IDashboardService _dashboardService) :ControllerBase
    {
        [HttpGet("get_four_boxes")]
        public async Task<IActionResult> GetFourBoxesAtTheTopOfPage()
        {
            var result = await _dashboardService.GetDashboardSummary();
            return Ok(result);
        }

        [HttpGet("get_top_moving_items")]
        public async Task<IActionResult> GetTopMovingItems()
        {
            var result = await _dashboardService.GetTopMovingItemsAsync();
            return Ok(result);
        }

        [HttpGet("get_category_distribution")]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            var result = await _dashboardService.GetCategoryDistribution();
            return Ok(result);
        }

        [HttpGet("get_bar_chart_IN-OUT")]
        public async Task<IActionResult> GetBarChartInOutStock()
        {
            var result = await _dashboardService.GetMonthlyStockMovementAsync();
            return Ok(result);
        }
        [HttpGet("get_line_chart_stock_value")]
        public async Task<IActionResult> GetLineChartStockValue()
        {
            var result = await _dashboardService.GetStockValueTrendAsync();
            return Ok(result);
        }
    }
}
