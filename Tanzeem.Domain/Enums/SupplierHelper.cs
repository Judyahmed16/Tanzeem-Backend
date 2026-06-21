using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Enums
{
    public enum SupplierFilter
    {
        ActiveSuppliers,
        InActiveSuppliers
    }
    
    public enum SupplierSort
    {
        AZSupplierName,
        ZASupplierName,
        AZCity,
        ZACity,
        HighOrdersCount,
        LowOrdersCount,
    }

}
