using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Transactions {
    public class TransactionItem : IAuditable{
    
        public int Id { get; set; }

        public int QuantityOfTransactedItem { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal UnitCost { get; set; }

        public string? BatchNumber { get; set; }


        #region Relationships

        #endregion
        public int TransactionId { get; set; }
        public int ProductId { get; set; }


        #region Navigation

        #endregion
        public Transaction Transaction { get; set; } = default!;
        public required Product Product { get; set; } = default!;

    }
}
