# Security And Submission Checklist

Use this before sharing ZMS with company reviewers.

## Secrets

Confirm no secret values are stored in:

- GitHub repository.
- Frontend source.
- Frontend `.env`.
- Generated reports.
- Screenshots.

Allowed frontend values:

- `VITE_API_BASE_URL`
- `VITE_SUPABASE_URL`
- `VITE_SUPABASE_PUBLISHABLE_KEY`
- Google Picker browser client ID, API key, and app ID if key restrictions are configured.

Backend-only secrets:

- `ConnectionStrings__ZmsDatabase`
- `GOOGLE_CLIENT_SECRET`
- `GOOGLE_REFRESH_TOKEN`
- Microsoft client secrets saved through backend connection profiles.
- `Sentry__Dsn`

Current 2026-06-09 finding:

- ROTATE REQUIRED: backend-only credentials and tokens were pasted during validation. Rotate them before company submission.
- Repo scan by file path found no backend secret-value prefixes in source files, but generated logs/artifacts and frontend-public keys were detected. Do not commit or share `.playwright-mcp`, local logs, `.vercel/output`, or local `.env` files as evidence unless reviewed and redacted.
- `ZMS.WebUI/.env` contains browser-public Vite values only; do not add backend secrets there.
- Local API startup was restored by setting backend-only values in ASP.NET user-secrets. Keep those values out of frontend `.env`, docs, screenshots, and commits.

## Supabase

Confirm:

- Backend uses `Database__Provider=Postgres`.
- `ConnectionStrings__ZmsDatabase` uses the Supabase pooler.
- Data Protection keys persist to database.
- Supabase JWT validation is enabled.
- Role claims are defined before enabling `Authorization__EnforceRoles=true`.
- Audit logs are written for mutating API calls.

Current status:

- Supabase/Postgres was proven during the 22-file live migration.
- Supabase/Postgres was proven again during the 231-file Stage 1 live migration on 2026-06-09.
- Data Protection database key storage is configured.
- Audit log code and table creation exist, but audit records were not separately queried during the 231-file Stage 1 pass.

## OAuth And Access

Confirm:

- Vercel production origin is registered in Supabase redirect URLs.
- Google Drive API is enabled.
- Google source folder is shared with the refresh-token account.
- Microsoft Graph application permissions have admin consent.
- SharePoint target library exists or ZMS can create it in the test tenant.

Current status:

- Google Drive -> SharePoint Online was validated for 22 files on 2026-06-08.
- Google Drive Folder B -> SharePoint Online was validated for 231 files on 2026-06-09.
- 1,000/10,000-file live stages are still pending.
- OneDrive remains unvalidated.

## Reports

Generate and open:

- Discovery Inventory CSV.
- Permission Risk CSV.
- Migration Risk CSV.
- Readiness Report.
- Migration Plan CSV.
- Migration Runbook Markdown.
- Go/No-Go Validation Report.
- Execution Job Report.
- Transfer Preview Report.

Each export must open in Excel, a text editor, or markdown preview without broken encoding.

## Error Recovery

Test one controlled interruption:

1. Start a small live migration.
2. Stop or restart the API while an item is in progress.
3. Restart the API.
4. Confirm running items are returned to retry queue.
5. Resume or let the queue recover.
6. Confirm no duplicate completed files are created.

## Monitoring

Trigger and capture these Sentry events:

- API unhandled exception in a non-production test route or controlled failure.
- OAuth/connection failure.
- Migration item failure.
- Backend request timeout or remote provider error.

## Submission Decision

Mark the product ready for company review only when:

- Backend tests pass.
- Frontend build passes.
- Live Google Drive -> SharePoint Online migration passes at Stage 1, 1,000, and 10,000 files.
- Live SharePoint Online -> SharePoint Online migration passes at 100, 1,000, and 10,000 files.
- Supabase audit and job records are visible.
- No secrets are present in source or frontend artifacts.
- OneDrive is either implemented and validated or explicitly listed as out of scope for this submission.

## 2026-06-13 Deployment Security Addendum

- Render backend is failing because PostgreSQL authentication is rejected for the configured database user. The Render connection string now has the expected Supabase pooler host, port `6543`, and scoped user shape, but the database password is not accepted.
- ROTATE REQUIRED: the Supabase database password was pasted during deployment work and must be treated as exposed before company submission. Do not reuse it as the final company-review credential.
- ROTATE REQUIRED: a backend connection string was visible in tool/browser output during deployment troubleshooting. Do not share those logs or screenshots externally.
- Supabase Advisor shows public-table RLS findings. Startup hardening now enables RLS for ZMS public tables after schema creation and migrations; verify the Advisor result after backend redeploy.
- `npm audit` reports 6 frontend dependency findings: 5 moderate and 1 high. The high finding is in the Vite/esbuild dependency chain and requires a dependency upgrade plan.
- A conservative secret-pattern scan reported only placeholders, test redaction strings, or documented variable names by path/line; no secret values were printed in this report.
- `GOOGLE_CLIENT_SECRET`, `GOOGLE_REFRESH_TOKEN`, Microsoft client secret, and Supabase database password remain ROTATE REQUIRED before company submission.
