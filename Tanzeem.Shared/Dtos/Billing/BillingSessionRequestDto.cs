namespace Tanzeem.Shared.Dtos.Billing;

public class BillingSessionRequestDto
{
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? ReturnUrl { get; set; }
    public string? PriceId { get; set; }
}

public class PublicPaymentMethodSetupRequestDto : BillingSessionRequestDto
{
    public string? CustomerId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyPhone { get; set; }
    public string? SelectedPlan { get; set; }
}
