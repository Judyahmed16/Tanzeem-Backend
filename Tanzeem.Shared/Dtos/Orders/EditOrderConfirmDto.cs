using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Orders
{
    public class EditOrderConfirmDto
    {
        public IEnumerable<OrderItemsConfirmDto> ItemsConfirmDtos { get; set; } = new List<OrderItemsConfirmDto>();
        public string Notes { get; set; }
    }
}
