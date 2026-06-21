using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared {
    public class StripeOptions {

        public string SecretKey { get; set; } = default!;
        public string PublishableKey { get; set; } = default!;
        public string WebhookSecret { get; set; } = default!;
        public string DefaultPriceId { get; set; } = default!;

    }
}
