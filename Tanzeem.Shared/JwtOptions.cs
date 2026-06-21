using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared {
    public class JwtOptions {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public string SecurityKey { get; set; }

        public double DurationInDays { get; set; }

    }
}
