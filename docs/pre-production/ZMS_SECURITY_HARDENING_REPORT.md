# ZMS Security Hardening Report

Status date: 2026-06-18

## Completed

- Backend normal startup no longer runs heavy schema initialization.
- Controlled schema initialization is gated by `ZMS_RUN_DB_SCHEMA_INIT`.
- Production default for schema initialization is false.
- `/api/status` uses a bounded schema readiness check.
- Security headers added to backend responses:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: no-referrer`
  - `Permissions-Policy`
  - basic API CSP
- API responses include `X-Correlation-ID`.
- API CORS allows the final Vercel origin.
- Lightweight API rate limiting is enabled.
- Frontend V2 avoids showing mock records as live data.
- Login page no longer shows build/debug fingerprint text or demo-like metric badges.
- Legacy reviewer pages now use clean empty states instead of raw labels or unlabeled fallback data.

## Final Security Results

| Check | Result |
| --- | --- |
| Secrets committed to Git | Not found in this pass |
| Backend secrets in frontend | Not added |
| Backend secrets in docs | Not added |
| Vercel backend secrets | Not added by this pass |
| Render backend secrets | Stored in Render only |
| CORS wildcard on backend | Not used |
| Auth guard for `/v2` | Covered by frontend tests and prior browser pass |
| Authenticated V2 walkthrough | Passed, 0 console errors |
| Security headers | Present on backend endpoints |
| Secrets shown on login page | Not found |
| Secrets shown in demo screenshots | Not found |

## Rotate Required

Previously pasted credentials remain `ROTATE REQUIRED` before broader company submission. Do not print or commit:

- Supabase DB password or pooler string.
- Google client secret or refresh token.
- Microsoft client secret.
- OAuth access or refresh tokens.
- Sentry DSN.

## Remaining Production Hardening

- Redeploy Render backend to subtree commit `53d6f08` so the empty-folder implementation is live.
- Rotate all previously exposed credentials before external company sharing.
- Add a repeatable authenticated E2E suite.
- Certify larger scale migrations before production claims.
