using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.DeliveryIssues;

namespace Tanzeem.Presentation.DeliveryIssue
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class DeliveryIssuesController(IDeliveryIssuesService _deliveryIssuesService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> ViewDelivery([FromQuery(Name = "page_size")] int pageSize=10, [FromQuery(Name ="page")]int page =1 , [FromQuery(Name = "sortId")] DeliveryIssuesSort? deliveryIssuesSort = null , [FromQuery(Name = "searchTerm")] string? searchTerm = null)
        {
            var result = await _deliveryIssuesService.GetAllDeliveryIssues(page , pageSize , deliveryIssuesSort,searchTerm);
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> ViewDeliveryById(int id)
        {
            var result = await _deliveryIssuesService.GetDeliveryIssueById(id);
            return Ok(result);
        }
    }
}
