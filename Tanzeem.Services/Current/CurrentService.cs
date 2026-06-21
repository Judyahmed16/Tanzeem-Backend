using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Current {
    public class CurrentService(IHttpContextAccessor httpContextAccessor) : ICurrentService {
        public int? UserId => TryGetInt(ClaimTypes.NameIdentifier);
        public int? CompanyId => TryGetInt("CompanyId");
        public int? BranchId => TryGetInt("BranchId");
        public string? Role => httpContextAccessor.HttpContext?.User
                                     ?.FindFirst(ClaimTypes.Role)?.Value;

        private int? TryGetInt(string claimType) {
            var val = httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
            return int.TryParse(val, out var result) ? result : null;
        }
    }

}
