# ZMS ShareGate-Like Feature Gap Matrix

Status date: 2026-06-09

This matrix is for internal positioning only. It does not claim feature parity with ShareGate.

| Capability | ZMS status | Gap |
| --- | --- | --- |
| Google Drive -> SharePoint file copy | Proven baseline | 22-file live run passed; still needs 100/1k/10k scale proof |
| SharePoint -> SharePoint file copy | Implemented foundation | Needs live certification with fresh source/target connections |
| File share -> SharePoint | Implemented foundation | Long-path discovery fixed and tested; needs live migration benchmark |
| OneDrive migration | Not proven | Needs first-class connector or explicit Graph user-drive extension |
| Discovery inventory | Strong | Needs repeated live tenant export evidence |
| Permission risk reporting | Strong | Permission writeback remains preview/limited |
| Metadata analysis | Strong | Full metadata writeback needs deeper validation |
| Migration planning | Strong | Needs large-scale operational proof |
| Runbook generation | Strong | Needs company-ready sample exports from live runs |
| Simulation engine | Strong | Simulation is not a substitute for live migration proof |
| Resume/retry | Implemented foundation | Needs controlled live interruption test |
| Monitoring | Partial | Sentry wiring exists; no event captured in this pass |
| RBAC/security | Good foundation | Need production role claims and unauthenticated endpoint audit |
| Reporting/export | Strong foundation | Live export download/open verification pending while backend is offline |
| Tenant-safe UX | Good | Needs V2 pass for migration wizard/evidence center |
| Enterprise scale | Not certified | 100/1k/10k/100k stages not complete |
| SaaS onboarding | Early | Custom domain, onboarding, tenant setup, and operations runbooks need completion |

## Current Positioning

ZMS is credible as an internal pre-production migration validation platform. It is not yet a market-ready ShareGate replacement because enterprise scale, recovery, OneDrive, permission writeback, and repeated live certification are still open.
