using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Notifications
{
    public class AlertCountsDto
    {
        public int DeadCount { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
    }
}
