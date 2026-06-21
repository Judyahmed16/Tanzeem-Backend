using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderCountsDto
    {
        public int PendingOrdersCount { get; set; }
        public int DeliveredOrdersCount { get; set; }
        public decimal TotalOrdersRevenue { get; set; }
    }
}
