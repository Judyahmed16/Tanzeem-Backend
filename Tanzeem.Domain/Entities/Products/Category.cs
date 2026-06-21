using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Companies;

namespace Tanzeem.Domain.Entities.Products {
    public class Category : IAuditable{
    
        public int Id { get; set; }

        public string Name { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }

    }
}
