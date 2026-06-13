import { BookOpenText, CheckCircle2, Download, Layers3, PlayCircle, Save, ShieldCheck } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import StatCard from "../components/StatCard";
import { zmsApi } from "../services/zmsApi";
import { LivePilotMigrationResult, MigrationTransferPreview, SharePointMigrationCapabilityResult, ExecutionSimulationResult, MigrationExecutionJob, MigrationPlan, MigrationPlanValidationResult, MigrationReadinessAssessment, PreMigrationValidationResult, RiskLevel } from "../types/zms";

function formatBytes(value: number): string {
  if (value <= 0) return "0 GB";
  return `${(value / 1024 / 1024 / 1024).toFixed(2)} GB`;
}

function normalizeRisk(value: string): RiskLevel {
  return value === "Critical" || value === "High" || value === "Medium" || value === "Low" ? value : "Medium";
}

export default function MigrationPlannerPage(): JSX.Element {
  const [assessment, setAssessment] = useState<MigrationReadinessAssessment | null>(null);
  const [plan, setPlan] = useState<MigrationPlan | null>(null);
  const [validation, setValidation] = useState<MigrationPlanValidationResult | null>(null);
  const [preValidation, setPreValidation] = useState<PreMigrationValidationResult | null>(null);
  const [simulation, setSimulation] = useState<ExecutionSimulationResult | null>(null);
  const [executionJob, setExecutionJob] = useState<MigrationExecutionJob | null>(null);
  const [capability, setCapability] = useState<SharePointMigrationCapabilityResult | null>(null);
  const [transferPreview, setTransferPreview] = useState<MigrationTransferPreview | null>(null);
  const [pilotResult, setPilotResult] = useState<LivePilotMigrationResult | null>(null);
  const [runbookMessage, setRunbookMessage] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;
    Promise.all([zmsApi.getLatestReadinessAssessment(), zmsApi.getLatestMigrationPlan(), zmsApi.getLatestPreMigrationValidation(), zmsApi.getLatestExecutionSimulation(), zmsApi.getLatestMigrationExecutionJob()]).then(([nextAssessment, nextPlan, nextPreValidation, nextSimulation, nextJob]) => {
      if (cancelled) return;
      setAssessment(nextAssessment);
      setPlan(nextPlan);
      setPreValidation(nextPreValidation);
      setSimulation(nextSimulation);
      setExecutionJob(nextJob);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const summary = useMemo(() => {
    const waves = plan?.waves ?? [];
    return {
      waves: waves.length,
      includedItems: waves.reduce((sum, wave) => sum + wave.includedItems.length, 0),
      excludedItems: waves.reduce((sum, wave) => sum + wave.excludedItems.length, 0),
      blockers: plan?.risks.filter((risk) => risk.migrationBlocker).length ?? 0,
      highRisks: plan?.risks.filter((risk) => risk.severity === "High" || risk.severity === "Critical").length ?? 0,
      files: waves.reduce((sum, wave) => sum + wave.estimatedFiles, 0),
      storage: waves.reduce((sum, wave) => sum + wave.estimatedStorage, 0),
      checklistDone: plan?.checklist.filter((item) => item.status === "completed").length ?? 0
    };
  }, [plan]);

  const createPlan = async () => {
    if (!assessment) return;
    setBusy(true);
    try {
      const response = await zmsApi.createMigrationPlanFromAssessment(assessment.assessmentId);
      setPlan(await zmsApi.getMigrationPlan(response.planId));
    } finally {
      setBusy(false);
    }
  };

  const savePlan = async (nextPlan = plan) => {
    if (!nextPlan) return;
    setPlan(await zmsApi.updateMigrationPlan(nextPlan));
  };

  const toggleOption = (key: string) => {
    if (!plan) return;
    const nextPlan = { ...plan, options: plan.options.map((option) => option.key === key ? { ...option, value: !option.value } : option) };
    setPlan(nextPlan);
  };

  const completeChecklistItem = (id: string) => {
    if (!plan) return;
    const nextPlan = {
      ...plan,
      checklist: plan.checklist.map((item) => item.id === id ? { ...item, status: item.status === "completed" ? "not_started" : "completed" } : item)
    };
    setPlan(nextPlan);
  };

  const validatePlan = async () => {
    if (!plan) return;
    setValidation(await zmsApi.validateMigrationPlan(plan.planId));
  };

  const generateRunbook = async () => {
    if (!plan) return;
    const runbook = await zmsApi.generateMigrationRunbook(plan.planId);
    setRunbookMessage(runbook ? "Runbook generated successfully." : "Runbook could not be generated.");
    setPlan(await zmsApi.getMigrationPlan(plan.planId));
  };

  const runPreMigrationValidation = async () => {
    if (!plan) return;
    const response = await zmsApi.runPreMigrationValidation(plan.planId);
    setPreValidation(await zmsApi.getPreMigrationValidation(response.validationId));
  };

  const runExecutionSimulation = async () => {
    if (!plan) return;
    const response = await zmsApi.runExecutionSimulation(plan.planId);
    setSimulation(await zmsApi.getExecutionSimulation(response.simulationId));
  };

  const createExecutionJob = async () => {
    if (!plan) return;
    const response = await zmsApi.createMigrationExecutionJobFromPlan(plan.planId, { mode: "simulation", requireGoDecision: false, createdBy: "Migration Lead" });
    window.location.href = `/jobs?jobId=${response.jobId}`;
  };

  const validateCapabilities = async () => setCapability(await zmsApi.validateSharePointMigrationCapabilities());
  const generateTransferPreview = async () => {
    const job = executionJob ?? await zmsApi.getLatestMigrationExecutionJob();
    if (!job) return;
    setTransferPreview(await zmsApi.generateSharePointTransferPreview(job.jobId));
  };
  const runPilot = async () => {
    const job = executionJob ?? await zmsApi.getLatestMigrationExecutionJob();
    if (!job) return;
    setPilotResult(await zmsApi.runLockedLivePilot(job.jobId, { selectedWaveId: job.waves[0]?.sourceWaveId ?? "", selectedLibrary: job.waves[0]?.items[0]?.library ?? "" }));
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Migration Planner"
        subtitle="Build a planning-only migration plan and runbook from readiness analysis."
        actions={
          <button
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90 disabled:opacity-60"
            disabled={!assessment || busy}
            onClick={() => void createPlan()}
          >
            <CheckCircle2 className="h-4 w-4" />
            {plan ? "Regenerate Plan" : "Create Migration Plan from Readiness"}
          </button>
        }
      />

      {!assessment ? (
        <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
          <h2 className="font-bold text-text-primary">No readiness assessment</h2>
          <p className="mt-2 text-sm text-text-muted">Run readiness analysis before generating a migration plan.</p>
        </section>
      ) : null}

      {plan ? (
        <>
          <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
            <div className="grid gap-4 lg:grid-cols-[1fr_auto]">
              <div>
                <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{plan.status}</p>
                <input
                  className="mt-1 w-full bg-transparent text-xl font-bold text-text-primary"
                  value={plan.planName}
                  onChange={(event) => setPlan({ ...plan, planName: event.target.value })}
                />
                <p className="mt-2 text-sm text-text-muted">{plan.sourceEnvironment} to {plan.targetEnvironment}</p>
                <p className="mt-1 text-xs text-text-subtle">Created {new Date(plan.createdAt).toLocaleDateString()} from readiness {plan.assessmentId}</p>
              </div>
              <button className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-bold" onClick={() => void savePlan()}>
                <Save className="h-4 w-4" />
                Save Plan
              </button>
            </div>
          </section>

          <section className="grid grid-cols-2 gap-4 xl:grid-cols-7">
            <StatCard label="Waves" value={summary.waves} />
            <StatCard label="Included" value={summary.includedItems} />
            <StatCard label="Excluded" value={summary.excludedItems} tone="warning" />
            <StatCard label="Blockers" value={summary.blockers} tone="error" />
            <StatCard label="High Risks" value={summary.highRisks} tone="error" />
            <StatCard label="Files" value={summary.files.toLocaleString()} />
            <StatCard label="Storage" value={formatBytes(summary.storage)} />
          </section>

          <section className="grid grid-cols-1 gap-4 xl:grid-cols-2">
            {plan.waves.map((wave) => (
              <article key={wave.waveId} className="rounded-xl border border-border bg-surface p-5 shadow-card">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Order {wave.order} / {wave.approvalStatus}</p>
                    <h2 className="mt-1 text-lg font-bold text-text-primary">{wave.waveName}</h2>
                  </div>
                  <RiskBadge level={normalizeRisk(wave.riskLevel)} />
                </div>
                <p className="mt-2 text-sm leading-6 text-text-muted">{wave.description}</p>
                <div className="mt-4 grid grid-cols-3 gap-2 text-sm">
                  <div className="rounded-lg bg-surface-container p-3"><strong>{wave.readinessScore}%</strong><br />Readiness</div>
                  <div className="rounded-lg bg-surface-container p-3"><strong>{wave.estimatedFiles.toLocaleString()}</strong><br />Files</div>
                  <div className="rounded-lg bg-surface-container p-3"><strong>{formatBytes(wave.estimatedStorage)}</strong><br />Storage</div>
                </div>
                <p className="mt-4 text-sm text-text-muted">{wave.includedItems.map((item) => item.library).join(", ") || "No included libraries."}</p>
                <p className="mt-2 text-xs text-text-subtle">Prerequisites: {wave.prerequisites.join(", ") || "None"}</p>
              </article>
            ))}
          </section>

          <section className="grid grid-cols-1 gap-4 xl:grid-cols-2">
            <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
              <h2 className="font-bold text-text-primary">Plan Options</h2>
              <div className="mt-4 grid gap-2">
                {plan.options.map((option) => (
                  <label key={option.key} className="flex items-center justify-between rounded-lg bg-surface-container p-3 text-sm font-semibold">
                    {option.label}
                    <input type="checkbox" checked={option.value} onChange={() => toggleOption(option.key)} />
                  </label>
                ))}
              </div>
            </article>

            <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
              <h2 className="font-bold text-text-primary">Pre-Migration Checklist</h2>
              <div className="mt-4 max-h-[420px] overflow-auto pr-2">
                {plan.checklist.map((item) => (
                  <label key={item.id} className="mb-2 flex items-start gap-3 rounded-lg bg-surface-container p-3 text-sm">
                    <input type="checkbox" checked={item.status === "completed"} onChange={() => completeChecklistItem(item.id)} />
                    <span>
                      <span className="block font-semibold text-text-primary">{item.title}</span>
                      <span className="text-text-muted">{item.ownerRole}</span>
                    </span>
                  </label>
                ))}
              </div>
            </article>
          </section>

          <section className="grid grid-cols-1 gap-4 xl:grid-cols-2">
            <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
              <div className="flex items-center justify-between gap-3">
                <h2 className="font-bold text-text-primary">Validation</h2>
                <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void validatePlan()}>
                  <ShieldCheck className="h-4 w-4" />
                  Validate Plan
                </button>
              </div>
              <div className="mt-4 grid gap-2 text-sm">
                {validation ? (
                  <>
                    <p className={validation.isValid ? "font-bold text-success" : "font-bold text-error"}>{validation.isValid ? "Plan is valid" : "Plan needs review"}</p>
                    {[...validation.errors, ...validation.warnings].map((message) => <p key={message} className="rounded-lg bg-surface-container p-3">{message}</p>)}
                  </>
                ) : <p className="text-text-muted">Run validation before execution design.</p>}
              </div>
            </article>

            <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h2 className="font-bold text-text-primary">Runbook</h2>
                <div className="flex flex-wrap gap-2">
                  <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void generateRunbook()}>
                    <BookOpenText className="h-4 w-4" />
                    Generate Runbook
                  </button>
                  <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void zmsApi.downloadMigrationPlanExport(plan.planId, "markdown")}>
                    <Download className="h-4 w-4" />
                    Markdown
                  </button>
                </div>
              </div>
              <p className="mt-4 text-sm text-text-muted">{runbookMessage || plan.runbookPath || "Runbook has not been generated yet."}</p>
            </article>
          </section>

          <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <h2 className="font-bold text-text-primary">Pre-Migration Validation</h2>
                <p className="mt-1 text-sm text-text-muted">Go/no-go validation and execution simulation. No migration is run.</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <button className="rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void runPreMigrationValidation()}>Run Pre-Migration Validation</button>
                <button className="rounded-lg bg-primary px-3 py-2 text-sm font-bold text-white" onClick={() => void runExecutionSimulation()}>Run Execution Simulation</button>
                <button className="inline-flex items-center gap-2 rounded-lg border border-primary px-3 py-2 text-sm font-bold text-primary" onClick={() => void createExecutionJob()}>
                  <PlayCircle className="h-4 w-4" />
                  Create Simulation Execution Job
                </button>
              </div>
            </div>

            {preValidation ? (
              <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-5">
                <div className="rounded-lg bg-surface-container p-3"><strong>{preValidation.decision}</strong><br />Decision</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{preValidation.summary.errors}</strong><br />Errors</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{preValidation.summary.warnings}</strong><br />Warnings</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{preValidation.summary.readyWaves}</strong><br />Ready Waves</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{preValidation.summary.blockedWaves}</strong><br />Blocked Waves</div>
              </div>
            ) : null}

            {preValidation ? (
              <div className="mt-4 overflow-auto">
                <table className="w-full text-left text-sm">
                  <thead className="text-xs uppercase text-text-subtle"><tr><th className="py-2">Check</th><th>Category</th><th>Status</th><th>Severity</th><th>Recommended Action</th></tr></thead>
                  <tbody>
                    {preValidation.checks.slice(0, 12).map((check) => (
                      <tr key={check.checkId} className="border-t border-border">
                        <td className="py-2 font-semibold">{check.title}</td>
                        <td>{check.category}</td>
                        <td>{check.status}</td>
                        <td>{check.severity}</td>
                        <td>{check.recommendedAction}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : null}

            {simulation ? (
              <div className="mt-5">
                <div className="grid grid-cols-2 gap-3 text-sm md:grid-cols-5">
                  <div className="rounded-lg bg-surface-container p-3"><strong>{Math.floor(simulation.estimatedDurationMinutes / 60)}h {simulation.estimatedDurationMinutes % 60}m</strong><br />Duration</div>
                  <div className="rounded-lg bg-surface-container p-3"><strong>{simulation.estimatedFiles.toLocaleString()}</strong><br />Files</div>
                  <div className="rounded-lg bg-surface-container p-3"><strong>{formatBytes(simulation.estimatedStorageBytes)}</strong><br />Storage</div>
                  <div className="rounded-lg bg-surface-container p-3"><strong>{simulation.expectedIssues.length}</strong><br />Issues</div>
                  <div className="rounded-lg bg-surface-container p-3"><strong>{simulation.waves.length}</strong><br />Waves</div>
                </div>
                <div className="mt-4 grid gap-3">
                  {simulation.waves.map((wave) => (
                    <div key={wave.waveId} className="rounded-lg border border-border bg-surface-container p-3">
                      <div className="flex justify-between gap-3"><strong>{wave.waveName}</strong><span>{wave.estimatedDurationMinutes} min</span></div>
                      <p className="mt-1 text-sm text-text-muted">{wave.steps.map((step) => step.stepName).join(" -> ")}</p>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}
          </section>

          <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <h2 className="font-bold text-text-primary">SharePoint Migration Adapter Preview</h2>
                <p className="mt-1 text-sm text-text-muted">Live pilot migration is disabled by default. Enable only in a test tenant after Go/No-Go approval.</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <button className="rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void validateCapabilities()}>Validate Migration Capabilities</button>
                <button className="rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void generateTransferPreview()}>Generate Transfer Preview</button>
                <button className="rounded-lg border border-error px-3 py-2 text-sm font-bold text-error opacity-60" onClick={() => void runPilot()}>Run Locked Pilot Migration</button>
              </div>
            </div>
            {capability ? (
              <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-7">
                {Object.entries(capability.capabilities).map(([key, value]) => (
                  <div key={key} className="rounded-lg bg-surface-container p-3"><strong>{value ? "Yes" : "No"}</strong><br />{key}</div>
                ))}
              </div>
            ) : null}
            {transferPreview ? (
              <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-4">
                <div className="rounded-lg bg-surface-container p-3"><strong>{transferPreview.totalItems}</strong><br />Total Items</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{transferPreview.eligibleItems}</strong><br />Eligible</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{transferPreview.blockedItems}</strong><br />Blocked</div>
                <div className="rounded-lg bg-surface-container p-3"><strong>{transferPreview.permissionMappings.length}</strong><br />Permission Mappings</div>
              </div>
            ) : null}
            {pilotResult ? (
              <div className="mt-4 rounded-lg bg-surface-container p-4 text-sm">
                <strong>{pilotResult.status}</strong>
                <p className="mt-1 text-text-muted">{pilotResult.message}</p>
                <p className="mt-2 text-xs text-text-subtle">Safety checks failed: {pilotResult.safetyChecks.filter((check) => check.status === "failed").length}</p>
              </div>
            ) : null}
          </section>
        </>
      ) : (
        <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
          <Layers3 className="h-5 w-5 text-primary" />
          <h2 className="mt-3 font-bold text-text-primary">No migration plan yet</h2>
          <p className="mt-2 text-sm text-text-muted">Create a draft migration plan from the latest readiness assessment.</p>
        </section>
      )}
    </div>
  );
}
