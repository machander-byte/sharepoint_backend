# ZMS Supabase Auth Deployment Report

Status date: 2026-06-13

## Supabase Project

| Item | Result |
| --- | --- |
| Project ref | `hxptmbphcdyzhmwnimwh` |
| Project URL | `https://hxptmbphcdyzhmwnimwh.supabase.co` |
| Dashboard status | Healthy |
| Region | South Asia (Mumbai) |
| Frontend Site URL | `https://zms-migration-suite.vercel.app` |

## Auth URL Configuration

Verified redirect allow-list entries visible in the Supabase dashboard:

- `http://localhost:5173/*`
- `http://127.0.0.1:5173/auth/callback`
- `http://localhost:5173/auth/callback`
- `https://sharepoint-one.vercel.app/`
- `https://sharepoint-one.vercel.app/auth/callback`
- `https://zms-migration-suite.vercel.app/auth/callback`

## Browser Auth Result

Supabase browser auth completed during this pass and the deployed frontend opened an authenticated route. The deployed backend API could not be validated because Render is failing to start.

## Security Findings

Supabase Advisor showed 19 issues, including critical RLS-disabled findings on public tables such as `DataProtectionKeys`, `DiscoveryRuns`, `DiscoveredSites`, and `DiscoveredWebs`.

Code hardening was added so backend startup enables RLS on the ZMS public tables after schema creation and migrations. This must be verified after Render database authentication is fixed and the backend starts.

## Blockers

- Render `ConnectionStrings__ZmsDatabase` is invalid or stale and must be updated/rotated.
- Backend protected API calls were not validated from the deployed frontend.
- Audit table records were not queried in this pass.

## Decision

Supabase Auth URL configuration is ready for the current Vercel frontend URL, but full Supabase deployment validation is blocked by the Render database authentication failure.
