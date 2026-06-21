using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Suppliers;

namespace Tanzeem.Domain.Entities.DeliveryIssues
{
    public class DeliveryIssue : IAuditable
    {
        public int Id { get; set; }
        public string DeliveryIssueNumber { get; set; }
        public DateTime RecieveDate { get; set; }
        public int ItemsAffected { get; set; }
        public int Discrepancy { get; set; }
        public string? Notes { get; set; }
        public string SupplierName { get; set; }

        public int OrderId { get; set; }
        public int? SupplierId { get; set; }
        public int BranchId { get; set; }


        public Order Order { get; set; }
        public Supplier? Supplier { get; set; }
        public Branch Branch { get; set; }
        public ICollection<DeliveryIssueItem> DeliveryIssueItem { get; set; } = new List<DeliveryIssueItem>(); //items
    }
}
