using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Suppliers
{
    //for dropdown menu of create order
    public class SupplierLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
    }
}
