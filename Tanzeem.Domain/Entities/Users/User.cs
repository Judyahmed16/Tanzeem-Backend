using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Users { 

    public class User {
    
        public int Id { get; set; }
        
        public string UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public string PhoneNumber { get; set; }

        public UserStatus Status { get; set; }

        public UserRoles Role { get; set; }
        public string? ResetToken { get; set; }              // OTP: "123456"
        public DateTime? ResetTokenExpiry { get; set; }      // Expires after 10 minutes
        public int FailedResetAttempts { get; set; } = 0;    // Track failed attempts (max 3)
        #region Relationships

        #endregion
        public int? CompanyId { get; set; }

        #region Navigation
        #endregion
        public Company Company { get; set; } = default!;
        public ICollection<BranchUserRelationship> BURelations { get; set; } = default!;
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

        public ICollection<AuditTrial> auditTrials { get; set; } = new List<AuditTrial>();
    }
}
