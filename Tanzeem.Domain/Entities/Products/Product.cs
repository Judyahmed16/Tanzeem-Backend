using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Domain.Entities.Products {
    public class Product : IAuditable{
        
        public int Id { get; set; }

        public string Name { get; set; }

        public string SKU { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SellingPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public int ReorderLevel { get; set; }

        public string Status { get; set; }


        #region Relationships
        #endregion
        public int CompanyId { get; set; }
        public int CategoryId { get; set; }


        #region Navigation
        #endregion
        public Company Company { get; set; } = default!;
        public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();
        public Category Category { get; set; }
        public ICollection<DemandForecast> Forecasts { get; set; } = new List<DemandForecast>();

    }
}
