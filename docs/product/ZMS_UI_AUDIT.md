# ZMS UI Audit

Status date: 2026-06-09

## Smoke Result

- Frontend production build passed.
- Local app loaded to `/dashboard` with an existing session.
- No blank page was observed.
- React Router future-flag warnings are present.
- Backend API calls currently show `ERR_CONNECTION_REFUSED` because `localhost:5206` is offline in this shell.

## High-Impact Findings

| Area | Severity | Finding | Recommendation |
| --- | --- | --- | --- |
| Offline backend UX | High | Dashboard still emits repeated browser console errors when API is unavailable | Add a global API status banner and avoid repeated polling bursts while offline |
| Live migration evidence | High | Migration ledger screenshots are useful, but evidence is spread across docs/screenshots | Add an Evidence Center page for completed jobs |
| Live migrations table | Medium | Dense job rows can become hard to scan with IDs, paths, and status columns | Add compact row expansion and horizontal overflow treatment |
| AI recommendations | Medium | Some actions are buttons/alerts without a completed workflow | Replace placeholder actions with disabled states or real plan-edit flows |
| Bundle size | Medium | Vite reports one JS chunk around 788 kB | Add route-level lazy loading/manual chunks |
| Error language | Low | API-down errors are technically accurate but not company-review friendly | Convert into “Backend unavailable, using local fallback where possible” copy |

## Current Good Points

- Authenticated shell and navigation load cleanly.
- Pages use a consistent app shell and controls.
- Connections and migration flows have clear task-oriented screens.
- Build output confirms the UI can be packaged for production.

## Immediate UI Fix Candidates

1. Add global backend status/connection health indicator.
2. Add route lazy loading.
3. Improve live migration ledger responsive table behavior.
4. Add an Evidence Center for live migration results and exports.
5. Convert placeholder AI action buttons into clear disabled/planned states.
