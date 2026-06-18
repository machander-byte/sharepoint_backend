# ZMS Pricing And Go-To-Market Review - 2026-06-18

This document converts the pasted pricing strategy into a reviewer-safe version. It should not be used to claim product launch readiness. ZMS still needs Tier 1 product gaps closed before commercial launch.

## Executive Position

ZMS should not compete with ShareGate on full feature parity today. The first commercial angle should be narrower:

> ZMS is a lower-cost, evidence-first Microsoft 365 migration pilot and readiness platform for teams that want byte-verified migration proof, risk assessment, planning, validation, and reports before committing to a full enterprise migration program.

The pricing argument is valid only if the claim boundary is tight. We can position ZMS as cheaper and more customizable for controlled pilots and selected migration paths. We should not claim it replaces ShareGate across OneDrive, Teams, Exchange, Box, metadata writeback, permissions, versions, and enterprise scale until those are certified.

## ShareGate Pricing Baseline

ShareGate's public site currently states that ShareGate Migrate uses annual flat-rate plans based on machine activations:

| ShareGate plan | Public starting price | Machine activations | Approx INR at 94.2 INR/USD |
| --- | ---: | ---: | ---: |
| Migrate Essentials | $5,995/year | 1 | about Rs 5.65 lakh/year |
| Migrate Pro | $9,995/year | up to 5 | about Rs 9.42 lakh/year |
| Migrate Enterprise | $17,995/year | up to 25 | about Rs 16.95 lakh/year |

Important ShareGate facts to acknowledge:

- ShareGate includes a 15-day trial.
- ShareGate Migrate has no end-user metered licensing and no tenant limit.
- The limits are based on machine activations/workstations.
- Pro and Enterprise enable multiple parallel migrations.
- ShareGate has broad workload support that ZMS does not yet match.

This means ZMS should not use unsupported statements like "ShareGate costs Rs 40+ lakh" unless there is a specific quote or customer contract. The safer claim is:

> Against ShareGate's public starting prices, ZMS can be priced 50-65% lower for pilot and controlled migration use cases.

## Proposed ZMS Pricing

These tiers are proposed for a future commercial launch after Phase 2/Phase 3 certification, not for today's pre-production demo.

| Tier | Annual price | Migrations/year | File capacity/run | Admin users | Support | Target buyer |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| Starter | Rs 1.99 lakh | 10 | 10,000 files | 3 | Email, 48h | Small teams and pilots |
| Professional | Rs 3.99 lakh | 50 | 100,000 files | 8 | Email/Slack, 24h | Mid-market and department migrations |
| Enterprise | Rs 5.99 lakh | Unlimited | 1M+ files after certification | 25+ | Dedicated Slack, 4h | Large organizations and multi-wave migrations |
| Partner/White Label | Custom | Custom | Custom | Custom | SLA-backed | SI/MSP/reseller partners |

## Discount Positioning

Using ShareGate's public starting prices and a rounded 94.2 INR/USD conversion:

| Comparison | ZMS price | ShareGate public start | Position |
| --- | ---: | ---: | --- |
| Starter vs Essentials | Rs 1.99L | about Rs 5.65L | about 65% lower |
| Professional vs Pro | Rs 3.99L | about Rs 9.42L | about 58% lower |
| Enterprise vs Enterprise | Rs 5.99L | about Rs 16.95L | about 65% lower |

Do not say "90% cheaper" or "2x the capability" in reviewer material. That is not defensible with the current evidence.

## Commercial Claim Boundary

Safe commercial claims after current proof:

- Lower-cost pilot and validation platform.
- Real Google Drive to SharePoint pilot evidence.
- Byte-level Microsoft Graph verification.
- Readiness, planning, validation, governance, AI-labeled recommendations, and reports.
- Customizable internal platform with owned code.

Do not claim yet:

- Full ShareGate replacement.
- Equivalent feature parity.
- Enterprise-scale certification.
- OneDrive, Teams, Exchange, or Box migration.
- Certified metadata writeback.
- Certified permission writeback.
- Version-history preservation.
- Empty-folder preservation.
- SLA-backed production reliability.

## Revenue Model

Conservative Year 1 model after launch readiness:

| Customer type | Count | Price | ARR |
| --- | ---: | ---: | ---: |
| Starter | 20 | Rs 1.99L | Rs 39.8L |
| Professional | 8 | Rs 3.99L | Rs 31.92L |
| Enterprise | 1 | Rs 5.99L | Rs 5.99L |
| Partner/White Label pilot | 2 | Rs 4.5L | Rs 9.0L |
| Total | 31 |  | Rs 86.71L |

This is a spreadsheet model, not a forecast. It depends on completing scale certification, empty folders, writeback, recovery, reports, monitoring, and commercial onboarding.

## Operating Cost Assumptions

Approximate yearly operating costs for early SaaS launch:

| Cost area | Estimate |
| --- | ---: |
| Vercel frontend | Rs 3K-5K/month |
| Render backend | Rs 10K-20K/month |
| Supabase/Postgres | Rs 2K-10K/month |
| Monitoring/logging | Rs 2K-10K/month |
| Miscellaneous domains/email/storage | Rs 2K-5K/month |
| Total infra estimate | Rs 25K-50K/month |

Support cost is the real margin risk. Plan for at least one support/implementation person once there are paying customers.

## Buyer Messaging

### Starter

> Prove your migration path before buying an enterprise migration stack. ZMS Starter gives small teams a controlled migration pilot, source-target validation, Microsoft Graph byte verification, and review-ready reports at a much lower entry cost.

### Professional

> Run department-level migration planning and validation with clear evidence. ZMS Professional is for teams that need more runs, more files, and stronger reporting without machine-activation friction.

### Enterprise

> Manage multi-wave Microsoft 365 migration readiness with governance, validation, reports, and support. Enterprise pricing is available only after scale certification and production hardening.

### Partner/White Label

> Use ZMS as a branded migration readiness and pilot platform for client engagements. Partner packaging should be offered only after the product has repeatable deployment, support, and tenant isolation processes.

## Pricing Page Copy

Use this copy only after the product reaches launch readiness.

### Headline

Microsoft 365 migration readiness without enterprise-tool pricing.

### Subheadline

Plan, validate, pilot, and report migrations with byte-verified evidence before scaling to full production waves.

### Starter Card

Starter - Rs 1.99 lakh/year

For small teams and controlled pilots.

- 10 migrations/year.
- Up to 10,000 files per run.
- 3 admin users.
- Google Drive to SharePoint pilot support after certification.
- Readiness, validation, and report exports.
- Email support, 48-hour response.

CTA: Start pilot

### Professional Card

Professional - Rs 3.99 lakh/year

For department-level migration teams.

- 50 migrations/year.
- Up to 100,000 files per run after scale certification.
- 8 admin users.
- Readiness, planning, validation, governance, AI-labeled recommendations, and reports.
- Email/Slack support, 24-hour response.

CTA: Talk to sales

### Enterprise Card

Enterprise - Rs 5.99 lakh/year

For multi-wave enterprise migration programs.

- Unlimited migrations after production certification.
- 1M+ file runs after enterprise-scale validation.
- 25+ admin users.
- Dedicated support channel.
- Custom report and connector roadmap options.

CTA: Request enterprise review

### Honest Comparison Copy

ZMS is not a full ShareGate replacement today. ShareGate remains the mature option for broad Microsoft 365 workload migration. ZMS is built for teams that want a lower-cost, customizable, evidence-first path for controlled migration pilots and readiness workflows.

## FAQ Copy

### Is ZMS a ShareGate replacement?

Not yet. ZMS currently has proven Google Drive to SharePoint migration evidence and a strong readiness/control-plane workflow. Full ShareGate parity requires scale certification, empty-folder preservation, metadata writeback, permission writeback, additional connectors, and recovery proof.

### Does ZMS support OneDrive, Teams, Exchange, or Box?

Not as certified migration workloads today. OneDrive is the recommended next connector. Teams, Exchange, and Box are roadmap items.

### Does ZMS preserve permissions and metadata?

ZMS analyzes permission and metadata risk today. Production writeback is not certified yet.

### Does ZMS support long paths?

ZMS has long-path discovery and risk handling foundations. Production path remediation and target write behavior must be validated at scale before launch claims.

### What happens when a trial expires?

Recommended behavior: existing evidence remains readable, but new migrations and exports are disabled until upgrade.

## Billing Integration Spec

Recommended stack:

- Stripe Checkout for subscriptions.
- Stripe Customer Portal for plan changes and cancellations.
- Supabase table for subscription state.
- Backend webhook endpoint for Stripe events.
- API authorization policy that checks active subscription and tier limits.

### Data Model

Suggested table: `subscriptions`

| Column | Purpose |
| --- | --- |
| `id` | Internal subscription ID |
| `user_id` | Supabase user/tenant owner |
| `stripe_customer_id` | Stripe customer |
| `stripe_subscription_id` | Stripe subscription |
| `tier` | trial/starter/professional/enterprise/partner |
| `status` | trialing/active/past_due/canceled/expired |
| `current_period_end` | Renewal/expiry date |
| `migration_limit_year` | Tier migration limit |
| `file_limit_per_run` | Tier file cap |
| `admin_user_limit` | Tier seat cap |
| `created_at` | Created timestamp |
| `updated_at` | Updated timestamp |

### Backend Endpoints

| Endpoint | Purpose |
| --- | --- |
| `POST /api/billing/checkout` | Create Stripe Checkout session |
| `POST /api/billing/portal` | Create Stripe Customer Portal session |
| `POST /api/billing/webhook` | Receive Stripe webhook events |
| `GET /api/billing/subscription` | Return current tenant subscription |
| `GET /api/auth/check-subscription` | Return feature/tier entitlement summary |

### Enforcement Rules

- Trial: max 100 files/run, no production connector write unless test tenant flag is enabled.
- Starter: max 10,000 files/run and 10 migrations/year.
- Professional: max 100,000 files/run and 50 migrations/year.
- Enterprise: limits configured by contract after certification.
- Expired/canceled: read-only mode; no new migrations; existing evidence remains accessible.

### Webhook Events

Handle at minimum:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_failed`
- `invoice.paid`

Security requirements:

- Verify Stripe webhook signature.
- Never trust frontend tier values.
- Store Stripe secret keys only in backend hosting environment variables.
- Do not put billing secrets in Vite/frontend env.

## Build Order

Do not build billing before the product claim is ready. Recommended order:

1. Rotate credentials.
2. Verify git merge state remains clean.
3. Keep npm audit clean after the Vite dependency upgrade.
4. Implement empty-folder preservation.
5. Run 1,000-file and 10,000-file migration certification.
6. Certify metadata and permission writeback for a controlled tenant.
7. Add report download/open verification.
8. Record demo video.
9. Add billing/subscription enforcement.
10. Publish pricing page.

## Sources Checked

- ShareGate homepage FAQ/pricing, checked 2026-06-18: https://sharegate.com/
- ShareGate pricing page, checked 2026-06-18: https://sharegate.com/pricing
- USD/INR reference, checked 2026-06-18: https://wise.com/us/currency-converter/usd-to-inr-rate/history
