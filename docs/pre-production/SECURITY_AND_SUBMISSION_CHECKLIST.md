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
- RESOLVED 2026-06-29: the exposed Supabase database password was replaced with a newly generated credential and Render was updated without committing the value.
- RESOLVED 2026-06-29: the old backend connection string is invalid after password rotation. A local browser snapshot that contained the old value was removed and remains excluded by `.gitignore`.
- Supabase Advisor shows public-table RLS findings. Startup hardening now enables RLS for ZMS public tables after schema creation and migrations; verify the Advisor result after backend redeploy.
- `npm audit` previously reported frontend dependency findings in the Vite/esbuild dependency chain. The upgraded lockfile was deployed to Vercel on 2026-06-29 and `npm audit` reports 0 vulnerabilities.
- A conservative secret-pattern scan reported only placeholders, test redaction strings, or documented variable names by path/line; no secret values were printed in this report.
- The Supabase database password was rotated on 2026-06-29. Google and Microsoft provider credentials must still be rotated before they are enabled for a broader production audience; they are not included in the repository or reviewer package.

## 2026-06-14 Final Demo Security Addendum

- Backend normal startup no longer runs heavy schema initialization.
- Controlled database schema initialization is gated by `ZMS_RUN_DB_SCHEMA_INIT`; production default is false.
- Render backend reports `/api/status = 200 Healthy`.
- Backend security headers are present on API responses.
- Backend CORS preflight passed for `https://zms-migration-suite.vercel.app`.
- Vercel final bundle contains the public Render backend URL.
- Authenticated `/v2` browser walkthrough completed with 0 console errors.
- No secret values were added to source, frontend code, docs, or reports in this pass.
- The database credential was rotated on 2026-06-29. Provider-specific Google/Microsoft credentials remain subject to rotation before broader production enablement.
- Reviewer credentials must be shared separately and must not be written into Git, docs, screenshots, or chat transcripts.

## 2026-06-15 UI Polish And Demo Addendum

- Login page was cleaned for reviewer use and does not show passwords, tokens, backend URLs, or build/debug values.
- Legacy reviewer routes `/migrations`, `/validation`, and `/copilot-readiness` were polished to remove raw concatenated labels.
- Dashboard, Reports, AI, and Copilot pages no longer auto-probe unsupported optional latest-record endpoints on load.
- Vercel final bundle contains the public Render backend URL. The latest production redeploy was completed on 2026-06-18.
- Authenticated reviewer walkthrough completed with 0 console errors. Failed network request count was not directly exposed by the available Playwright tool during the 2026-06-18 pass.
- Demo screenshots were captured and committed under `docs/pre-production/screenshots`.
- Demo video was not recorded because the connected Playwright browser session did not expose video recording.

## 2026-06-18 Push, Deploy, And Empty-Folder Addendum

- Latest frontend app source commit `8afdb8e9cc1817f804a81710aa1ab51b88fca907` was pushed to `origin/master` and deployed to Vercel.
- Latest backend subtree commit `53d6f082c3b1e9618c0e59a4eac54d3a26761a92` was pushed to `origin/main`.
- Superseded on 2026-06-29: Render now reports live backend commit `03573c7911a9d875d61f98285e2442592692fcde`, which includes the empty-folder implementation and release hardening.
- Vercel production deployment passed and the public alias serves the latest frontend bundle with the Render API base URL.
- Local backend tests pass 49/49, frontend tests pass 6/6, frontend lint and build pass, and npm audit reports 0 vulnerabilities.
- Authenticated V2 browser walkthrough completed with 0 console errors.
- The Render redeploy blocker is resolved. Live empty-folder certification still requires an approved safe source/target run.

## 2026-06-29 Submission Readiness Addendum

- Supabase was resumed from its paused state.
- The Supabase database password was rotated and the Render connection string was updated without storing the credential in source control.
- Render backend commit `03573c7911a9d875d61f98285e2442592692fcde` is live.
- Hosted `/api/status` returns `200 Healthy`; PostgreSQL is connected and the required schema is ready.
- Vercel production was redeployed and `https://zms-migration-suite.vercel.app` points to the new deployment.
- Frontend CSP, frame protection, content-type protection, referrer policy, permissions policy, and OAuth-compatible opener policy are present.
- Production Google OAuth completed and opened the authenticated dashboard.
- Local validation passed: ESLint, 6 frontend tests, frontend production build, npm audit, backend Release build, 49 backend tests, backend publish, and NuGet vulnerability scan.
- This is approved for controlled project submission as a pre-production demo. It is not a market-launch or production-scale certification.
