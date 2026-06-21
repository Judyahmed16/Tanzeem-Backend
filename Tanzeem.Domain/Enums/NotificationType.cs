using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Enums
{
    public enum NotificationType
    {
        LowStockAlert,
        DeadStockAlert,
        ExpiryAlert,
        OutOfStock,
        OrderUpdate,        
        SystemMessage
        
    }
}
