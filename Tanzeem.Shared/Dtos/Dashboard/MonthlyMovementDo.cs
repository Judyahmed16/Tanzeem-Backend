using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Dashboard
{
    public class MonthlyMovementDto
    {
        public string MonthName { get; set; } = string.Empty;
        public int StockIn { get; set; }                      
        public int StockOut { get; set; }                    
    }
}
