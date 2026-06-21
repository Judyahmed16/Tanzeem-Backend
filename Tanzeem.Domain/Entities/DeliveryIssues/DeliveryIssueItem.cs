using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.DeliveryIssues
{
    public class DeliveryIssueItem : IAuditable
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public IssueType IssueType { get; set; }
        public string? Notes { get; set; }

        public int OrderItemId { get; set; }
        public int DeliveryIssueId { get; set; }

        public OrderItem OrderItem { get; set; }
        public DeliveryIssue DeliveryIssue { get; set; }
    }
}
