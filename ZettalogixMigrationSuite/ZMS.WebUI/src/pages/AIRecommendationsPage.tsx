import { ClipboardCheck, Sparkles, TimerReset } from "lucide-react";
import { useState } from "react";
import PageHeader from "../components/PageHeader";
import { MigrationExecutionJob, MigrationReadinessAssessment, MigrationTransferPreview, PreMigrationValidationResult, WorkflowValidationRun } from "../types/zms";

export default function AIRecommendationsPage(): JSX.Element {
  const [assessment] = useState<MigrationReadinessAssessment | null>(null);
  const [eta] = useState("Not estimated");
  const [planAvailable] = useState(false);
  const [preValidation] = useState<PreMigrationValidationResult | null>(null);
  const [executionJob] = useState<MigrationExecutionJob | null>(null);
  const [transferPreview] = useState<MigrationTransferPreview | null>(null);
  const [workflow] = useState<WorkflowValidationRun | null>(null);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="AI Recommendations"
        subtitle="Readiness-driven remediation actions for migration planning. No migration is executed from this page."
      />

      <section className="grid grid-cols-1 gap-4 xl:grid-cols-3">
        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <div className="flex items-center gap-3">
            <Sparkles className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-text-primary">Readiness</h2>
          </div>
          <p className="mt-4 text-3xl font-bold text-primary">{assessment ? `${assessment.readinessScore}%` : "-"}</p>
          <p className="mt-2 text-sm leading-6 text-text-muted">
            {assessment ? `${assessment.riskLevel} risk with ${assessment.summary.blockers} blockers and ${assessment.summary.remediationActions} remediation actions.` : "Run readiness analysis to populate recommendations."}
          </p>
        </article>

        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <div className="flex items-center gap-3">
            <TimerReset className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-text-primary">Predictive ETA</h2>
          </div>
          <p className="mt-4 text-3xl font-bold text-primary">{eta}</p>
          <p className="mt-2 text-sm leading-6 text-text-muted">
            ETA uses discovered file count, size, retry pressure, large-file count, and concurrency assumptions.
          </p>
        </article>

        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <div className="flex items-center gap-3">
            <ClipboardCheck className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-text-primary">Modernization</h2>
          </div>
          <p className="mt-4 text-3xl font-bold text-primary">{assessment?.modernizationOpportunities.length ?? 0}</p>
          <p className="mt-2 text-sm leading-6 text-text-muted">Potential workflow, form, reporting, and governance modernization candidates.</p>
        </article>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="flex items-center gap-3">
          <Sparkles className="h-5 w-5 text-primary" />
          <h2 className="font-bold text-text-primary">Validation Recommendations</h2>
        </div>
        <div className="mt-4 grid gap-3">
          {workflow?.issues.slice(0, 6).map((issue) => (
            <div key={issue.issueId} className="rounded-lg border border-border bg-surface-container p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Workflow Validation / {issue.severity}</p>
              <h3 className="mt-1 font-bold text-text-primary">{issue.stepName}</h3>
              <p className="mt-2 text-sm leading-6 text-text-muted">{issue.message}</p>
              <p className="mt-2 text-sm font-semibold text-primary">{issue.recommendedAction}</p>
            </div>
          ))}
          {!workflow && transferPreview?.blocked.slice(0, 6).map((item) => (
            <div key={item.itemId} className="rounded-lg border border-border bg-surface-container p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Transfer Preview Blocker</p>
              <h3 className="mt-1 font-bold text-text-primary">{item.reason}</h3>
              <p className="mt-2 text-sm leading-6 text-text-muted">{item.recommendedAction}</p>
              <p className="mt-2 text-xs text-text-subtle">{item.path}</p>
            </div>
          ))}
          {!transferPreview && executionJob?.waves.flatMap((wave) => wave.items.filter((item) => item.status !== "completed" || item.warnings.length > 0).map((item) => ({ wave, item }))).slice(0, 6).map(({ wave, item }) => (
            <div key={item.itemExecutionId} className="rounded-lg border border-border bg-surface-container p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Execution Simulation / {wave.waveName}</p>
              <h3 className="mt-1 font-bold text-text-primary">Review simulated {item.status} item in {item.library}</h3>
              <p className="mt-2 text-sm leading-6 text-text-muted">
                {item.errors[0] ?? item.warnings[0] ?? "Re-run validation before enabling live execution."}
              </p>
            </div>
          ))}
          {!executionJob && preValidation?.checks.filter((check) => check.status !== "passed").slice(0, 8).map((check) => (
            <div key={check.checkId} className="rounded-lg border border-border bg-surface-container p-4">
              <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{check.severity} / {check.category}</p>
              <h3 className="mt-1 font-bold text-text-primary">{check.title}</h3>
              <p className="mt-2 text-sm leading-6 text-text-muted">{check.recommendedAction}</p>
            </div>
          ))}
          {!executionJob && !preValidation && (!assessment || assessment.remediationActions.length === 0) ? (
            <p className="rounded-lg bg-surface-container p-4 text-sm text-text-muted">
              No remediation actions are available until discovery and readiness analysis complete.
            </p>
          ) : !executionJob && !preValidation ? assessment?.remediationActions.map((item) => (
            <div key={item.id} className="rounded-lg border border-border bg-surface-container p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{item.priority} / {item.ownerRole}</p>
                  <h3 className="mt-1 font-bold text-text-primary">{item.actionTitle}</h3>
                </div>
                <span className="rounded-full bg-primary-soft px-3 py-1 text-xs font-bold text-primary">{item.estimatedEffort}</span>
              </div>
              <p className="mt-3 text-sm leading-6 text-text-muted">{item.actionDescription}</p>
              <p className="mt-2 text-sm font-semibold text-text-primary">{item.expectedBenefit}</p>
              <p className="mt-2 text-xs text-text-subtle">{item.affectedLocations.slice(0, 4).join(", ")}</p>
              <div className="mt-3 flex flex-wrap gap-2">
                <button className="rounded-lg border border-border px-3 py-1.5 text-xs font-bold text-text-primary">Review</button>
                <button className="rounded-lg bg-primary px-3 py-1.5 text-xs font-bold text-white">Mark as planned</button>
                <button
                  className="rounded-lg border border-primary px-3 py-1.5 text-xs font-bold text-primary"
                  onClick={() => window.alert(planAvailable ? "Prerequisite can be added when detailed prerequisite editing is enabled." : "Create a migration plan first.")}
                >
                  Add to Migration Plan Prerequisites
                </button>
              </div>
            </div>
          )) : null}
        </div>
      </section>
    </div>
  );
}
