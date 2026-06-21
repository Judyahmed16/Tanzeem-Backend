using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Inventories {
    public class Inventory : IAuditable {
    
        public int Id { get; set; }

        public int? Quantity { get; set; }


        #region Relationships

        #endregion
        public int ProductId { get; set; }
        public int BranchId { get; set; }

        #region Navigation
        #endregion
        public Product Product { get; set; } = default!;
        public Branch Branch { get; set; } = default!;


    }
}
