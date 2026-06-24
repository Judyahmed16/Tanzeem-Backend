using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.DeliveryIssues;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Authentication {
    public class AuthService(IUnitOfWork unitOfWork,
        ICurrentService currentService,
        IOptions<JwtOptions> options,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor) : IAuthService {
        
        private const int OTP_EXPIRY_MINUTES = 10;
        private const int MAX_FAILED_ATTEMPTS = 3;


        public async Task<int> CreateAdminAsync(AdminSignUpDto userDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == userDto.Email);

            if (user is not null) {
                throw new BusinessRuleException("Email is already Registered!");
            }

            #region Mapping
            var admin = new User() {
                UserId = Guid.NewGuid().ToString("N")[..8],
                Name = userDto.Name,
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                Status = UserStatus.Active,
                Role = UserRoles.Admin,
            };

            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(admin, userDto.Password);
            admin.PasswordHash = hashedPassword;

            #endregion

            await unitOfWork.GetRepository<User>().AddAsync(admin);
            var count = await unitOfWork.SaveChangesAsync();

            return admin.Id;
        }

        public async Task<string?> Login(UserLoginDto userLoginDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == userLoginDto.Email);

            if (user is null) {
                throw new BusinessRuleException("User not found!");
            }


            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userLoginDto.Password)
                == PasswordVerificationResult.Failed) {
                throw new BusinessRuleException("Email or password is incorrect!");
            }

            var now = DateTime.UtcNow;
            var userAgent = TrimToLength(GetUserAgent(), 512);
            var ipAddress = TrimToLength(GetIpAddress(), 64);
            var jwtOptions = options.Value;

            var matchingSessions = await unitOfWork.GetRepository<UserSession>()
                .GetAllAsIQueryable()
                .Where(s =>
                    s.UserId == user.Id &&
                    s.RevokedAt == null &&
                    s.ExpiresAt > now &&
                    s.UserAgent == userAgent &&
                    s.IpAddress == ipAddress)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var session = matchingSessions.FirstOrDefault();

            if (session is null)
            {
                session = CreateSession(user, now, userAgent, ipAddress);
                await unitOfWork.GetRepository<UserSession>().AddAsync(session);
            }
            else
            {
                session.SessionKey = Guid.NewGuid().ToString("N");
                session.LastSeenAt = now;
                session.ExpiresAt = now.AddDays(jwtOptions.DurationInDays);
                unitOfWork.GetRepository<UserSession>().UpdateAsync(session);

                foreach (var duplicateSession in matchingSessions.Skip(1))
                {
                    RevokeSession(duplicateSession, "Replaced by newer login");
                    unitOfWork.GetRepository<UserSession>().UpdateAsync(duplicateSession);
                }
            }

            await unitOfWork.SaveChangesAsync();

            var token = await AuthHelper.GenerateToken(user, options, unitOfWork, session.SessionKey);

            return token;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto)
        {
            int userId = currentService.UserId ?? throw new UnauthorizedAccessException("no user id assigned");
            
            var user = await unitOfWork.GetRepository<User>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                throw new BusinessRuleException("User not found!");

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);

            if (result == PasswordVerificationResult.Failed)
                throw new BusinessRuleException("Old password is incorrect!");

            user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);

            unitOfWork.GetRepository<User>().UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IReadOnlyList<UserSessionDto>> GetSessionsAsync()
        {
            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated.");
            var currentSessionKey = currentService.SessionId;
            var now = DateTime.UtcNow;

            return await unitOfWork.GetRepository<UserSession>()
                .GetAllAsIQueryable()
                .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
                .OrderByDescending(s => s.LastSeenAt)
                .Select(s => new UserSessionDto
                {
                    Id = s.Id,
                    DeviceName = s.DeviceName,
                    IpAddress = s.IpAddress,
                    UserAgent = s.UserAgent,
                    CreatedAt = s.CreatedAt,
                    LastSeenAt = s.LastSeenAt,
                    ExpiresAt = s.ExpiresAt,
                    RevokedAt = s.RevokedAt,
                    IsCurrent = s.SessionKey == currentSessionKey,
                    IsActive = s.RevokedAt == null && s.ExpiresAt > now
                })
                .ToListAsync();
        }

        public async Task<bool> RevokeSessionAsync(int sessionId)
        {
            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated.");

            var session = await unitOfWork.GetRepository<UserSession>()
                .GetAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session is null)
                throw new BusinessRuleException("Session not found.");

            RevokeSession(session, "Revoked by user");
            unitOfWork.GetRepository<UserSession>().UpdateAsync(session);
            await unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RevokeCurrentSessionAsync()
        {
            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated.");
            var sessionKey = currentService.SessionId
                ?? throw new BusinessRuleException("Current token is not linked to a login session.");

            var session = await unitOfWork.GetRepository<UserSession>()
                .GetAsync(s => s.SessionKey == sessionKey && s.UserId == userId);

            if (session is null)
                throw new BusinessRuleException("Current session not found.");

            RevokeSession(session, "Signed out");
            unitOfWork.GetRepository<UserSession>().UpdateAsync(session);
            await unitOfWork.SaveChangesAsync();

            return true;
        }

        #region Forget Password - Step 1: Request Reset

        /// <summary>
        /// Step 1: User requests password reset by providing email
        /// System generates OTP and sends it via email
        /// </summary>
        public async Task<bool> RequestResetPasswordAsync(RequestResetPasswordDto dto)
        {
            try
            {
                // Get user by email
                var user = await unitOfWork.GetRepository<User>()
                    .GetAsync(u => u.Email == dto.Email);

                if (user is null)
                    throw new BusinessRuleException("Email not found in system!");

                // Generate OTP (6 random digits)
                var otp = AuthHelper.GenerateOtp();

                // Send OTP before persisting it so a failed email does not leave a usable reset token behind.
                await SendOtpEmailAsync(user.Email, user.Name, otp);

                // Store OTP in database with expiry time
                user.ResetToken = otp;
                user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);
                user.FailedResetAttempts = 0; // Reset counter for fresh attempt

                unitOfWork.GetRepository<User>().UpdateAsync(user);
                await unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException($"Failed to request password reset: {ex.Message}");
            }
        }

        #endregion

        #region Forget Password - Step 2: Verify OTP

        /// <summary>
        /// Step 2: User enters email and OTP to verify identity
        /// Does NOT change password yet - just validates OTP
        /// </summary>
        public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto)
        {
            try
            {
                // Get user by email
                var user = await unitOfWork.GetRepository<User>()
                    .GetAsync(u => u.Email == dto.Email);

                if (user is null)
                    throw new BusinessRuleException("Email not found in system!");

                // Check if reset is locked (exceeded max failed attempts)
                if (user.FailedResetAttempts >= MAX_FAILED_ATTEMPTS)
                    throw new BusinessRuleException("Too many failed attempts. Please request a new OTP.");

                // Check if OTP has expired
                if (user.ResetTokenExpiry == null || DateTime.UtcNow > user.ResetTokenExpiry)
                    throw new BusinessRuleException("OTP has expired. Please request a new one.");

                // Verify the OTP code
                if (user.ResetToken != dto.Otp)
                {
                    user.FailedResetAttempts++;
                    unitOfWork.GetRepository<User>().UpdateAsync(user);
                    await unitOfWork.SaveChangesAsync();

                    int remainingAttempts = MAX_FAILED_ATTEMPTS - user.FailedResetAttempts;
                    throw new BusinessRuleException(
                        $"Invalid OTP. {remainingAttempts} attempt{(remainingAttempts != 1 ? "s" : "")} remaining.");
                }

                // OTP is valid! User can proceed to Step 3
                return true;
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException($"OTP verification failed: {ex.Message}");
            }
        }

        #endregion

        #region Forget Password - Step 3: Confirm Reset Password

        /// <summary>
        /// Step 3: User submits email + OTP + new password
        /// Password is changed after OTP verification
        /// No need for old password since user forgot it
        /// </summary>
        public async Task<bool> ConfirmResetPasswordAsync(ConfirmResetPasswordDto dto)
        {
            try
            {
                // Get user by email
                var user = await unitOfWork.GetRepository<User>()
                    .GetAsync(u => u.Email == dto.Email);

                if (user is null)
                    throw new BusinessRuleException("Email not found in system!");

                // Check if reset is locked
                if (user.FailedResetAttempts >= MAX_FAILED_ATTEMPTS)
                    throw new BusinessRuleException("Too many failed attempts. Please request a new OTP.");

                // Check if OTP has expired
                if (user.ResetTokenExpiry == null || DateTime.UtcNow > user.ResetTokenExpiry)
                    throw new BusinessRuleException("OTP has expired. Please request a new one.");

                // Verify OTP one more time before changing password
                if (user.ResetToken != dto.Otp)
                {
                    user.FailedResetAttempts++;
                    unitOfWork.GetRepository<User>().UpdateAsync(user);
                    await unitOfWork.SaveChangesAsync();

                    int remainingAttempts = MAX_FAILED_ATTEMPTS - user.FailedResetAttempts;
                    throw new BusinessRuleException(
                        $"Invalid OTP. {remainingAttempts} attempt{(remainingAttempts != 1 ? "s" : "")} remaining.");
                }

                // ✅ All validations passed! Now change the password

                // Hash the new password
                var hasher = new PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);

                // Clear reset token and reset counter
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                user.FailedResetAttempts = 0;

                // Update user in database
                unitOfWork.GetRepository<User>().UpdateAsync(user);
                await unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException($"Failed to reset password: {ex.Message}");
            }
        }

        #endregion

        #region Forget Password send otp email
        /// <summary>
        /// Sends OTP to user's email via SMTP
        /// Uses Gmail credentials from configuration/user secrets
        /// </summary>
        public async Task SendOtpEmailAsync(string email, string userName, string otp)
        {
            try
            {
                var smtpHost = configuration["SmtpSettings:Host"];
                var smtpPortValue = configuration["SmtpSettings:Port"] ?? "587";
                var smtpEmail = configuration["SmtpSettings:Email"];
                var smtpPassword = configuration["SmtpSettings:Password"];
                var displayName = configuration["SmtpSettings:DisplayName"];
                var missingSettings = new List<string>();

                if (string.IsNullOrWhiteSpace(smtpHost)) missingSettings.Add("SmtpSettings:Host");
                if (string.IsNullOrWhiteSpace(smtpEmail)) missingSettings.Add("SmtpSettings:Email");
                if (string.IsNullOrWhiteSpace(smtpPassword)) missingSettings.Add("SmtpSettings:Password");
                if (string.IsNullOrWhiteSpace(displayName)) missingSettings.Add("SmtpSettings:DisplayName");

                if (missingSettings.Count > 0) {
                    throw new BusinessRuleException(
                        $"Password reset email is not configured. Missing: {string.Join(", ", missingSettings)}.");
                }

                if (!int.TryParse(smtpPortValue, out var smtpPort)) {
                    throw new BusinessRuleException("Password reset email is not configured. SmtpSettings:Port must be a number.");
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    // Enable SSL for secure connection
                    client.EnableSsl = true;

                    // Use credentials from configuration
                    client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpEmail!, displayName),
                        Subject = "🔐 Reset Your Password",
                        Body = AuthHelper.GenerateEmailBody(userName, otp),
                        IsBodyHtml = true  // HTML email
                    };

                    mailMessage.To.Add(email);

                    // Send email asynchronously
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (SmtpException ex)
            {
                throw new BusinessRuleException($"Failed to send email: {ex.Message}");
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException($"Email service error: {ex.Message}");
            }
        }
        #endregion

        #region delete admin account
        public async Task<bool> DeleteAccountFullyAsync()
        {
            int CompanyId = currentService.CompanyId ?? throw new UnauthorizedAccessException();

            var companyRepo = unitOfWork.GetRepository<Company>();
            var company = await companyRepo.GetAsync(c => c.Id == CompanyId);

            if (company is null)
                throw new BusinessRuleException("Company not found in the system!");

            using var transaction = await unitOfWork.BeginTransactionAsync();

            try
            {
                var branchIds = await unitOfWork.GetRepository<Branch>().GetAllAsIQueryable()
                    .Where(b => b.CompanyId == CompanyId)
                    .Select(b => b.Id)
                    .ToListAsync();

                var userIds = await unitOfWork.GetRepository<User>().GetAllAsIQueryable()
                    .Where(u => u.CompanyId == CompanyId)
                    .Select(u => u.Id)
                    .ToListAsync();

                var auditTrials = await unitOfWork.GetRepository<AuditTrial>().GetAllAsIQueryable()
                    .IgnoreQueryFilters()
                    .Where(a =>
                        (a.BranchId.HasValue && branchIds.Contains(a.BranchId.Value)) ||
                        (a.UserId.HasValue && userIds.Contains(a.UserId.Value)))
                    .ToListAsync();

                if (auditTrials.Any()) unitOfWork.GetRepository<AuditTrial>().DeleteRangeAsync(auditTrials);

                if (branchIds.Any())
                {
                    var issues = await unitOfWork.GetRepository<DeliveryIssue>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (issues.Any()) unitOfWork.GetRepository<DeliveryIssue>().DeleteRangeAsync(issues);

                    var transactions = await unitOfWork.GetRepository<Transaction>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (transactions.Any()) unitOfWork.GetRepository<Transaction>().DeleteRangeAsync(transactions);

                    var demandForecasts = await unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (demandForecasts.Any()) unitOfWork.GetRepository<DemandForecast>().DeleteRangeAsync(demandForecasts);

                    var inventories = await unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (inventories.Any()) unitOfWork.GetRepository<Inventory>().DeleteRangeAsync(inventories);

                    var inventoryBatches = await unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (inventoryBatches.Any()) unitOfWork.GetRepository<InventoryBatch>().DeleteRangeAsync(inventoryBatches);

                    var notifications = await unitOfWork.GetRepository<Notification>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (notifications.Any()) unitOfWork.GetRepository<Notification>().DeleteRangeAsync(notifications);

                    var orders = await unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (orders.Any()) unitOfWork.GetRepository<Order>().DeleteRangeAsync(orders);

                    var alertConfigs = await unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (alertConfigs.Any()) unitOfWork.GetRepository<AlertConfigurations>().DeleteRangeAsync(alertConfigs);

                    var aiConfigs = await unitOfWork.GetRepository<AIConfigurations>().GetAllAsIQueryable()
                        .IgnoreQueryFilters()
                        .Where(x => branchIds.Contains(x.BranchId)).ToListAsync();
                    if (aiConfigs.Any()) unitOfWork.GetRepository<AIConfigurations>().DeleteRangeAsync(aiConfigs);
                }

                var products = await unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                    .IgnoreQueryFilters()
                    .Where(x => x.CompanyId == CompanyId).ToListAsync();
                if (products.Any()) unitOfWork.GetRepository<Product>().DeleteRangeAsync(products);

                var categories = await unitOfWork.GetRepository<Category>().GetAllAsIQueryable()
                    .Where(x => x.CompanyId == CompanyId).ToListAsync();
                if (categories.Any()) unitOfWork.GetRepository<Category>().DeleteRangeAsync(categories);

                var suppliers = await unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                    .Where(x => x.CompanyId == CompanyId).ToListAsync();
                if (suppliers.Any()) unitOfWork.GetRepository<Supplier>().DeleteRangeAsync(suppliers);

                var branches = await unitOfWork.GetRepository<Branch>().GetAllAsIQueryable()
                    .Where(x => x.CompanyId == CompanyId).ToListAsync();
                if (branches.Any())
                    unitOfWork.GetRepository<Branch>().DeleteRangeAsync(branches);

                companyRepo.DeleteAsync(company);

                await unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new BusinessRuleException($"Failed to delete account completely: {ex.Message}");
            }
        }
        #endregion

        private UserSession CreateSession(User user, DateTime now, string? userAgent, string? ipAddress)
        {
            var jwtOptions = options.Value;

            return new UserSession
            {
                SessionKey = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                DeviceName = DetectDeviceName(userAgent),
                UserAgent = userAgent,
                IpAddress = ipAddress,
                CreatedAt = now,
                LastSeenAt = now,
                ExpiresAt = now.AddDays(jwtOptions.DurationInDays)
            };
        }

        private static void RevokeSession(UserSession session, string reason)
        {
            if (session.RevokedAt is not null)
                return;

            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
        }

        private string? GetUserAgent()
        {
            return httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        }

        private string? GetIpAddress()
        {
            var request = httpContextAccessor.HttpContext?.Request;
            var forwardedFor = request?.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIp = request?.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp.Trim();

            return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        private static string DetectDeviceName(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "Unknown device";

            var lower = userAgent.ToLowerInvariant();

            var browser = lower switch
            {
                var value when value.Contains("edg/") => "Edge",
                var value when value.Contains("chrome/") || value.Contains("crios/") => "Chrome",
                var value when value.Contains("firefox/") || value.Contains("fxios/") => "Firefox",
                var value when value.Contains("safari/") => "Safari",
                _ => "Browser"
            };

            var platform = lower switch
            {
                var value when value.Contains("iphone") => "iPhone",
                var value when value.Contains("ipad") => "iPad",
                var value when value.Contains("android") => "Android",
                var value when value.Contains("mac os") || value.Contains("macintosh") => "Mac",
                var value when value.Contains("windows") => "Windows",
                _ => "device"
            };

            return $"{browser} on {platform}";
        }

        private static string? TrimToLength(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }

}
