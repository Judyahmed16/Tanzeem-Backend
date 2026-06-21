using Tanzeem.Shared.Dtos.Billing;

namespace Tanzeem.Services.Abstractions.Billing;

public interface IBillingService
{
    Task<BillingStatusDto> GetBillingStatusAsync();
    BillingStatusDto GetPublicBillingStatus();
    Task<BillingSessionResponseDto> CreatePublicPaymentMethodSetupSessionAsync(PublicPaymentMethodSetupRequestDto dto);
    Task<PublicPaymentMethodVerifyResponseDto> VerifyPublicPaymentMethodSetupSessionAsync(PublicPaymentMethodVerifyRequestDto dto);
    Task<BillingSessionResponseDto> CreatePublicSubscriptionCheckoutSessionAsync(PublicPaymentMethodSetupRequestDto dto);
    Task<PublicPaymentMethodVerifyResponseDto> VerifyPublicSubscriptionCheckoutSessionAsync(PublicPaymentMethodVerifyRequestDto dto);
    Task<PublicPaymentMethodVerifyResponseDto> VerifyPaymentMethodSetupSessionAsync(PublicPaymentMethodVerifyRequestDto dto);
    Task<PublicPaymentMethodVerifyResponseDto> VerifySubscriptionCheckoutSessionAsync(PublicPaymentMethodVerifyRequestDto dto);
    Task<BillingSessionResponseDto> CreatePaymentMethodSetupSessionAsync(BillingSessionRequestDto dto);
    Task<BillingSessionResponseDto> CreateSubscriptionCheckoutSessionAsync(BillingSessionRequestDto dto);
    Task<BillingSessionResponseDto> CreateBillingPortalSessionAsync(BillingSessionRequestDto dto);
    Task HandleStripeWebhookAsync(string payload, string signatureHeader);
}
