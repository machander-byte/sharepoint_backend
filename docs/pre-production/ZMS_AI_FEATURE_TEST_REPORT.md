# ZMS AI Feature Test Report

Status date: 2026-06-09

## Results

| Test | Result | Notes |
| --- | --- | --- |
| Backend compile | Passed | AI services/controllers compile in solution build |
| Backend automated tests | Passed | Full suite passed: 46/46 after this pass |
| Secret redaction coverage | Passed by existing tests | `SecretRedactorTests` are part of the backend suite |
| AI endpoint authorization review | Fixed | Discovery remediation and discovery ETA were moved behind Viewer policy |
| Frontend AI page compile | Passed | Included in `npm run build` |
| Frontend AI page live smoke | Partial | App loads, but backend API is offline in the current shell |
| Ollama live model call | Not run | No local Ollama service was verified in this pass |

## Verified Behaviors By Code Review

- Advisor requests are redacted before Ollama prompt construction.
- Platform context is summarized before model use.
- If no discovery, migration, or validation context exists, the advisor returns a non-fabricated response asking for required data.
- If Ollama is unavailable, the service returns deterministic fallback guidance.

## Blockers

- `localhost:5206` is not running because backend-only environment variables are not present in the current shell.
- No Ollama runtime is confirmed locally.

## Required Next Tests

1. Start backend with Supabase, Google, and Microsoft backend-only variables.
2. Start Ollama or explicitly test deterministic fallback mode.
3. Call `POST /api/ai/advisor/ask` with an authenticated Supabase JWT.
4. Confirm no secrets are present in prompt logs or responses.
5. Save request/response samples with all IDs and tokens redacted.
