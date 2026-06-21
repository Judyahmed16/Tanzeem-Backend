const baseUrl = process.env.API_BASE_URL || 'http://localhost:5131'

const response = await fetch(`${baseUrl}/api/Billing/public/status`)
const text = await response.text()

let status
try {
  status = text ? JSON.parse(text) : null
} catch {
  throw new Error(`Billing status returned non-JSON response: ${text}`)
}

if (!response.ok) {
  const message = status?.message || status?.Message || status?.title || status?.Title || text
  throw new Error(`Billing status failed: ${response.status} ${message}`)
}

const stripeConfigured = Boolean(status?.stripeConfigured ?? status?.StripeConfigured)
const subscriptionPriceConfigured = Boolean(status?.subscriptionPriceConfigured ?? status?.SubscriptionPriceConfigured)

console.log(JSON.stringify({
  baseUrl,
  stripeConfigured,
  subscriptionPriceConfigured,
  paymentMethodSetupReady: stripeConfigured,
  subscriptionCheckoutReady: stripeConfigured && subscriptionPriceConfigured,
}, null, 2))

if (!stripeConfigured) {
  process.exitCode = 1
}
