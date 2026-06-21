namespace Tanzeem.Shared.Dtos.Billing;

public class BillingSessionResponseDto
{
    public string Url { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
}

public class PublicPaymentMethodVerifyRequestDto
{
    public string? SessionId { get; set; }
    public string? CustomerId { get; set; }
}

public class PublicPaymentMethodVerifyResponseDto
{
    public bool Verified { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? StripeSubscriptionId { get; set; }
}
