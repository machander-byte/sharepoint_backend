import { zmsApi } from "../../services/zmsApi";
import { migrationEvidence, reportExports } from "./v2DashboardData";

export interface V2RuntimeStatus {
  apiStatus: "Adapter" | "Healthy" | "Unavailable";
  version?: string;
  databaseProvider?: string;
  queueStatus?: string;
}

export interface V2ReadOnlySnapshot {
  source: "api" | "fallback";
  runtime: V2RuntimeStatus;
  connectionCount: number;
  latestJobName: string;
  latestJobStatus: string;
  latestReadinessScore: string;
  latestReadinessStatus: string;
  latestPlanStatus: string;
  latestWorkflowStatus: string;
  reportCount: number;
  aiRecommendationCount: number;
  errors: string[];
}

const fallbackSnapshot: V2ReadOnlySnapshot = {
  source: "fallback",
  runtime: {
    apiStatus: "Adapter",
    queueStatus: migrationEvidence.queue
  },
  connectionCount: 2,
  latestJobName: "Stage 1 Google Drive -> SharePoint",
  latestJobStatus: "Passed",
  latestReadinessScore: "Adapter",
  latestReadinessStatus: "Fallback evidence",
  latestPlanStatus: "Planning foundation present",
  latestWorkflowStatus: "Adapter evidence",
  reportCount: reportExports.length,
  aiRecommendationCount: 6,
  errors: []
};

interface HealthResponse {
  status?: string;
}

interface StatusResponse {
  status?: string;
  database?: {
    provider?: string;
  };
  queue?: {
    pendingCount?: number;
    statusMessage?: string;
  };
}

interface VersionResponse {
  version?: string;
}

export function getFallbackV2Snapshot(): V2ReadOnlySnapshot {
  return fallbackSnapshot;
}

export async function loadV2ReadOnlySnapshot(): Promise<V2ReadOnlySnapshot> {
  const errors: string[] = [];
  const runtime = await loadRuntimeStatus(errors);

  const [
    connections,
    latestJob,
    readiness,
    plan,
    workflow,
    reports,
    aiRecommendations
  ] = await Promise.all([
    settle("connections", () => zmsApi.getConnections(), errors),
    settle("latest migration job", () => zmsApi.getLatestMigrationExecutionJob(), errors),
    settle("latest readiness", () => zmsApi.getLatestReadinessAssessment(), errors),
    settle("latest migration plan", () => zmsApi.getLatestMigrationPlan(), errors),
    settle("latest workflow validation", () => zmsApi.getLatestWorkflowValidation(), errors),
    settle("reports", () => zmsApi.getReports(), errors),
    settle("AI recommendations", () => zmsApi.getAIRecommendations(), errors)
  ]);

  const connectionCount = Array.isArray(connections) ? connections.length : fallbackSnapshot.connectionCount;
  const reportCount = Array.isArray(reports) ? reports.length : fallbackSnapshot.reportCount;
  const aiRecommendationCount = Array.isArray(aiRecommendations) ? aiRecommendations.length : fallbackSnapshot.aiRecommendationCount;

  return {
    source: errors.length === 0 && runtime.apiStatus === "Healthy" ? "api" : "fallback",
    runtime,
    connectionCount,
    latestJobName: latestJob?.jobId ? `Execution job ${latestJob.jobId.slice(0, 8)}` : fallbackSnapshot.latestJobName,
    latestJobStatus: latestJob?.status ?? fallbackSnapshot.latestJobStatus,
    latestReadinessScore: typeof readiness?.readinessScore === "number" ? `${readiness.readinessScore}` : fallbackSnapshot.latestReadinessScore,
    latestReadinessStatus: readiness?.riskLevel ?? readiness?.status ?? fallbackSnapshot.latestReadinessStatus,
    latestPlanStatus: plan?.status ?? fallbackSnapshot.latestPlanStatus,
    latestWorkflowStatus: workflow?.overallResult ?? workflow?.status ?? fallbackSnapshot.latestWorkflowStatus,
    reportCount,
    aiRecommendationCount,
    errors
  };
}

async function loadRuntimeStatus(errors: string[]): Promise<V2RuntimeStatus> {
  const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/+$/, "");
  if (!baseUrl) {
    return fallbackSnapshot.runtime;
  }

  try {
    const [healthResponse, statusResponse, versionResponse] = await Promise.all([
      fetch(`${baseUrl}/api/health`),
      fetch(`${baseUrl}/api/status`),
      fetch(`${baseUrl}/api/version`)
    ]);

    if (!healthResponse.ok || !statusResponse.ok || !versionResponse.ok) {
      throw new Error("One or more runtime endpoints returned a non-success status.");
    }

    const health = (await healthResponse.json()) as HealthResponse;
    const status = (await statusResponse.json()) as StatusResponse;
    const version = (await versionResponse.json()) as VersionResponse;
    const healthy = health.status === "Healthy" && status.status === "Healthy";

    return {
      apiStatus: healthy ? "Healthy" : "Unavailable",
      version: version.version,
      databaseProvider: status.database?.provider,
      queueStatus: status.queue?.pendingCount === 0 ? "Queue empty" : status.queue?.statusMessage ?? migrationEvidence.queue
    };
  } catch {
    errors.push("Runtime health/status/version unavailable; using adapter fallback.");
    return {
      apiStatus: "Unavailable",
      queueStatus: migrationEvidence.queue
    };
  }
}

async function settle<T>(label: string, action: () => Promise<T>, errors: string[]): Promise<T | null> {
  try {
    return await action();
  } catch {
    errors.push(`${label} unavailable; using adapter fallback.`);
    return null;
  }
}
