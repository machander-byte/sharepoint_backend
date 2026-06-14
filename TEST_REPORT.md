# ZMS Test Report

Generated: 2026-06-14

## Final Demo Summary

ZMS is deployed as a final project / pre-production review demo.

- Backend build passed: 0 warnings, 0 errors.
- Backend tests passed: 46/46.
- Frontend build passed.
- Render backend is live and healthy at `https://sharepoint-backend-g5vc.onrender.com`.
- Vercel frontend is live at `https://zms-migration-suite.vercel.app`.
- Supabase/Google login was verified in the browser.
- Authenticated `/v2` and the requested V2 pages loaded with no browser console errors.
- CORS preflight from `https://zms-migration-suite.vercel.app` to the Render backend passed.

## Deployed Versions

| Area | Result |
| --- | --- |
| Full repo latest commit | `694069a` |
| Backend source latest commit | `669aba1` |
| Render backend subtree commit | `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| Frontend build fingerprint | `694069a` |
| Frontend JS asset | `index-B9Vkb8MT.js` |

## Final Backend Endpoint Results

| Endpoint | Result |
| --- | --- |
| `/api/version` | 200 OK, commit `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| `/api/health` | 200 OK, `Healthy`, DB connected, schema ready |
| `/api/status` | 200 OK, `Healthy`, schema `Ready`, queue empty |

## Final Frontend Smoke

| Check | Result |
| --- | --- |
| `/login` | 200 OK; latest V2 login bundle contains fingerprint `694069a` |
| Unauthenticated `/v2` | Redirects to `/login` |
| Unauthenticated `/v2/tutorial` | Redirects to `/login` |
| Unauthenticated `/v2/monitor` | Redirects to `/login` |
| Login | Google/Supabase login succeeded in browser |
| Authenticated `/v2` | Loaded and showed backend runtime `Healthy` |
| Browser console | 0 errors after final V2 walkthrough |
| CORS | Passed for final frontend origin |

## V2 Pages Tested

- `/v2`
- `/v2/command-center`
- `/v2/sources`
- `/v2/destinations`
- `/v2/assess`
- `/v2/plan`
- `/v2/migrate`
- `/v2/monitor`
- `/v2/validate`
- `/v2/reports`
- `/v2/ai-advisor`
- `/v2/governance`
- `/v2/settings`
- `/v2/tutorial`

All pages loaded, were not blank, showed no subscription/billing UI, and had no console errors in the authenticated browser pass.

## Verification Commands Run

```powershell
Set-Location "d:\projects\Shearpoint to google\sharepoint_backend"
dotnet build .\Zettalogix.MigrationSuite.sln
dotnet test .\Zettalogix.MigrationSuite.sln --no-build

Set-Location "d:\projects\Shearpoint to google\ZettalogixMigrationSuite\ZMS.WebUI"
npm run build
npx vercel deploy --prod --yes -b VITE_API_BASE_URL=https://sharepoint-backend-g5vc.onrender.com -b VITE_APP_COMMIT=694069a
```

## Known Warnings

- Vite chunk-size warning remains present and accepted for this demo.
- Vercel `npm ci` reports 6 dependency audit findings: 5 moderate, 1 high. These need a dependency upgrade pass before a production release.

## Known Limitations

- Empty-folder preservation is not complete as first-class SharePoint folder migration.
- Stage 2 1,000-file migration is pending.
- Stage 3 10,000-file migration is pending.
- Subscription/payment is not implemented.
- OneDrive, Teams, Exchange, and Box are roadmap items.
- Permission writeback is not certified.
- Metadata writeback is not certified.
- Full ShareGate parity is not claimed.
- Production-scale certification is not claimed.

## Security Note

No secret values were added to Git, frontend source, or docs in this pass. Previously pasted credentials remain marked `ROTATE REQUIRED` before broader company submission.
