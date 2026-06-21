using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Dashboard
{
    public class CategoryDistributionDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
