using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Transactions {
    public class TransactionDto {

        public int Id { get; set; }
        public string TransactionId { get; set; }
        public string? TransactionNumber { get; set; }

        public TransactionType Type { get; set; }          // In_Out_Adjustment

        public DateTime CreatedAt { get; set; }

        public TransactionStatus Status { get; set; }         // Pending_Completed_Failed

        public decimal Value { get; set; }

        public int TotalTransactedItems { get; set; }

        public TransactionSource SourceReason { get; set; }     // Supplier_Production_etc

        public string ReferenceNumber { get; set; }

        public string Notes { get; set; }

        public string PreformedBy { get; set; }

        public List<TransactionItemDto> TransactionItemDtos { get; set; } = new List<TransactionItemDto>();

    }
}
