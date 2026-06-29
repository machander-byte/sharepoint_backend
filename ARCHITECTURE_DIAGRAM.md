# ZMS Architecture Diagram

## High-Level Architecture

```mermaid
flowchart LR
    User[Migration Lead] --> UI[React/Vite Web UI]
    UI --> Auth[Supabase Auth]
    UI --> API[ASP.NET Core API]

    API --> App[Application Services]
    API --> DB[(EF Core Database)]
    API --> Reports[Report Export Services]

    App --> Discovery[Discovery Services]
    App --> Readiness[Readiness And Risk Engines]
    App --> Planner[Migration Planner]
    App --> Validation[Pre/Post Validation]
    App --> Simulation[Execution Simulation]
    App --> Package[Environment Package Generator]
    App --> AI[AI Advisor / Ollama Fallback]

    App --> Connectors[Connector Resolver]
    Connectors --> SPO[SharePoint Online Graph]
    Connectors --> GDrive[Google Drive API]
    Connectors --> FileShare[File Share]
    Connectors --> OnPrem[SharePoint On-Prem Stub]

    API --> Worker[Background Migration Worker]
    Worker --> Connectors
    Worker --> DB
```

## Main Layers

| Layer | Responsibility |
| --- | --- |
| React/Vite UI | Authenticated operator workflow, dashboards, forms, reports, planning views |
| ASP.NET Core API | API endpoints, auth enforcement, CORS, startup, controllers |
| Application services | Discovery, readiness, planning, validation, simulation, AI, package generation |
| Infrastructure | EF Core persistence, repositories, Data Protection key storage |
| Migration engine | Background job processing, retries, state transitions, timelines |
| Connectors | File Share, Google Drive, SharePoint Online, SharePoint On-Prem stub |
| Reporting | CSV/JSON/Markdown exports and generated artifacts |

## Current Safety Boundary

The demo/submission workflow emphasizes discovery, planning, validation, simulation, preview, and reporting. Live pilot copy, metadata writeback, and permission preservation are roadmap items, not current submission claims.
