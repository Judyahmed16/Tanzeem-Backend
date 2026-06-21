using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.CustomExceptions
{
    public class DbUpdateFailedException : Exception
    {        
        public DbUpdateFailedException(string message) : base(message)
        {
        }
    }
}
