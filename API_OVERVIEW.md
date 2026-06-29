# ZMS API Overview

Base path: `/api`

Authentication: all API controllers are protected by default with Supabase JWT bearer authentication. Anonymous endpoints are `/`, `/api/health`, `/api/version`, and `/api/status`. Mutating operations generally require the `Operator` policy; admin-only destructive actions require the `Admin` policy when role enforcement is enabled.

## Health And Version

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/` | GET | Anonymous | Basic service liveness. |
| `/api/health` | GET | Anonymous | Dependency, schema, startup, and queue health. |
| `/api/status` | GET | Anonymous | Deployment status with 200 or 503 status code. |
| `/api/version` | GET | Anonymous | Service version, environment, commit/build metadata. |

## Connections

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/connections` | GET | Viewer | List current user's connection profiles. |
| `/api/connections` | POST | Operator | Create source or target connection. |
| `/api/connections/{connectionId}` | PUT | Operator | Update connection profile. |
| `/api/connections/{connectionId}/test` | POST | Operator | Test connector health. |
| `/api/connections/{connectionId}` | DELETE | Admin | Delete connection profile. |

Connection responses include redacted capability flags such as `hasClientSecret` and `hasRefreshToken`, not raw secret values.

## Discovery And Readiness

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/discovery/start` | POST | Operator | Start source discovery. |
| `/api/discovery/{scanId}/results` | GET | Viewer | Fetch discovery result. |
| `/api/discovery/{scanId}/inventory` | GET | Viewer | Fetch inventory graph/data. |
| `/api/discovery/{scanId}/permissions` | GET | Viewer | Fetch permission findings. |
| `/api/discovery/{scanId}/metadata` | GET | Viewer | Fetch metadata findings. |
| `/api/discovery/{scanId}/risks` | GET | Viewer | Fetch migration risk findings. |
| `/api/discovery/{scanId}/export/csv` | GET | Viewer | Export discovery CSV. |
| `/api/readiness/analyze/{scanId}` | POST | Operator | Create readiness assessment. |
| `/api/readiness/{assessmentId}` | GET | Viewer | Fetch readiness score and summary. |
| `/api/readiness/{assessmentId}/remediation-plan` | GET | Viewer | Fetch remediation actions. |
| `/api/readiness/{assessmentId}/migration-waves` | GET | Viewer | Fetch suggested waves. |

## Planning And Pre-Migration

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/migration-plans/from-assessment/{assessmentId}` | POST | Operator | Build migration plan from readiness data. |
| `/api/migration-plans/{planId}` | GET | Viewer | Fetch plan, waves, checklist, risks. |
| `/api/migration-plans/{planId}` | PUT | Operator | Update plan details. |
| `/api/migration-plans/{planId}/validate` | POST | Operator | Validate plan readiness. |
| `/api/migration-plans/{planId}/generate-runbook` | POST | Operator | Generate runbook. |
| `/api/pre-migration/validate/{planId}` | POST | Operator | Run pre-migration safety gate. |
| `/api/pre-migration/simulate/{planId}` | POST | Operator | Simulate execution. |

## Execution Jobs

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/jobs` | GET | Viewer | List migration jobs. |
| `/api/jobs/{jobId}` | GET | Viewer | Fetch job details. |
| `/api/jobs/{jobId}/items` | GET | Viewer | Fetch job items. |
| `/api/jobs` | POST | Operator | Create migration job. |
| `/api/jobs/{jobId}/start` | POST | Operator | Start job. |
| `/api/jobs/{jobId}/pause` | POST | Operator | Pause job safely. |
| `/api/jobs/{jobId}/resume` | POST | Operator | Resume job. |
| `/api/jobs/{jobId}/cancel` | POST | Operator | Cancel job. |
| `/api/jobs/{jobId}/retry` | POST | Operator | Retry failed or cancelled work. |
| `/api/jobs/{jobId}/timeline` | GET | Viewer | Fetch state transition timeline. |

## Validation And Reports

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/validation/start` | POST | Operator | Start validation for a job. |
| `/api/validation/{validationRunId}` | GET | Viewer | Fetch validation run. |
| `/api/validation/{validationRunId}/findings` | GET | Viewer | Fetch validation findings. |
| `/api/validation/{validationRunId}/items` | GET | Viewer | Fetch item-level results. |
| `/api/validation/{validationRunId}/export/{exportType}` | GET | Viewer | Export validation data. |
| `/api/reports/jobs.csv` | GET | Viewer | Export job list CSV. |
| `/api/reports/jobs/{jobId}/summary.csv` | GET | Viewer | Export job summary CSV. |
| `/api/reports/jobs/{jobId}/items.csv` | GET | Viewer | Export job items CSV. |
| `/api/reports/jobs/{jobId}/logs.csv` | GET | Viewer | Export job logs CSV. |

## AI, Governance, And Extended Foundations

| Endpoint | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/ai/advisor/ask` | POST | Viewer | Ask AI advisor using redacted platform context. |
| `/api/ai/remediation/discovery/{discoveryRunId}` | GET | Viewer | Discovery remediation suggestions. |
| `/api/ai/remediation/migration/{jobId}` | GET | Viewer | Migration remediation suggestions. |
| `/api/copilot-readiness/{discoveryRunId}` | GET | Viewer | Copilot readiness analysis. |
| `/api/onprem/discovery/import` | POST | Operator | Import SharePoint On-Prem discovery foundation data. |
| `/api/teams/discovery/start` | POST | Operator | Start Teams discovery foundation flow. |
| `/api/sharepoint-migration/preview/from-job/{jobId}` | POST | Operator | Generate guarded transfer preview. |
| `/api/workflow-validation/run-full-chain` | POST | Operator | Run end-to-end workflow validation chain. |

## Error Format

Unhandled backend failures return a safe JSON shape without stack traces:

```json
{
  "error": "Something went wrong",
  "code": "ZMS_INTERNAL_ERROR",
  "requestId": "trace-id",
  "timestamp": "2026-06-22T00:00:00Z"
}
```

Model validation errors use ASP.NET Core problem details with field-level errors. Frontend code formats these into user-facing messages without exposing secrets.
