using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Dashboard
{
    public class TopMovingItemsDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public string Trend { get; set; } //enum
        
    }
}
