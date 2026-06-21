using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Services.Abstractions.Settings;
using Tanzeem.Shared.Dtos.Settings;

namespace Tanzeem.Presentation.Settings
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class AlertConfigurationsController(IAlertConfigurationsService _alertConfigurationsService) : ControllerBase
    {
        //[HttpPost]
        //public async Task<IActionResult> createDefault()
        //{
        //    var result = await _alertConfigurationsService.CreateDefaultAlertsConfigurationsAsync(1);
        //    return Ok(result);
        //}

        [HttpGet]
        public async Task<IActionResult> ViewSettings()
        {
            var result = await _alertConfigurationsService.GetAlertConfigurations();
            return Ok(result);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateSettings(AlertConfigurationsDto alertConfigurationsDto)
        {
            var result = await _alertConfigurationsService.UpdateAlertConfigurations(alertConfigurationsDto);
            return Ok(result);
        }
    }
}
