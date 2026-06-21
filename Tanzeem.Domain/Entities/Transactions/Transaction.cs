using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Transactions {

    public class Transaction : IAuditable{

        public int Id { get; set; }

        public string TransactionId { get; set; }         // Frontend Id

        public TransactionType Type { get; set; }          // In_Out_Adjustment

        public DateTime CreatedAt { get; set; }

        public TransactionStatus Status { get; set; }         // Pending_Completed_Failed

        public decimal Value { get; set; }

        public int TotalTransactedItems { get; set; }

        public TransactionSource SourceReason { get; set; }    // Supplier_Return_Production_Recovered_FromAnoterBranch_Adjustment    

        public string ReferenceNumber { get; set; }

        public string Notes { get; set; }




        #region Relationships
        #endregion
        public int BranchId { get; set; }
        public int PerformedByUserId { get; set; }

        #region Navigation
        #endregion
        public Branch Branch { get; set; } = default!;
        public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
        public User PreformedByUser { get; set; }

    }
}   
