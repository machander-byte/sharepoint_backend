# ZMS Demo Video Script

Status date: 2026-06-15

## Recording Status

Video recording was checked in the connected Playwright browser session. The active session does not expose a video recorder, so no video file was recorded in this pass.

Screenshots were captured instead and indexed in `docs/pre-production/ZMS_DEMO_SCREENSHOTS_INDEX.md`.

## Two-Minute Narration Script

1. Open `https://zms-migration-suite.vercel.app`.
   "This is the Zettalogix Migration Suite final demo / pre-production review build."

2. Show the login screen.
   "The login page is clean and reviewer-focused. It does not show secrets, backend URLs, debug values, or mock counters."

3. Log in with the reviewer account shared separately.
   "Authentication uses the configured Supabase/Google flow. Credentials are shared outside the recording."

4. Open the dashboard or `/v2`.
   "The command center shows the migration workspace and live backend status without treating fallback data as real data."

5. Open Live Migrations.
   "The migration monitor now uses polished queue cards, clear actions, and a professional empty state when no jobs are present."

6. Open Validation.
   "Validation now shows Status, Passed, Warnings, and Failed as clear cards, with styled findings and item comparison tables."

7. Open Reports.
   "Reports load without optional backend 404 noise. Disabled exports explain that data must exist first."

8. Open AI Advisor and Copilot Readiness.
   "AI and governance readiness pages show safe empty states until discovery and readiness analysis are available."

9. Open Tutorial.
   "The tutorial gives reviewers a guided path through the demo without requiring destructive actions."

10. Mention backend health.
    "Backend `/api/version`, `/api/health`, and `/api/status` are all returning 200, and status is Healthy."

## Reviewer Instructions

- Use the reviewer credentials shared separately.
- Do not show passwords, tokens, Render environment variables, Supabase database settings, Google secrets, or Microsoft secrets.
- Do not run Stage 2 migration.
- Do not delete data.
- Use only safe navigation and read-only review actions unless explicitly approved.

## Known Limitations To Mention

- Empty-folder preservation is not complete.
- 1,000-file certification is pending.
- Subscription is not implemented.
- Additional connectors remain roadmap items.
- This is final demo / pre-production review, not full production certification.
