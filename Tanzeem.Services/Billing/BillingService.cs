using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Subscriptions;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Billing;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos.Billing;

namespace Tanzeem.Services.Billing;

public class BillingService(
    IUnitOfWork unitOfWork,
    ICurrentService currentService,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IBillingService
{
    private const string StripeApiBaseUrl = "https://api.stripe.com/v1";

    public async Task<BillingStatusDto> GetBillingStatusAsync()
    {
        var company = await GetCurrentCompanyWithSubscriptionAsync();
        var subscription = company.Subscription;
        var paymentMethods = !string.IsNullOrWhiteSpace(company.StripeCustomerId) && IsStripeConfigured()
            ? await GetBillingPaymentMethodsAsync(company, subscription)
            : [];

        return new BillingStatusDto
        {
            StripeConfigured = IsStripeConfigured(),
            SubscriptionPriceConfigured = IsSubscriptionPriceConfigured(),
            HasStripeCustomer = !string.IsNullOrWhiteSpace(company.StripeCustomerId),
            StripeCustomerId = company.StripeCustomerId,
            HasSubscription = subscription is not null,
            Plan = subscription?.Plan.ToString(),
            SubscriptionStatus = subscription?.Status.ToString(),
            StartedAt = subscription?.StartedAt,
            ExpiresAt = subscription?.ExpiresAt,
            PaymentMethods = paymentMethods
        };
    }

    public BillingStatusDto GetPublicBillingStatus()
    {
        return new BillingStatusDto
        {
            StripeConfigured = IsStripeConfigured(),
            SubscriptionPriceConfigured = IsSubscriptionPriceConfigured()
        };
    }

    public async Task<BillingSessionResponseDto> CreatePublicPaymentMethodSetupSessionAsync(PublicPaymentMethodSetupRequestDto dto)
    {
        var customerId = await EnsurePublicStripeCustomerAsync(dto);
        var successUrl = RequiredUrl(dto.SuccessUrl, "SuccessUrl");
        var cancelUrl = RequiredUrl(dto.CancelUrl, "CancelUrl");

        var json = await PostStripeAsync("checkout/sessions", new Dictionary<string, string>
        {
            ["customer"] = customerId,
            ["mode"] = "setup",
            ["success_url"] = AppendCheckoutSessionPlaceholder(successUrl),
            ["cancel_url"] = cancelUrl,
            ["payment_method_types[]"] = "card",
            ["metadata[source]"] = "public-onboarding",
            ["metadata[selectedPlan]"] = dto.SelectedPlan ?? "starter"
        });

        return new BillingSessionResponseDto
        {
            Url = RequiredJsonString(json, "url"),
            CustomerId = customerId
        };
    }

    public async Task<PublicPaymentMethodVerifyResponseDto> VerifyPublicPaymentMethodSetupSessionAsync(PublicPaymentMethodVerifyRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new BusinessRuleException("SessionId is required.");

        var json = await GetStripeAsync($"checkout/sessions/{Uri.EscapeDataString(dto.SessionId.Trim())}");
        var customerId = RequiredJsonString(json, "customer");
        var mode = ReadString(json.RootElement, "mode");
        var status = ReadString(json.RootElement, "status");
        var setupIntent = ReadString(json.RootElement, "setup_intent");

        if (!string.Equals(mode, "setup", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe session is not a setup session.");

        if (!string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe payment method setup is not complete.");

        if (string.IsNullOrWhiteSpace(setupIntent))
            throw new BusinessRuleException("Stripe session did not save a setup intent.");

        if (!string.IsNullOrWhiteSpace(dto.CustomerId) &&
            !string.Equals(customerId, dto.CustomerId.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessRuleException("Stripe session does not match the onboarding customer.");
        }

        return new PublicPaymentMethodVerifyResponseDto
        {
            Verified = true,
            CustomerId = customerId
        };
    }

    public async Task<BillingSessionResponseDto> CreatePublicSubscriptionCheckoutSessionAsync(PublicPaymentMethodSetupRequestDto dto)
    {
        var customerId = await EnsurePublicStripeCustomerAsync(dto);
        var successUrl = RequiredUrl(dto.SuccessUrl, "SuccessUrl");
        var cancelUrl = RequiredUrl(dto.CancelUrl, "CancelUrl");
        var priceId = FirstNonEmpty(dto.PriceId, configuration["StripeOptions:DefaultPriceId"]);

        if (string.IsNullOrWhiteSpace(priceId))
            throw new BusinessRuleException("Stripe subscription price is not configured. Set StripeOptions:DefaultPriceId or send PriceId.");

        var json = await PostStripeAsync("checkout/sessions", new Dictionary<string, string>
        {
            ["customer"] = customerId,
            ["mode"] = "subscription",
            ["success_url"] = AppendCheckoutSessionPlaceholder(successUrl),
            ["cancel_url"] = cancelUrl,
            ["line_items[0][price]"] = priceId,
            ["line_items[0][quantity]"] = "1",
            ["allow_promotion_codes"] = "true",
            ["metadata[source]"] = "public-onboarding",
            ["metadata[selectedPlan]"] = dto.SelectedPlan ?? "pro",
            ["subscription_data[metadata][source]"] = "public-onboarding"
        });

        return new BillingSessionResponseDto
        {
            Url = RequiredJsonString(json, "url"),
            CustomerId = customerId
        };
    }

    public async Task<PublicPaymentMethodVerifyResponseDto> VerifyPublicSubscriptionCheckoutSessionAsync(PublicPaymentMethodVerifyRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new BusinessRuleException("SessionId is required.");

        var json = await GetStripeAsync($"checkout/sessions/{Uri.EscapeDataString(dto.SessionId.Trim())}");
        var customerId = RequiredJsonString(json, "customer");
        var mode = ReadString(json.RootElement, "mode");
        var status = ReadString(json.RootElement, "status");
        var subscriptionId = ReadString(json.RootElement, "subscription");

        if (!string.Equals(mode, "subscription", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe session is not a subscription checkout session.");

        if (!string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe subscription checkout is not complete.");

        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new BusinessRuleException("Stripe session did not include a subscription.");

        if (!string.IsNullOrWhiteSpace(dto.CustomerId) &&
            !string.Equals(customerId, dto.CustomerId.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessRuleException("Stripe session does not match the onboarding customer.");
        }

        return new PublicPaymentMethodVerifyResponseDto
        {
            Verified = true,
            CustomerId = customerId,
            StripeSubscriptionId = subscriptionId
        };
    }

    public async Task<PublicPaymentMethodVerifyResponseDto> VerifyPaymentMethodSetupSessionAsync(PublicPaymentMethodVerifyRequestDto dto)
    {
        var company = await GetCurrentCompanyAsync();
        var verification = await VerifySetupSessionAsync(dto, company.StripeCustomerId);

        if (string.IsNullOrWhiteSpace(company.StripeCustomerId))
        {
            company.StripeCustomerId = verification.CustomerId;
            unitOfWork.GetRepository<Company>().UpdateAsync(company);
            await unitOfWork.SaveChangesAsync();
        }

        return verification;
    }

    public async Task<PublicPaymentMethodVerifyResponseDto> VerifySubscriptionCheckoutSessionAsync(PublicPaymentMethodVerifyRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new BusinessRuleException("SessionId is required.");

        var company = await GetCurrentCompanyAsync();
        var json = await GetStripeAsync($"checkout/sessions/{Uri.EscapeDataString(dto.SessionId.Trim())}");
        var customerId = RequiredJsonString(json, "customer");
        var mode = ReadString(json.RootElement, "mode");
        var status = ReadString(json.RootElement, "status");
        var subscriptionId = ReadString(json.RootElement, "subscription");

        if (!string.Equals(mode, "subscription", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe session is not a subscription checkout session.");

        if (!string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe subscription checkout is not complete.");

        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new BusinessRuleException("Stripe session did not include a subscription.");

        if (!string.IsNullOrWhiteSpace(company.StripeCustomerId) &&
            !string.Equals(customerId, company.StripeCustomerId, StringComparison.Ordinal))
        {
            throw new BusinessRuleException("Stripe session does not match the workspace customer.");
        }

        if (string.IsNullOrWhiteSpace(company.StripeCustomerId))
        {
            company.StripeCustomerId = customerId;
            unitOfWork.GetRepository<Company>().UpdateAsync(company);
            await unitOfWork.SaveChangesAsync();
        }

        using var subscriptionJson = await GetStripeAsync($"subscriptions/{Uri.EscapeDataString(subscriptionId)}");
        await UpsertSubscriptionFromStripeAsync(subscriptionJson.RootElement);

        return new PublicPaymentMethodVerifyResponseDto
        {
            Verified = true,
            CustomerId = customerId
        };
    }

    public async Task<BillingSessionResponseDto> CreatePaymentMethodSetupSessionAsync(BillingSessionRequestDto dto)
    {
        var company = await GetCurrentCompanyAsync();
        var customerId = await EnsureStripeCustomerAsync(company);

        var successUrl = RequiredUrl(dto.SuccessUrl, "SuccessUrl");
        var cancelUrl = RequiredUrl(dto.CancelUrl, "CancelUrl");

        var json = await PostStripeAsync("checkout/sessions", new Dictionary<string, string>
        {
            ["customer"] = customerId,
            ["mode"] = "setup",
            ["success_url"] = AppendCheckoutSessionPlaceholder(successUrl),
            ["cancel_url"] = cancelUrl,
            ["payment_method_types[]"] = "card",
            ["metadata[companyId]"] = company.Id.ToString()
        });

        return new BillingSessionResponseDto
        {
            Url = RequiredJsonString(json, "url"),
            CustomerId = customerId
        };
    }

    public async Task<BillingSessionResponseDto> CreateSubscriptionCheckoutSessionAsync(BillingSessionRequestDto dto)
    {
        var company = await GetCurrentCompanyAsync();
        var customerId = await EnsureStripeCustomerAsync(company);

        var successUrl = RequiredUrl(dto.SuccessUrl, "SuccessUrl");
        var cancelUrl = RequiredUrl(dto.CancelUrl, "CancelUrl");
        var priceId = FirstNonEmpty(dto.PriceId, configuration["StripeOptions:DefaultPriceId"]);

        if (string.IsNullOrWhiteSpace(priceId))
            throw new BusinessRuleException("Stripe subscription price is not configured. Set StripeOptions:DefaultPriceId or send PriceId.");

        var json = await PostStripeAsync("checkout/sessions", new Dictionary<string, string>
        {
            ["customer"] = customerId,
            ["mode"] = "subscription",
            ["success_url"] = AppendCheckoutSessionPlaceholder(successUrl),
            ["cancel_url"] = cancelUrl,
            ["line_items[0][price]"] = priceId,
            ["line_items[0][quantity]"] = "1",
            ["allow_promotion_codes"] = "true",
            ["metadata[companyId]"] = company.Id.ToString(),
            ["subscription_data[metadata][companyId]"] = company.Id.ToString()
        });

        return new BillingSessionResponseDto
        {
            Url = RequiredJsonString(json, "url"),
            CustomerId = customerId
        };
    }

    public async Task<BillingSessionResponseDto> CreateBillingPortalSessionAsync(BillingSessionRequestDto dto)
    {
        var company = await GetCurrentCompanyAsync();
        var customerId = await EnsureStripeCustomerAsync(company);
        var returnUrl = RequiredUrl(dto.ReturnUrl, "ReturnUrl");

        var json = await PostStripeAsync("billing_portal/sessions", new Dictionary<string, string>
        {
            ["customer"] = customerId,
            ["return_url"] = returnUrl
        });

        return new BillingSessionResponseDto
        {
            Url = RequiredJsonString(json, "url"),
            CustomerId = customerId
        };
    }

    public async Task HandleStripeWebhookAsync(string payload, string signatureHeader)
    {
        var webhookSecret = configuration["StripeOptions:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
            throw new BusinessRuleException("Stripe webhook is not configured. Missing StripeOptions:WebhookSecret.");

        if (!IsValidStripeSignature(payload, signatureHeader, webhookSecret))
            throw new BusinessRuleException("Invalid Stripe webhook signature.");

        using var json = JsonDocument.Parse(payload);
        var eventType = ReadString(json.RootElement, "type");
        if (string.IsNullOrWhiteSpace(eventType) ||
            !json.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("object", out var stripeObject))
        {
            throw new BusinessRuleException("Invalid Stripe webhook payload.");
        }

        if (eventType.StartsWith("customer.subscription.", StringComparison.OrdinalIgnoreCase))
        {
            await UpsertSubscriptionFromStripeAsync(stripeObject);
        }
    }

    private async Task<Company> GetCurrentCompanyAsync()
    {
        var companyId = currentService.CompanyId
            ?? throw new UnauthorizedAccessException("CompanyId not found");

        return await unitOfWork.GetRepository<Company>()
            .GetAllAsIQueryable()
            .AsTracking()
            .FirstOrDefaultAsync(company => company.Id == companyId)
            ?? throw new KeyNotFoundException("Company not found");
    }

    private async Task<Company> GetCurrentCompanyWithSubscriptionAsync()
    {
        var companyId = currentService.CompanyId
            ?? throw new UnauthorizedAccessException("CompanyId not found");

        return await unitOfWork.GetRepository<Company>()
            .GetAllAsIQueryable()
            .Include(company => company.Subscription)
            .FirstOrDefaultAsync(company => company.Id == companyId)
            ?? throw new KeyNotFoundException("Company not found");
    }

    private async Task<string> EnsureStripeCustomerAsync(Company company)
    {
        if (!string.IsNullOrWhiteSpace(company.StripeCustomerId))
            return company.StripeCustomerId;

        var json = await PostStripeAsync("customers", new Dictionary<string, string>
        {
            ["name"] = company.Name,
            ["email"] = company.Email,
            ["phone"] = company.Phone,
            ["metadata[companyId]"] = company.Id.ToString()
        });

        company.StripeCustomerId = RequiredJsonString(json, "id");
        unitOfWork.GetRepository<Company>().UpdateAsync(company);
        await unitOfWork.SaveChangesAsync();

        return company.StripeCustomerId;
    }

    private async Task<string> EnsurePublicStripeCustomerAsync(PublicPaymentMethodSetupRequestDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.CustomerId))
            return dto.CustomerId.Trim();

        var email = FirstNonEmpty(dto.CompanyEmail)
            ?? throw new BusinessRuleException("CompanyEmail is required before payment setup.");

        var json = await PostStripeAsync("customers", new Dictionary<string, string>
        {
            ["name"] = FirstNonEmpty(dto.CompanyName) ?? email,
            ["email"] = email,
            ["phone"] = FirstNonEmpty(dto.CompanyPhone) ?? string.Empty,
            ["metadata[source]"] = "public-onboarding",
            ["metadata[selectedPlan]"] = dto.SelectedPlan ?? "starter"
        });

        return RequiredJsonString(json, "id");
    }

    private async Task<PublicPaymentMethodVerifyResponseDto> VerifySetupSessionAsync(
        PublicPaymentMethodVerifyRequestDto dto,
        string? expectedCustomerId)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new BusinessRuleException("SessionId is required.");

        var json = await GetStripeAsync($"checkout/sessions/{Uri.EscapeDataString(dto.SessionId.Trim())}");
        var customerId = RequiredJsonString(json, "customer");
        var mode = ReadString(json.RootElement, "mode");
        var status = ReadString(json.RootElement, "status");
        var setupIntent = ReadString(json.RootElement, "setup_intent");
        var comparisonCustomerId = FirstNonEmpty(expectedCustomerId, dto.CustomerId);

        if (!string.Equals(mode, "setup", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe session is not a setup session.");

        if (!string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Stripe payment method setup is not complete.");

        if (string.IsNullOrWhiteSpace(setupIntent))
            throw new BusinessRuleException("Stripe session did not save a setup intent.");

        if (!string.IsNullOrWhiteSpace(comparisonCustomerId) &&
            !string.Equals(customerId, comparisonCustomerId.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessRuleException("Stripe session does not match the workspace customer.");
        }

        return new PublicPaymentMethodVerifyResponseDto
        {
            Verified = true,
            CustomerId = customerId
        };
    }

    private async Task<IReadOnlyList<BillingPaymentMethodDto>> GetBillingPaymentMethodsAsync(
        Company company,
        Subscription? subscription)
    {
        var methods = new List<BillingPaymentMethodDto>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var customerId = company.StripeCustomerId;

        if (string.IsNullOrWhiteSpace(customerId))
            return methods;

        if (!string.IsNullOrWhiteSpace(subscription?.StripeSubscriptionId))
        {
            try
            {
                using var subscriptionJson = await GetStripeAsync(
                    $"subscriptions/{Uri.EscapeDataString(subscription.StripeSubscriptionId)}?expand%5B%5D=default_payment_method&expand%5B%5D=latest_invoice.payment_intent.payment_method");

                await AddPaymentMethodFromPropertyAsync(methods, seenIds, subscriptionJson.RootElement, "default_payment_method", true);

                if (TryGetNestedProperty(
                    subscriptionJson.RootElement,
                    out var latestInvoicePaymentMethod,
                    "latest_invoice",
                    "payment_intent",
                    "payment_method"))
                {
                    await AddPaymentMethodFromElementAsync(methods, seenIds, latestInvoicePaymentMethod, true);
                }
            }
            catch
            {
                // A stale Stripe subscription id should not make the billing page unusable.
            }
        }

        try
        {
            using var customerJson = await GetStripeAsync(
                $"customers/{Uri.EscapeDataString(customerId)}?expand%5B%5D=invoice_settings.default_payment_method");

            if (TryGetNestedProperty(
                customerJson.RootElement,
                out var invoiceDefaultPaymentMethod,
                "invoice_settings",
                "default_payment_method"))
            {
                await AddPaymentMethodFromElementAsync(methods, seenIds, invoiceDefaultPaymentMethod, true);
            }
        }
        catch
        {
            // Fall back to the customer payment method collection below.
        }

        foreach (var method in await GetCustomerCardPaymentMethodsAsync(customerId))
            AddPaymentMethod(methods, seenIds, method);

        return methods;
    }

    private async Task<IReadOnlyList<BillingPaymentMethodDto>> GetCustomerCardPaymentMethodsAsync(string customerId)
    {
        using var json = await GetStripeAsync($"customers/{Uri.EscapeDataString(customerId)}/payment_methods?type=card&limit=5");

        if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return [];

        return data.EnumerateArray()
            .Where(item => item.TryGetProperty("card", out var card) && card.ValueKind == JsonValueKind.Object)
            .Select(item => MapStripePaymentMethod(item, false))
            .ToList();
    }

    private async Task AddPaymentMethodFromPropertyAsync(
        List<BillingPaymentMethodDto> methods,
        HashSet<string> seenIds,
        JsonElement owner,
        string propertyName,
        bool isDefault)
    {
        if (owner.TryGetProperty(propertyName, out var property))
            await AddPaymentMethodFromElementAsync(methods, seenIds, property, isDefault);
    }

    private async Task AddPaymentMethodFromElementAsync(
        List<BillingPaymentMethodDto> methods,
        HashSet<string> seenIds,
        JsonElement element,
        bool isDefault)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            AddPaymentMethod(methods, seenIds, MapStripePaymentMethod(element, isDefault));
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
            return;

        var paymentMethodId = element.GetString();
        if (string.IsNullOrWhiteSpace(paymentMethodId))
            return;

        using var paymentMethodJson = await GetStripeAsync($"payment_methods/{Uri.EscapeDataString(paymentMethodId)}");
        AddPaymentMethod(methods, seenIds, MapStripePaymentMethod(paymentMethodJson.RootElement, isDefault));
    }

    private static void AddPaymentMethod(
        List<BillingPaymentMethodDto> methods,
        HashSet<string> seenIds,
        BillingPaymentMethodDto method)
    {
        if (string.IsNullOrWhiteSpace(method.Id))
        {
            methods.Add(method);
            return;
        }

        if (seenIds.Add(method.Id))
        {
            methods.Add(method);
            return;
        }

        var existing = methods.FirstOrDefault(item => item.Id == method.Id);
        if (existing is not null)
            existing.IsDefault = existing.IsDefault || method.IsDefault;
    }

    private static BillingPaymentMethodDto MapStripePaymentMethod(JsonElement item, bool isDefault)
    {
        var type = ReadString(item, "type") ?? "payment_method";
        var id = ReadString(item, "id") ?? string.Empty;

        if (string.Equals(type, "card", StringComparison.OrdinalIgnoreCase) &&
            item.TryGetProperty("card", out var card) &&
            card.ValueKind == JsonValueKind.Object)
        {
            var brand = ReadString(card, "brand") ?? "Card";
            var last4 = ReadString(card, "last4") ?? string.Empty;
            var wallet = card.TryGetProperty("wallet", out var walletElement) && walletElement.ValueKind == JsonValueKind.Object
                ? ReadString(walletElement, "type")
                : null;
            var isLinkCard = string.Equals(wallet, "link", StringComparison.OrdinalIgnoreCase);

            return new BillingPaymentMethodDto
            {
                Id = id,
                Type = type,
                Brand = isLinkCard ? "Link" : brand,
                Last4 = last4,
                DisplayName = isLinkCard
                    ? string.IsNullOrWhiteSpace(last4) ? "Link payment" : $"Link card ending {last4}"
                    : string.IsNullOrWhiteSpace(last4) ? ToTitleCase(brand) : $"{ToTitleCase(brand)} ending {last4}",
                Wallet = wallet,
                ExpMonth = ReadInt(card, "exp_month"),
                ExpYear = ReadInt(card, "exp_year"),
                IsDefault = isDefault
            };
        }

        return new BillingPaymentMethodDto
        {
            Id = id,
            Type = type,
            Brand = ToTitleCase(type),
            DisplayName = string.Equals(type, "link", StringComparison.OrdinalIgnoreCase)
                ? "Link payment"
                : $"{ToTitleCase(type)} payment method",
            IsDefault = isDefault
        };
    }

    private static bool TryGetNestedProperty(JsonElement element, out JsonElement value, params string[] propertyNames)
    {
        value = element;

        foreach (var propertyName in propertyNames)
        {
            if (value.ValueKind != JsonValueKind.Object ||
                !value.TryGetProperty(propertyName, out value) ||
                value.ValueKind == JsonValueKind.Null)
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static string ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Payment";

        var normalized = value.Trim().Replace("_", " ");
        return string.Join(
            " ",
            normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private bool IsStripeConfigured()
    {
        return !string.IsNullOrWhiteSpace(configuration["StripeOptions:SecretKey"]);
    }

    private bool IsSubscriptionPriceConfigured()
    {
        return !string.IsNullOrWhiteSpace(configuration["StripeOptions:DefaultPriceId"]);
    }

    private async Task<JsonDocument> PostStripeAsync(string path, Dictionary<string, string> formValues)
    {
        var secretKey = configuration["StripeOptions:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new BusinessRuleException("Stripe is not configured. Missing StripeOptions:SecretKey.");

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var content = new FormUrlEncodedContent(formValues);
        using var response = await client.PostAsync($"{StripeApiBaseUrl}/{path}", content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BusinessRuleException($"Stripe request failed: {ExtractStripeError(responseText)}");

        return JsonDocument.Parse(responseText);
    }

    private async Task<JsonDocument> GetStripeAsync(string path)
    {
        var secretKey = configuration["StripeOptions:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new BusinessRuleException("Stripe is not configured. Missing StripeOptions:SecretKey.");

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var response = await client.GetAsync($"{StripeApiBaseUrl}/{path}");
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new BusinessRuleException($"Stripe request failed: {ExtractStripeError(responseText)}");

        return JsonDocument.Parse(responseText);
    }

    private static string AppendCheckoutSessionPlaceholder(string url)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}session_id={{CHECKOUT_SESSION_ID}}";
    }

    private static string RequiredJsonString(JsonDocument json, string propertyName)
    {
        if (json.RootElement.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            return property.GetString()!;
        }

        throw new BusinessRuleException($"Stripe response did not include {propertyName}.");
    }

    private static string RequiredUrl(string? url, string fieldName)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.ToString();
        }

        throw new BusinessRuleException($"{fieldName} must be an absolute http or https URL.");
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private async Task UpsertSubscriptionFromStripeAsync(JsonElement subscriptionObject)
    {
        var stripeSubscriptionId = ReadString(subscriptionObject, "id");
        var stripeCustomerId = ReadString(subscriptionObject, "customer");

        if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            throw new BusinessRuleException("Stripe subscription payload did not include id.");

        var company = await FindWebhookCompanyAsync(subscriptionObject, stripeCustomerId);
        if (company is null)
            throw new BusinessRuleException("Could not match Stripe subscription to a company.");

        if (string.IsNullOrWhiteSpace(company.StripeCustomerId) && !string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            company.StripeCustomerId = stripeCustomerId;
            unitOfWork.GetRepository<Company>().UpdateAsync(company);
        }

        var subscription = await unitOfWork.GetRepository<Subscription>()
            .GetAllAsIQueryable()
            .AsTracking()
            .FirstOrDefaultAsync(item =>
                item.StripeSubscriptionId == stripeSubscriptionId ||
                item.CompanyId == company.Id);

        var isNewSubscription = subscription is null;
        if (isNewSubscription)
        {
            subscription = new Subscription
            {
                CompanyId = company.Id,
                StripeSubscriptionId = stripeSubscriptionId
            };
            await unitOfWork.GetRepository<Subscription>().AddAsync(subscription);
        }

        ArgumentNullException.ThrowIfNull(subscription);

        subscription.StripeSubscriptionId = stripeSubscriptionId;
        subscription.CompanyId = company.Id;
        subscription.Plan = PlanStatus.Pro;
        subscription.Status = IsStripeSubscriptionActive(ReadString(subscriptionObject, "status"))
            ? SubscriptionStatus.Active
            : SubscriptionStatus.Expired;
        subscription.StartedAt = ReadUnixTime(subscriptionObject, "current_period_start") ?? subscription.StartedAt;
        subscription.ExpiresAt = ReadUnixTime(subscriptionObject, "current_period_end") ?? subscription.ExpiresAt;

        if (subscription.StartedAt == default)
            subscription.StartedAt = DateTime.UtcNow;

        if (subscription.ExpiresAt == default)
            subscription.ExpiresAt = subscription.StartedAt.AddMonths(1);

        if (!isNewSubscription)
            unitOfWork.GetRepository<Subscription>().UpdateAsync(subscription);

        await unitOfWork.SaveChangesAsync();
    }

    private async Task<Company?> FindWebhookCompanyAsync(JsonElement stripeObject, string? stripeCustomerId)
    {
        var metadataCompanyId = ReadMetadataInt(stripeObject, "companyId");
        if (metadataCompanyId.HasValue)
        {
            var companyByMetadata = await unitOfWork.GetRepository<Company>()
                .GetAllAsIQueryable()
                .AsTracking()
                .FirstOrDefaultAsync(company => company.Id == metadataCompanyId.Value);

            if (companyByMetadata is not null)
                return companyByMetadata;
        }

        if (string.IsNullOrWhiteSpace(stripeCustomerId))
            return null;

        return await unitOfWork.GetRepository<Company>()
            .GetAllAsIQueryable()
            .AsTracking()
            .FirstOrDefaultAsync(company => company.StripeCustomerId == stripeCustomerId);
    }

    private static bool IsValidStripeSignature(string payload, string signatureHeader, string webhookSecret)
    {
        var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var timestamp = parts.FirstOrDefault(part => part.StartsWith("t=", StringComparison.OrdinalIgnoreCase))?[2..];
        var signature = parts.FirstOrDefault(part => part.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))?[3..];

        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
            return false;

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static DateTime? ReadUnixTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            return null;

        return property.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            return null;

        return property.TryGetInt32(out var value) ? value : null;
    }

    private static int? ReadMetadataInt(JsonElement element, string metadataKey)
    {
        if (!element.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            return null;

        var value = ReadString(metadata, metadataKey);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool IsStripeSubscriptionActive(string? status)
    {
        return status is "active" or "trialing";
    }

    private static string ExtractStripeError(string responseText)
    {
        try
        {
            using var json = JsonDocument.Parse(responseText);
            if (json.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? responseText;
            }
        }
        catch
        {
            // Fall through to raw text.
        }

        return responseText;
    }
}
