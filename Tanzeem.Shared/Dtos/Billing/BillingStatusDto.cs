namespace Tanzeem.Shared.Dtos.Billing;

public class BillingStatusDto
{
    public bool StripeConfigured { get; set; }
    public bool SubscriptionPriceConfigured { get; set; }
    public bool HasStripeCustomer { get; set; }
    public string? StripeCustomerId { get; set; }
    public bool HasSubscription { get; set; }
    public string? Plan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public IReadOnlyList<BillingPaymentMethodDto> PaymentMethods { get; set; } = [];
}

public class BillingPaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "card";
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Wallet { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
    public bool IsDefault { get; set; }
}
