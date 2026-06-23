using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Shared;

namespace Tanzeem.Services.Authentication {
    public static class AuthHelper{

        public static async Task<string> GenerateToken(User user, IOptions<JwtOptions> options
            , IUnitOfWork unitOfWork, string? sessionId = null) {

            var jwtOptions = options.Value;
            var primaryBranch = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bu => bu.UserId == user.Id && bu.IsPrimary == true);


            var authClaims = new List<Claim>() {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("CompanyId", user.CompanyId?.ToString() ?? "0"),
                new Claim("BranchId", primaryBranch?.BranchId.ToString() ?? "0")
            };

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                authClaims.Add(new Claim("SessionId", sessionId));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);


            var tokenDescriptor = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                expires: DateTime.UtcNow.AddDays(jwtOptions.DurationInDays),
                claims: authClaims,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        }

        #region Private Helper Methods

        /// <summary>
        /// Generates a random 6-digit OTP
        /// Returns string like "123456"
        /// </summary>
        public static string GenerateOtp()
        {
            var random = new Random();
            int otp = random.Next(100000, 999999);
            return otp.ToString();
        }



        /// <summary>
        /// Generates beautiful HTML email body with OTP
        /// </summary>
        public static string GenerateEmailBody(string userName, string otp)
        {
            return $@"
<!DOCTYPE html>
<html dir='ltr' lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', 'Cantarell', sans-serif;
            background-color: #f5f5f5;
            padding: 20px 0;
        }}
        .container {{
            max-width: 500px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            font-size: 28px;
            font-weight: 600;
            margin: 0;
            letter-spacing: -0.5px;
        }}
        .content {{
            padding: 30px;
            color: #333333;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 20px;
            line-height: 1.5;
        }}
        .greeting strong {{
            color: #667eea;
        }}
        .otp-section {{
            background-color: #f9f9f9;
            border-left: 4px solid #667eea;
            padding: 20px;
            margin: 25px 0;
            border-radius: 4px;
            text-align: center;
        }}
        .otp-label {{
            font-size: 12px;
            color: #666666;
            text-transform: uppercase;
            font-weight: 600;
            letter-spacing: 1px;
            margin-bottom: 12px;
            display: block;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: 700;
            color: #667eea;
            letter-spacing: 8px;
            font-family: 'Courier New', 'Monaco', monospace;
            word-spacing: 10px;
        }}
        .expiry-warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
            font-size: 14px;
            color: #856404;
            line-height: 1.6;
        }}
        .expiry-warning strong {{
            color: #664d03;
        }}
        .security-note {{
            background-color: #e7f3ff;
            border-left: 4px solid #2196F3;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
            font-size: 14px;
            color: #0b5394;
            line-height: 1.6;
        }}
        .footer {{
            background-color: #f5f5f5;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #999999;
            border-top: 1px solid #e0e0e0;
        }}
        .footer a {{
            color: #667eea;
            text-decoration: none;
            margin: 0 5px;
        }}
        .footer a:hover {{
            text-decoration: underline;
        }}
        .divider {{
            height: 1px;
            background-color: #e0e0e0;
            margin: 20px 0;
        }}
        .text-muted {{
            color: #666666;
            font-size: 14px;
            line-height: 1.6;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <!-- Header -->
        <div class='header'>
            <h1>🔐 Password Reset</h1>
        </div>
        
        <!-- Content -->
        <div class='content'>
            <div class='greeting'>
                Hello <strong>{userName}</strong>,
            </div>
            
            <p class='text-muted'>
                We received a request to reset your password. Use the code below to complete the process:
            </p>
            
            <!-- OTP Section -->
            <div class='otp-section'>
                <span class='otp-label'>Your One-Time Code</span>
                <div class='otp-code'>{otp}</div>
            </div>
            
            <!-- Expiry Warning -->
            <div class='expiry-warning'>
                ⏰ <strong>Important:</strong> This code expires in 10 minutes. If you didn't request a password reset, please ignore this email.
            </div>
            
            <!-- Security Note -->
            <div class='security-note'>
                🔒 <strong>Security Tip:</strong> Never share this code with anyone. Our support team will never ask for it.
            </div>
            
            <div class='divider'></div>
            
            <p class='text-muted'>
                <strong>Need help?</strong> Contact our support team if you have any questions.
            </p>
        </div>
        
        <!-- Footer -->
        <div class='footer'>
            <p>© 2024 Your Company. All rights reserved.</p>
            <p style='margin-top: 10px;'>
                <a href='#'>Contact Support</a> | 
                <a href='#'>Privacy Policy</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }
}
