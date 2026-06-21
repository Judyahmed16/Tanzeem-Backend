# Tanzeem Backend

## Local development

This repo targets .NET 8. On this machine, the working SDK is:

```sh
/usr/local/share/dotnet/x64/dotnet --version
```

Build:

```sh
/usr/local/share/dotnet/x64/dotnet build Tanzeem.sln --no-restore
```

Install repo-local .NET tools:

```sh
/usr/local/share/dotnet/x64/dotnet tool restore
```

For local development on this machine, `Tanzeem.Web/appsettings.Development.json` is set to use a local SQLite file:

```text
Tanzeem.Web/.local/tanzeem-dev.db
```

Do not point Development at the original/shared backend database. The app has a guard that refuses known shared connection strings in Development.

With SQLite, do not run EF migrations. The local database is created automatically when the API starts. Only use migrations when you intentionally configure a separate SQL Server database that belongs to you:

```sh
/usr/local/share/dotnet/x64/dotnet dotnet-ef database update \
  --project Tanzeem.Persistence/Tanzeem.Persistence.csproj \
  --startup-project Tanzeem.Web/Tanzeem.Web.csproj \
  --context TanzeemDbContext
```

Run the API locally:

```sh
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:5131 \
/usr/local/share/dotnet/x64/dotnet run --project Tanzeem.Web/Tanzeem.Web.csproj
```

Swagger UI:

```text
http://localhost:5131/index.html
```

Swagger JSON:

```text
http://localhost:5131/swagger/v1/swagger.json
```

## Frontend connection

In the frontend repo, set:

```env
VITE_API_BASE_URL=http://localhost:5131
```

Then run:

```sh
npm run dev
```

## Development defaults

`Tanzeem.Web/appsettings.Development.json` disables Hangfire locally so background jobs do not run while you are debugging API requests.

Local CORS allows both `localhost:5173` and `127.0.0.1:5173`, so the copied Vite frontend can run from either host.

## Billing and Stripe

Billing uses Stripe-hosted Checkout and Billing Portal sessions. Configure these values only for an isolated local/test Stripe setup:

```json
{
  "StripeOptions": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "DefaultPriceId": "price_..."
  }
}
```

For local signup payment testing, create a private file named `Tanzeem.Web/appsettings.Development.local.json`. This file is ignored by git. You can copy `Tanzeem.Web/appsettings.Development.local.example.json` and replace the placeholders.

Get test keys from Stripe Dashboard:

1. Open [Stripe API keys](https://dashboard.stripe.com/test/apikeys).
2. Make sure you are in test mode.
3. Copy the publishable key that starts with `pk_test_`.
4. Reveal/copy the secret key that starts with `sk_test_`.
5. Put them in `appsettings.Development.local.json`.

The signup payment-method step needs `SecretKey` to open Stripe Checkout. `DefaultPriceId` is only needed for subscription checkout. The payment-method setup flow returns through `/signup?billing=success&session_id=...`; the frontend then calls the backend verification endpoint before it marks the payment method as ready.

You can also use environment variables instead of the private JSON file:

```sh
export STRIPE_SECRET_KEY=sk_test_...
export STRIPE_PUBLISHABLE_KEY=pk_test_...
```

Or write the private local config file from environment variables:

```sh
STRIPE_SECRET_KEY=sk_test_... STRIPE_PUBLISHABLE_KEY=pk_test_... node scripts/write-local-stripe-config.mjs
```

Restart the local backend after changing Stripe settings:

```sh
/usr/local/share/dotnet/x64/dotnet run --no-build --project Tanzeem.Web/Tanzeem.Web.csproj --urls http://localhost:5131
```

Without `SecretKey`, checkout and portal creation are disabled. Without `DefaultPriceId`, subscription checkout is disabled, but the billing status endpoint still works.

## Forgot password email

Forgot password requires SMTP settings. Configure them with user secrets, environment variables, or deployment settings:

```json
{
  "SmtpSettings": {
    "Host": "smtp.example.com",
    "Port": "587",
    "Email": "no-reply@example.com",
    "Password": "smtp-password-or-app-password",
    "DisplayName": "Tanzeem"
  }
}
```

Without these settings, `/api/Auth/forget-password/request` returns a clear configuration error instead of the old misleading `no domain for this company` message.

## Deployment

See `DEPLOYMENT_CHECKLIST.md` before publishing this backend. It lists the fixes in this build, required production settings, and the post-deploy audit command.
