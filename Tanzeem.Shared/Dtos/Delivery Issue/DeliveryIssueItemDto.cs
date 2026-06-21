using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Delivery_Issue
{
    public class DeliveryIssueItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        //public int? DamagedQuantity { get; set; }
        //public int? DefectiveQuantity { get; set; }
        //public int? MissingQuantity { get; set; }
        //public int? IncorrectQuantity { get; set; }
        public int OrderedQuantity { get; set; }
        public IEnumerable<ItemIssuesDto> Issues { get; set; } = new List<ItemIssuesDto>();

    }
}
