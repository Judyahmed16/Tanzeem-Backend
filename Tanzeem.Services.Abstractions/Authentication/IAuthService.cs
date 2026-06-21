using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Abstractions.Authentication {
    public interface IAuthService {
        Task<int> CreateAdminAsync(AdminSignUpDto userDto);
        Task<string?> Login(UserLoginDto userLoginDto);

        public Task<bool> ChangePasswordAsync(ChangePasswordDto dto);
        public Task<bool> RequestResetPasswordAsync(RequestResetPasswordDto dto);
        public  Task<bool> VerifyOtpAsync(VerifyOtpDto dto);
        public Task<bool> ConfirmResetPasswordAsync(ConfirmResetPasswordDto dto);
        public Task<bool> DeleteAccountFullyAsync();
    }
}

#region For later
//Task ResetPasswordAsync(ResetPasswordDto dto);
#endregion
