using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.DeliveryIssues;

namespace Tanzeem.Shared.Dtos.Delivery_Issue
{
    public class DeliveryIssueDto
    {
        public int Id { get; set; }
        public string StringId { get; set; }
        public int OrderId { get; set; }
        public string OrderDisplayId => $"ORD-{Id:D4}";
        public DateTime RecievedDate { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string SupplierPhone { get; set; }
        public int ItemsAffected { get; set; }
        public int Discrepancy { get; set; }
        public string? Notes { get; set; }

        public IEnumerable<DeliveryIssueItemDto> Items { get; set; } = new List<DeliveryIssueItemDto>();
    }
}
