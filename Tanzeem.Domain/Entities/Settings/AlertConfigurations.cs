using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;

namespace Tanzeem.Domain.Entities.Settings
{
    public class AlertConfigurations : IAuditable
    {
        public int Id { get; set; }
        
        public int LowStockThreshold { get; set; }
        public int DaysBeforeExpiry { get; set; }
        public int DaysWithoutMovement { get; set; }
        public bool IsActive_InAppNotifiation { get; set; }
        public bool IsActive_EmailNotifiation { get; set; }
        public bool IsActive_LowAlert { get; set; }
        public bool IsActive_OutAlert { get; set; }
        public bool IsActive_ExpiryAlert { get; set; }
        public bool IsActive_DeadAlert { get; set; }
        public bool IsActive_NewOrderAlert { get; set; }
        public bool IsActive_OrderUpdateAlert { get; set; }


        public int BranchId { get; set; }
        public Branch Branch { get; set; }
    }
}
