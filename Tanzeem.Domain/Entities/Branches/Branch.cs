using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.AuditLogs;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.DeliveryIssues;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Branches {
    
    public class Branch {
    
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string? Location { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public BranchStatus Status { get; set; } = BranchStatus.Active;


        #region Relationships
        #endregion
        public int CompanyId { get; set; }  // fk


        #region Navigation
        #endregion
        public Company Company { get; set; } = default!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<BranchUserRelationship> BURelations { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        

        public AlertConfigurations AlertConfigurations { get; set; }
        public AIConfigurations AIConfiguration { get; set; }
        public ICollection<DeliveryIssue> DeliveryIssues { get; set; } = new List<DeliveryIssue>();
        public ICollection<DemandForecast> DemandForecasts { get; set; } = new List<DemandForecast>();
        public ICollection<AuditTrial> auditTrials { get; set; } = new List<AuditTrial>();
    }
}


#region Later
//public ICollection<User> Users { get; set; } = new List<User>();
#endregion