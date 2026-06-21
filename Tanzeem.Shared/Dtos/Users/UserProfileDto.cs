using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Users {
    public class UserProfileDto {

        public int Id { get; set; }
        public string UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public UserStatus Status { get; set; }

        public UserRoles Role { get; set; }

    }
}
