using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Settings
{
    public class AlertConfigurationsDto
    {
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
    }
}
