import { readFile, writeFile } from 'node:fs/promises'
import { resolve } from 'node:path'

const configPath = resolve('Tanzeem.Web/appsettings.Development.local.json')
const lookupKey = process.env.STRIPE_PRICE_LOOKUP_KEY || 'tanzeem_pro_yearly_local'
const productName = process.env.STRIPE_PRODUCT_NAME || 'Tanzeem Pro'
const currency = (process.env.STRIPE_PRICE_CURRENCY || 'egp').toLowerCase()
const amount = Number(process.env.STRIPE_PRICE_AMOUNT || 99900)
const interval = process.env.STRIPE_PRICE_INTERVAL || 'year'
const shouldCreate = process.argv.includes('--create')

if (!Number.isInteger(amount) || amount <= 0) {
  console.error('STRIPE_PRICE_AMOUNT must be a positive integer in the smallest currency unit.')
  process.exit(1)
}

const config = JSON.parse(await readFile(configPath, 'utf8'))
const stripeOptions = config.StripeOptions || {}
const secretKey = stripeOptions.SecretKey || process.env.STRIPE_SECRET_KEY || ''

if (!secretKey.startsWith('sk_test_')) {
  console.error('A Stripe test secret key is required in Tanzeem.Web/appsettings.Development.local.json.')
  process.exit(1)
}

async function stripeRequest(path, options = {}) {
  const response = await fetch(`https://api.stripe.com/v1/${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${secretKey}`,
      ...(options.body ? { 'Content-Type': 'application/x-www-form-urlencoded' } : {}),
      ...options.headers,
    },
  })
  const text = await response.text()
  const data = text ? JSON.parse(text) : {}
  if (!response.ok) {
    const message = data?.error?.message || text || `Stripe request failed with ${response.status}`
    throw new Error(message)
  }
  return data
}

function formBody(values) {
  const params = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  })
  return params
}

async function findPriceByLookupKey() {
  const data = await stripeRequest(`prices?active=true&limit=10&lookup_keys[]=${encodeURIComponent(lookupKey)}`)
  return data.data?.find((price) => price.lookup_key === lookupKey) || null
}

async function writePrice(price) {
  config.StripeOptions = {
    ...stripeOptions,
    DefaultPriceId: price.id,
  }
  await writeFile(configPath, `${JSON.stringify(config, null, 2)}\n`)
}

let price = await findPriceByLookupKey()
if (!price && shouldCreate) {
  const product = await stripeRequest('products', {
    method: 'POST',
    body: formBody({
      name: productName,
      'metadata[source]': 'tanzeem-local-dev',
    }),
  })

  price = await stripeRequest('prices', {
    method: 'POST',
    body: formBody({
      product: product.id,
      currency,
      unit_amount: amount,
      lookup_key: lookupKey,
      'recurring[interval]': interval,
      'metadata[source]': 'tanzeem-local-dev',
    }),
  })
}

if (!price) {
  console.log(JSON.stringify({
    configured: false,
    reason: 'No active Stripe price found for lookup key. Run with --create after confirming you want to create a test Stripe product and price.',
    lookupKey,
  }, null, 2))
  process.exit(1)
}

await writePrice(price)

console.log(JSON.stringify({
  configured: true,
  priceId: price.id,
  lookupKey,
  currency: price.currency,
  unitAmount: price.unit_amount,
  recurring: price.recurring,
}, null, 2))
