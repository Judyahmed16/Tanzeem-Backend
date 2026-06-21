using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;

namespace Tanzeem.Domain.Entities.Settings
{
    public class AIConfigurations : IAuditable
    {
        public int Id { get; set; }
        public bool DemandForecasting { get; set; }
        public bool AutoCategorization { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; }
    }
}
