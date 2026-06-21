using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Users;

namespace Tanzeem.Shared.Dtos.AuditLogs
{
    public class AuditLogsDto
    {
        public int Id { get; set; }
        public required string EntityName { get; set; }
        public required string Action { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EntityPrimaryKey { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }


    }
}
