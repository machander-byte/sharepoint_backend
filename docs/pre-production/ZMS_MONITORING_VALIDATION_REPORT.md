# ZMS Monitoring Validation Report

Status date: 2026-06-09

## Current Result

Monitoring is implemented at a foundation level but not fully validated in this pass.

## Verified

- Sentry integration is wired in `Program.cs` when a DSN is configured.
- `/api/health`, `/api/status`, and `/api/version` endpoints exist.
- Audit logging middleware is registered.
- Audit log table creation exists for supported providers.
- Backend build and tests pass after security and long-path fixes.

## Current Blockers

- The backend API is not running on `localhost:5206`.
- The current shell does not contain backend-only environment variables.
- No Sentry DSN was verified.
- No controlled Sentry event was generated.
- Audit log records were not queried in this pass.

## Required Monitoring Tests

| Test | Expected |
| --- | --- |
| Health endpoint | Healthy response while backend is running |
| Status endpoint | Supabase/Postgres connection reported healthy |
| Version endpoint | Version payload returned |
| OAuth failure | Sentry captures connection/auth failure without secrets |
| Migration item failure | Sentry or structured logs capture redacted failure |
| Unhandled exception | Sentry event visible in project |
| Audit mutation | Create/update/test connection writes audit record |

## Submission Requirement

Before company submission, capture screenshots or redacted exports proving Sentry events, health checks, and audit logs are visible.
