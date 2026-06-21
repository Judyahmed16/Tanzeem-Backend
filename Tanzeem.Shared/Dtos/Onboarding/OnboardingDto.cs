using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Shared.Dtos.Onboarding {
    public class OnboardingDto {

        public AdminSignUpDto SignUpDto { get; set; }
        
        public CompanyDto CompanyDto { get; set; }
        
        public BranchDto BranchDto { get; set; }

    }
}
