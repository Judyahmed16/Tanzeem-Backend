using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Products {
    public class ProductDto {

        public int Id { get; set; }

        public string? ProductNumber { get; set; }

        public string Name { get; set; }

        public string SKU { get; set; }

        public string? Category { get; set; }

        public int CategoryId { get; set; }

        public int? Stock { get; set; }

        public string? BatchNumber { get; set; }

        public IEnumerable<InventoryBatchDto> Batches { get; set; } = new List<InventoryBatchDto>();

        public decimal CostPrice { get; set; }

        public decimal SellingPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public int ReorderLevel { get; set; }

        public string Status { get; set; }

    }
}
