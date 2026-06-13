# ZMS UI V2 Integration Plan

Status date: 2026-06-10

## Source ZIP

UI export inspected:

`C:\Users\JASHWANTH\Downloads\Complete task using resources.zip`

The ZIP extracted successfully to a temporary local folder during this pass. The export is a React/Vite concept with:

- `src/app/App.tsx`
- `src/app/components/Sidebar.tsx`
- `src/app/components/TopBar.tsx`
- Page components for Command Center, Sources, Destinations, Assess, Plan, Migrate, Monitor, Validate, Reports, AI Advisor, Governance, and Settings
- `src/styles/theme.css`
- A large shadcn/Radix-style UI component folder

## Export Inspection

| Area | Finding |
| --- | --- |
| Framework | React + Vite |
| Styling | Tailwind/global theme export with dark ZMS tokens |
| Primary page dependencies | Mostly `lucide-react` and inline styles |
| Heavy listed dependencies | MUI, Emotion, motion, Recharts, Sonner, many Radix packages, React Router 7 |
| Current app compatibility | Current app already uses React 18, Vite 5, Tailwind 4, React Router 6, and `lucide-react` |
| Dependency conflict risk | Export lists React Router 7 and Vite 6; current app stays on React Router 6 and Vite 5 |

## Integration Decision

The export was adapted rather than copied blindly.

No new dependencies were added. The V2 implementation uses dependencies already present in the current frontend, primarily `lucide-react`, React, and existing Vite/Tailwind infrastructure.

This avoids pulling in unused heavy libraries from the export package:

- `@mui/*`
- `@emotion/*`
- `motion`
- `recharts`
- `sonner`
- `react-router` 7
- additional Radix packages not required by the adapted V2 screens

## Route Added

Route path:

`/v2/*`

Implementation:

- Added `src/ui-v2/V2App.tsx`.
- Mounted `/v2/*` inside the existing `RequireAuth` guard.
- Mounted `/v2/*` outside the existing production `AppLayout`, so V2 uses its own sidebar and topbar.
- Added subpath mapping for V2 pages, for example `/v2/monitor`.
- Updated the existing `/login` screen to match the dark UI V2 premium design while preserving the current Supabase auth flow.
- Existing routes such as `/dashboard`, `/connections`, `/discovery`, `/planner`, `/jobs`, `/reports`, `/ai`, and `/settings` remain unchanged.

Unauthenticated browser smoke result:

- Navigating to `/v2` redirects to `/login`, matching the current app's protected-route behavior.
- Navigating directly to `/v2/monitor` also redirects to the redesigned `/login` screen when unauthenticated.

## Files Added

| Path | Purpose |
| --- | --- |
| `src/ui-v2/V2App.tsx` | V2 shell, page switching, optional read-only runtime health fetch |
| `src/ui-v2/components/V2Sidebar.tsx` | V2 sidebar navigation |
| `src/ui-v2/components/V2TopBar.tsx` | V2 topbar and runtime status pills |
| `src/ui-v2/components/V2Primitives.tsx` | Shared cards, tables, headers, status pills, limitation banner |
| `src/ui-v2/data/v2DashboardData.ts` | Adapter data using current ZMS validation evidence |
| `src/ui-v2/data/v2ReadOnlyAdapter.ts` | Read-only API adapter with fallback data |
| `src/ui-v2/styles/v2-theme.css` | Scoped dark premium V2 theme under `.zms-v2-root` |
| `src/ui-v2/pages/*.tsx` | V2 page components |

## Pages Integrated

- Command Center
- Sources
- Destinations
- Assess
- Plan
- Migrate
- Monitor
- Validate
- Reports
- AI Advisor
- Governance
- Settings

## Data Source

Current V2 data source:

- Adapter data from `src/ui-v2/data/v2DashboardData.ts`
- Read-only API/fallback orchestration from `src/ui-v2/data/v2ReadOnlyAdapter.ts`
- Anonymous runtime endpoints:
  - `/api/health`
  - `/api/status`
  - `/api/version`
- Existing authenticated-safe frontend service calls with fallback:
  - `zmsApi.getConnections()`
  - `zmsApi.getLatestMigrationExecutionJob()`
  - `zmsApi.getLatestReadinessAssessment()`
  - `zmsApi.getLatestMigrationPlan()`
  - `zmsApi.getLatestWorkflowValidation()`
  - `zmsApi.getReports()`
  - `zmsApi.getAIRecommendations()`

The V2 adapter uses current verified facts:

- Stage 0 Google Drive -> SharePoint: 22/22 files passed
- Stage 1 Google Drive -> SharePoint: 231/231 files passed
- Failed files: 0
- Retries: 0
- Source bytes: 2,589,962
- Target bytes verified by Microsoft Graph: 2,589,962
- Backend tests: 46/46 passed
- Frontend build: passed
- Queue: empty
- Supabase/Postgres: connected during live validation

## Claim Boundaries

V2 intentionally shows:

`File migration integrity passed. Empty folders are not yet migrated as first-class objects.`

V2 does not claim:

- Full production readiness
- Full ShareGate parity
- Empty-folder preservation
- Subscription, billing, upgrade plans, or paid-tier features

V2 uses "internal safety limits" language instead of subscription language.

## Styling Isolation

The export's global `:root`, `body`, and Tailwind base styles were not imported directly.

V2 styling is scoped under:

`.zms-v2-root`

This prevents V2 theme tokens from restyling the existing production UI.

## Build And Test Result

| Check | Result |
| --- | --- |
| `npm run build` | Passed |
| `dotnet build .\Zettalogix.MigrationSuite.sln` | Passed, 0 warnings, 0 errors |
| `dotnet test .\Zettalogix.MigrationSuite.sln --no-build` | Passed, 46/46 |

Known frontend build warning:

- Vite still reports a JS chunk larger than 500 kB. This existed before V2 and increased slightly after adding V2. Route-level lazy loading/manual chunks should be considered next.

## Known UI V2 Limitations

- V2 now supports page subpaths such as `/v2/monitor`, but page switching still uses the V2 shell state internally.
- V2 uses read-only APIs where available and keeps adapter fallback data when APIs are unavailable.
- Authenticated browser rendering of the V2 shell requires a real Supabase session; this pass verified route protection, login layout, and build integrity, not a real-login page-by-page walkthrough.
- No committed Playwright/Vitest route test exists yet for V2.
- Mobile layout is responsive by CSS, but full visual QA across all V2 pages still needs a real authenticated browser session.

## Next API Wiring Steps

1. Run authenticated browser smoke coverage for `/v2` and each V2 subpath with a real Supabase session.
2. Add more authenticated read-only fields for latest validation result, discovery, audit summary, and connection status where endpoint coverage exists.
3. Add route-level lazy loading for V2 pages to reduce the main JS chunk.
4. Keep current production UI as the default until V2 has authenticated QA coverage.
