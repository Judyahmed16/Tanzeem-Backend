using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Constants;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Presentation.Authentication {

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase {


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
