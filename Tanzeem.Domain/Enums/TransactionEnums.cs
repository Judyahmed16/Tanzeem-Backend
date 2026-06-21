using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Enums {
    public enum TransactionType {
        In = 1,
        Out = 2,
        Adjustment = 3,
    }

    public enum TransactionStatus {
        Completed = 4,
        Pending = 5,
        Failed = 6,
    }

    public enum TransactionSource {
        Supplier = 7,
        Production = 8,
        Return = 9,
        Recovered = 10,
        FromAnotherBranch = 11,
        Adjustment = 12,
        Selling = 13,
    }


}
