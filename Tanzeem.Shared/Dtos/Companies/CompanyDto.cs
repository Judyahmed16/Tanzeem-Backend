using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Companies {
    public class CompanyDto {

        public string Name { get; set; }

        public string Field { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string? StripeCustomerId { get; set; }

        public string? StripeSubscriptionId { get; set; }

    }
}
