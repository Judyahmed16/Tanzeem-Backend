using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Subscriptions {
    public class Subscription {
    
        public int Id { get; set; }

        public string StripeSubscriptionId { get; set; } = default!;

        public PlanStatus Plan { get; set; }

        public SubscriptionStatus Status { get; set; } // Active_Expired

        public DateTime StartedAt { get; set; } = default!;

        public DateTime ExpiresAt { get; set; } = default!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = default!;

    }
}
