using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Enums
{
    public enum OrderFilter
    {
        PendingOrders,
        DeliveredOrders

    }
    public enum OrderSort
    {
        NearRecieveDate,
        FarRecieveDate,
        NearOrderDate,
        FarOrderDate,
        AZSupplierName,
        ZASupplierName,
        HighTotal,
        LowTotal

    }
}
