# ZMS Error Handling Report

Status date: 2026-06-14

## Backend

- Global exception handler returns structured JSON.
- Production responses do not expose stack traces.
- Error responses include request ID and timestamp.
- Request logging includes method, path, status, elapsed time, and correlation scope.
- Audit middleware continues to record mutating API actions without logging request bodies or secrets.

Example backend error shape:

```json
{
  "error": "Something went wrong",
  "code": "ZMS_INTERNAL_ERROR",
  "requestId": "correlation-id",
  "timestamp": "2026-06-14T00:00:00Z"
}
```

## Frontend

- Added global `ErrorBoundary`.
- Runtime fallback screen has a retry action.
- V2 runtime adapter shows healthy/degraded backend state without blocking on optional domain data endpoints.
- V2 does not show mock records as live records.
- Final authenticated V2 walkthrough had 0 browser console errors.

## Remaining Work

- Add committed frontend route/component tests.
- Add Playwright E2E tests for login and the V2 reviewer path.
- Add controlled Sentry capture tests before production release.
