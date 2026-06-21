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
    public class AIConfigurationsController (IAIConfigService _aIConfigService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetIAIConfigService()
        {
            var result = await _aIConfigService.GetIConfigurationsAsync();
            return Ok(result);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateAiConfig(AIConfigurationsDto aIConfigurationsDto)
        {
            var result = await _aIConfigService.UpdateIConfigurationsAsync(aIConfigurationsDto);
            return Ok(result);
        }
    }
}
