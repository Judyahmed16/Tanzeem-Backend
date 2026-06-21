using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Onboarding;

namespace Tanzeem.Services.Abstractions.Onboarding {
    public interface IOnboardingService {
        Task<string> OnboardNewTenantAsync(OnboardingDto onboardingDto);
    }
}
