using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Services.Abstractions.Notifications;

namespace Tanzeem.Presentation.Notifications
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class NotificationController(INotificationService _notificationService) : ControllerBase
    {
        [HttpGet]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> GetNotifications([FromQuery(Name = "Page_Size")] int pageSize =20, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await _notificationService.GetAllNotifications(page,pageSize);
            return Ok(result);
        }

        [HttpPatch("mark-as-read/{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsReadAsync(id);
            if (!result) return NotFound("Notification not found.");

            return Ok(new { Message = "Notification marked as read." });
        }

        [HttpPatch("mark-all-read")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> MarkAllAsRead()
        { 
            await _notificationService.MarkAllAsReadAsync();

            return Ok(new { Message = "All notifications marked as read." });
        }

    }
}
