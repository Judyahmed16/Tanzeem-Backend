using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.AuditLogs;

namespace Tanzeem.Presentation.AuditLogs
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class AuditLogsController(IAuditLogsService auditLogsService):ControllerBase
    {
        [HttpGet("Get-Audits-Branch")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAudits([FromQuery(Name = "Page_Size")] int pageSize = 15, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await auditLogsService.ViewAllAudits(page, pageSize);
            return Ok(result);
        }
        [HttpGet("Get-Audits-User")]
        [Authorize]
        public async Task<IActionResult> GetAuditsProfile([FromQuery(Name = "Page_Size")] int pageSize = 15, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await auditLogsService.ViewAuditsPerProfile(page, pageSize);
            return Ok(result);
        }
    }
}
