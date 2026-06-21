using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;

namespace Tanzeem.Domain.Entities.AuditLogs
{
    public class AuditTrial
    {
        public int Id { get; set; }
        public required string EntityName { get; set; }
        public required string Action { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EntityPrimaryKey { get; set; }

        public int? UserId { get; set; }
        public int? BranchId { get; set; }
        public virtual User User { get; set; }
        public virtual Branch Branch { get; set; }

    }
}
