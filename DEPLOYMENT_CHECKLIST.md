# Tanzeem Backend Deployment Checklist

## What This Build Fixes

- Product creation now works when the submitted category already exists in the company.
- Branch switching and employee branch assignment now update the single primary branch relation in a transaction-safe order.
- Forgot password now reports missing SMTP settings clearly and does not persist an OTP when email sending fails.
- Delivery issue list mapping now tolerates deleted or missing suppliers.
- JWT generation now tolerates users without a company id.
- Local development can disable Hangfire background jobs with `Hangfire:Enabled = false`.
- A repair migration makes the Stripe user/subscription columns idempotently recover if EF history says the old subscription migration ran but the physical columns are missing.

## Required Production Settings

Set these in the hosting provider's environment/configuration system before testing forgot password:

```text
SmtpSettings__Host
SmtpSettings__Port
SmtpSettings__Email
SmtpSettings__Password
SmtpSettings__DisplayName
```

Keep these production settings configured as environment variables or provider secrets:

```text
ConnectionStrings__DefaultConnection
JwtOptions__Issuer
JwtOptions__Audience
JwtOptions__DurationInDays
JwtOptions__SecurityKey
StripeOptions__SecretKey
StripeOptions__PublishableKey
StripeOptions__webhookSecret
AIModels__ForecastApiUrl
```

For production, leave Hangfire enabled unless you intentionally want to stop background jobs:

```text
Hangfire__Enabled=true
```

## Pre-Deploy Verification

From the backend repo:

```sh
/usr/local/share/dotnet/x64/dotnet tool restore
/usr/local/share/dotnet/x64/dotnet build Tanzeem.sln --no-restore
```

Expected result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Run locally:

```sh
ASPNETCORE_ENVIRONMENT=Development \
/usr/local/share/dotnet/x64/dotnet dotnet-ef database update \
  --project Tanzeem.Persistence/Tanzeem.Persistence.csproj \
  --startup-project Tanzeem.Web/Tanzeem.Web.csproj \
  --context TanzeemDbContext

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:5131 \
/usr/local/share/dotnet/x64/dotnet run --project Tanzeem.Web/Tanzeem.Web.csproj --no-build
```

From the copied frontend audit repo:

```sh
cd /Users/judyahmed/TanzeemFrontendLocal
npm run audit:api
```

Expected result:

```text
passed: 59
failed: 0
skipped: 0
```

## Post-Deploy Verification

After deploying the backend, run:

```sh
cd /Users/judyahmed/TanzeemFrontendLocal
API_BASE_URL=https://tanzeem.runasp.net npm run audit:api
```

The audit creates and then deletes a disposable tenant. To keep the tenant for inspection, add `AUDIT_SKIP_CLEANUP=true`.

The previous production failure was:

```text
product create existing category - 500 Un-Expected Error occur, please try again later
```

That must pass after deployment.

Also test forgot password after SMTP settings are configured:

```sh
curl -X POST https://tanzeem.runasp.net/api/Auth/forget-password/request \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"your-test-email@example.com\"}"
```

Expected result with SMTP configured:

```json
{
  "success": true,
  "message": "OTP has been sent to your email. Please check your inbox (and spam folder).",
  "data": {
    "email": "your-test-email@example.com",
    "expiresIn": "10 minutes"
  }
}
```
