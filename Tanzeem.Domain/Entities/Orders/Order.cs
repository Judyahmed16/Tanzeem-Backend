using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.DeliveryIssues;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.AuditLogs;

namespace Tanzeem.Domain.Entities.Orders
{
    public class Order : IAuditable
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; } // calculated from order items
        public OrderStatus Status { get; set; } = OrderStatus.Pending; //enum
        public DateTime ExpectedDeliveryDate { get; set; } 
        public DateTime? RecievedDeliveryDate { get; set; } // can be null until recieving order
        public string? Notes { get; set; }
        public decimal ShippingCost { get; set; } = 0;
        public decimal Taxes { get; set; } = 0;

        public string SupplierName { get; set; }
        #region Navigation property
        #endregion
        public Supplier? Supplier { get; set; }
        public Branch Branch { get; set; }
        public DeliveryIssue? DeliveryIssue { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        #region Relations
        #endregion
        public int? SupplierId { get; set; }
        public int BranchId { get; set; }


    }
}
