# ZMS AI Feature Inventory

Status date: 2026-06-09

## Implemented AI-Adjacent Features

| Feature | Location | Status |
| --- | --- | --- |
| AI advisor ask endpoint | `POST /api/ai/advisor/ask` | Implemented, protected by Viewer policy |
| Discovery remediation | `GET /api/ai/remediation/discovery/{discoveryRunId}` | Implemented, protected by Viewer policy after this pass |
| Migration remediation | `GET /api/ai/remediation/migration/{jobId}` | Implemented, protected by Viewer policy |
| Validation remediation | `GET /api/ai/remediation/validation/{validationRunId}` | Implemented, protected by Viewer policy |
| Migration ETA | `GET /api/migrations/{jobId}/eta` | Implemented, protected by Viewer policy |
| Discovery ETA | `GET /api/discovery/{runId}/eta-estimate` | Implemented, protected by Viewer policy after this pass |
| AI Recommendations UI | `/ai` | Implemented as a read-only recommendations dashboard |
| Copilot readiness | `/copilot-readiness`, `api/copilot-readiness/*` | Implemented as readiness analysis, not an automated Microsoft 365 Copilot deployment |
| Modernization recommendations | `api/modernization/*` | Implemented as recommendation/demo workflow |

## Model Behavior

The backend uses `AiAdvisorService` with:

- Platform-context-only prompting.
- Secret redaction before prompts are sent to the model.
- Ollama support through `OLLAMA_BASE_URL` and `OLLAMA_MODEL`.
- Deterministic fallback guidance when Ollama is unavailable.

Default Ollama settings:

```text
Base URL: http://localhost:11434
Model: llama3.1
```

## Safety Boundaries

- AI does not execute migrations.
- AI does not modify tenant data.
- AI does not receive raw secrets by design; prompts and context are redacted.
- AI answers are bounded to discovery, migration, and validation context already in ZMS.

## Current Gaps

- No live Ollama model was verified in this pass.
- No prompt regression suite is committed.
- The UI does not yet expose the free-form advisor ask workflow; it focuses on recommendations and ETA cards.
- Recommendations are advisory and should not be represented as autonomous remediation.

## Submission Language

Use:

```text
ZMS includes AI-assisted migration recommendations, ETA estimation, remediation suggestions, and optional local Ollama advisor support with deterministic fallback behavior.
```

Do not use:

```text
ZMS automatically fixes migration risks with AI.
```
