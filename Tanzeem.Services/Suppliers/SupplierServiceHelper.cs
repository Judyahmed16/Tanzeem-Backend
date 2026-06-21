using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Services.Suppliers
{
    public static class SupplierServiceHelper
    {
    
        public static double GetLeadTime(IEnumerable<Order> orders)
        {
            var leadTime = orders.Any(e => e.RecievedDeliveryDate.HasValue) ?
                orders.Where(e => e.RecievedDeliveryDate.HasValue)
                .Average(o => (o.RecievedDeliveryDate!.Value - o.OrderDate).TotalDays) : 0;
            return leadTime;
        }

        public static decimal GetOnTimePercentage(IEnumerable<Order> orders)
        {
            var onTime = orders.Any(o => o.RecievedDeliveryDate.HasValue) ?
            (decimal)orders.Count(o => o.RecievedDeliveryDate <= o.ExpectedDeliveryDate) /
              orders.Count(o => o.RecievedDeliveryDate.HasValue) * 100
            : 0;
            return onTime;
        }

        //public static SupplierStatus GetSupplierStatus(IEnumerable<Order> orders)
        //{
        //    var status = orders.Any(o => o.RecievedDeliveryDate.HasValue) ?
        //        (DateTime.Now - orders.Where(o => o.RecievedDeliveryDate.HasValue).Max(o => o.RecievedDeliveryDate.Value)).TotalDays > 360
        //        ? SupplierStatus.InActive
        //        : SupplierStatus.Active
        //        : SupplierStatus.InActive;

        //    return status;
        //}

        public static string GetBadge(IEnumerable<Order> orders)
        {
            decimal onTimePercent = GetOnTimePercentage(orders);
            double leadTime = GetLeadTime(orders);
            if (onTimePercent >= 95 && leadTime < 3)
            {
                return "TopPerformer";
            }
            else if (onTimePercent >= 85 && leadTime < 6)
            {
                return "Reliable";
            }
            else if (onTimePercent < 50)
            {
                return "At Risk";
            }
            else
            {
                return "Standard";
            }
        }

    }
}


//LeadTime = s.Orders.Any(o => o.RecievedDeliveryDate.HasValue) ?
//s.Orders
//.Where(o => o.RecievedDeliveryDate.HasValue)
//.Average(o => (o.RecievedDeliveryDate!.Value - o.OrderDate).TotalDays)
//: 0,


//onTimePercentage = s.Orders.Any(o => o.RecievedDeliveryDate.HasValue) ?
//        (decimal) s.Orders.Count(o => o.RecievedDeliveryDate <= o.ExpectedDeliveryDate) /
//        s.Orders.Count(o => o.RecievedDeliveryDate.HasValue) * 100
//        : 0,

//s.Orders.Any(o => o.RecievedDeliveryDate.HasValue) ?
//(DateTime.Now - s.Orders.Where(o => o.RecievedDeliveryDate.HasValue).Max(o => o.RecievedDeliveryDate.Value)).TotalDays > 360
//        ? SupplierStatus.InActive : SupplierStatus.Active
//        : SupplierStatus.InActive,