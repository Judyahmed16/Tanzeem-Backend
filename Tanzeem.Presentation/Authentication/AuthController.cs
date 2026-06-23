using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Presentation.Authentication {

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService, IHttpClientFactory httpClientFactory) : ControllerBase {


        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto) {
            var token = await authService.Login(userLoginDto);
            return Ok(token);
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto dto)
        {
            var result = await authService.ChangePasswordAsync(dto);
            return Ok(new { success = result });
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetSessions()
        {
            var sessions = await authService.GetSessionsAsync();
            return Ok(sessions);
        }

        [HttpDelete("sessions/{sessionId:int}")]
        [Authorize]
        public async Task<IActionResult> RevokeSession(int sessionId)
        {
            var result = await authService.RevokeSessionAsync(sessionId);
            return Ok(new { success = result });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var result = await authService.RevokeCurrentSessionAsync();
            return Ok(new { success = result });
        }

        [HttpGet("session-location")]
        [Authorize]
        public async Task<IActionResult> GetSessionLocation([FromQuery] string ip)
        {
            if (!IsPublicIpAddress(ip))
                return Ok(new { label = "Unknown location" });

            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                var response = await client.GetAsync($"https://ipwho.is/{Uri.EscapeDataString(ip)}");
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);
                var root = document.RootElement;

                if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    var label = string.Join(", ", new[]
                    {
                        GetJsonString(root, "city"),
                        GetJsonString(root, "region"),
                        GetJsonString(root, "country")
                    }.Where(value => !string.IsNullOrWhiteSpace(value)));

                    if (!string.IsNullOrWhiteSpace(label))
                        return Ok(new { label });
                }
            }
            catch
            {
                // Keep the sessions page usable even when the public lookup service is down.
            }

            return Ok(new { label = "Location unavailable" });
        }

        [HttpPost("forget-password/request")]
        public async Task<IActionResult> RequestResetPassword(
           [FromBody] RequestResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                await authService.RequestResetPasswordAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "OTP has been sent to your email. Please check your inbox (and spam folder).",
                    data = new { email = dto.Email, expiresIn = "10 minutes" }
                });
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again later."
                });
            }
        }

        private static string? GetJsonString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static bool IsPublicIpAddress(string? value)
        {
            if (!IPAddress.TryParse(value, out var ipAddress))
                return false;

            if (IPAddress.IsLoopback(ipAddress))
                return false;

            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ipAddress.GetAddressBytes();
                if (bytes[0] == 10 || bytes[0] == 127 || bytes[0] == 169 && bytes[1] == 254)
                    return false;
                if (bytes[0] == 192 && bytes[1] == 168)
                    return false;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return false;
            }

            return true;
        }

        [HttpPost("forget-password/verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                await authService.VerifyOtpAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "OTP verified successfully! You can now reset your password.",
                    data = new { email = dto.Email }
                });
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again later."
                });
            }
        }

        [HttpPost("forget-password/confirm")]
        public async Task<IActionResult> ConfirmResetPassword(
            [FromBody] ConfirmResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                await authService.ConfirmResetPasswordAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Password reset successfully! You can now login with your new password.",
                    data = new { email = dto.Email }
                });
            }
            catch (BusinessRuleException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again later."
                });
            }
        }

        [HttpDelete("Delete-Account-Fully")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteCompanyFully()
        {
            var result = await authService.DeleteAccountFullyAsync();
            return Ok(new { success = result });
        }

    }
}
