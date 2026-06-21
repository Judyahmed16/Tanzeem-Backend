using Microsoft.AspNetCore.Mvc;
using Tanzeem.Services.Abstractions.Onboarding;
using Tanzeem.Shared.Dtos.Onboarding;

namespace Tanzeem.Presentation.Onboarding {

    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController(IOnboardingService onboardingService) : ControllerBase {

        [HttpPost]
        [Route("Onboarding")]
        public async Task<IActionResult> OnboardNewTenant(OnboardingDto onboardingDto) {
            var result = await onboardingService.OnboardNewTenantAsync(onboardingDto);
            return Ok(result);
        }

    }
}
