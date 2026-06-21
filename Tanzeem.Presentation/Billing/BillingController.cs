using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tanzeem.Services.Abstractions.Billing;
using Tanzeem.Shared.Dtos.Billing;

namespace Tanzeem.Presentation.Billing;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BillingController(IBillingService billingService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetBillingStatus()
    {
        var result = await billingService.GetBillingStatusAsync();
        return Ok(result);
    }

    [HttpGet("public/status")]
    [AllowAnonymous]
    public IActionResult GetPublicBillingStatus()
    {
        var result = billingService.GetPublicBillingStatus();
        return Ok(result);
    }

    [HttpPost("public/payment-method/setup-session")]
    [AllowAnonymous]
    public async Task<IActionResult> CreatePublicPaymentMethodSetupSession(PublicPaymentMethodSetupRequestDto dto)
    {
        var result = await billingService.CreatePublicPaymentMethodSetupSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("public/payment-method/verify-session")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPublicPaymentMethodSetupSession(PublicPaymentMethodVerifyRequestDto dto)
    {
        var result = await billingService.VerifyPublicPaymentMethodSetupSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("public/subscription/checkout-session")]
    [AllowAnonymous]
    public async Task<IActionResult> CreatePublicSubscriptionCheckoutSession(PublicPaymentMethodSetupRequestDto dto)
    {
        var result = await billingService.CreatePublicSubscriptionCheckoutSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("public/subscription/verify-session")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPublicSubscriptionCheckoutSession(PublicPaymentMethodVerifyRequestDto dto)
    {
        var result = await billingService.VerifyPublicSubscriptionCheckoutSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("payment-method/verify-session")]
    public async Task<IActionResult> VerifyPaymentMethodSetupSession(PublicPaymentMethodVerifyRequestDto dto)
    {
        var result = await billingService.VerifyPaymentMethodSetupSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("subscription/verify-session")]
    public async Task<IActionResult> VerifySubscriptionCheckoutSession(PublicPaymentMethodVerifyRequestDto dto)
    {
        var result = await billingService.VerifySubscriptionCheckoutSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("payment-method/setup-session")]
    public async Task<IActionResult> CreatePaymentMethodSetupSession(BillingSessionRequestDto dto)
    {
        var result = await billingService.CreatePaymentMethodSetupSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("subscription/checkout-session")]
    public async Task<IActionResult> CreateSubscriptionCheckoutSession(BillingSessionRequestDto dto)
    {
        var result = await billingService.CreateSubscriptionCheckoutSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("portal-session")]
    public async Task<IActionResult> CreateBillingPortalSession(BillingSessionRequestDto dto)
    {
        var result = await billingService.CreateBillingPortalSessionAsync(dto);
        return Ok(result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await billingService.HandleStripeWebhookAsync(payload, signature);
        return Ok();
    }
}
