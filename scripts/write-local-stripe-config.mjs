import { mkdir, writeFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'

const outputPath = resolve('Tanzeem.Web/appsettings.Development.local.json')
const secretKey = process.env.STRIPE_SECRET_KEY || ''
const publishableKey = process.env.STRIPE_PUBLISHABLE_KEY || ''
const webhookSecret = process.env.STRIPE_WEBHOOK_SECRET || ''
const defaultPriceId = process.env.STRIPE_DEFAULT_PRICE_ID || ''

if (!secretKey.startsWith('sk_test_')) {
  console.error('STRIPE_SECRET_KEY must be a Stripe test secret key that starts with sk_test_.')
  process.exit(1)
}

if (publishableKey && !publishableKey.startsWith('pk_test_')) {
  console.error('STRIPE_PUBLISHABLE_KEY must be a Stripe test publishable key that starts with pk_test_.')
  process.exit(1)
}

if (defaultPriceId && !defaultPriceId.startsWith('price_')) {
  console.error('STRIPE_DEFAULT_PRICE_ID must be a Stripe price id that starts with price_.')
  process.exit(1)
}

const config = {
  StripeOptions: {
    SecretKey: secretKey,
    PublishableKey: publishableKey,
    WebhookSecret: webhookSecret,
    DefaultPriceId: defaultPriceId,
  },
}

await mkdir(dirname(outputPath), { recursive: true })
await writeFile(`${outputPath}.tmp`, `${JSON.stringify(config, null, 2)}\n`)
await writeFile(outputPath, `${JSON.stringify(config, null, 2)}\n`)

console.log(JSON.stringify({
  wrote: outputPath,
  hasSecretKey: true,
  hasPublishableKey: Boolean(publishableKey),
  hasWebhookSecret: Boolean(webhookSecret),
  hasDefaultPriceId: Boolean(defaultPriceId),
}, null, 2))
