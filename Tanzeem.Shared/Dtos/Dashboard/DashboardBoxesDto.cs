using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Dashboard
{
    public class DashboardBoxesDto
    {
        public decimal TotalStockValue { get; set; }
        public int DeadStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
    }
}
