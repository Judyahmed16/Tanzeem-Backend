using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Dashboard
{
    public class StockValueDto
    {
        public string Month { get; set; } = string.Empty; 
        public decimal TotalValue { get; set; }  
    }
}
