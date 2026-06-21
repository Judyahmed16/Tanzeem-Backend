using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Enums {
    public enum UserRoles {
        Admin = 1,
        Manager = 2,
        Staff = 3,
    }

    public enum UserStatus {
        Active = 1,
        Inactive = 2,
        Suspended = 3,
    }

}
