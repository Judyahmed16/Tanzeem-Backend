using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Services.Abstractions.Current {
    public interface ICurrentService {

        int? UserId { get; }
        int? CompanyId { get; }
        int? BranchId { get; }
        string? Role { get; }

    }
}
