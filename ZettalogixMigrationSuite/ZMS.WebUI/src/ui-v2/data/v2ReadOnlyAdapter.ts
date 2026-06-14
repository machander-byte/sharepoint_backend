import { migrationEvidence, reportExports } from "./v2DashboardData";

export interface V2RuntimeStatus {
  apiStatus: "Adapter" | "Healthy" | "Degraded" | "Unavailable";
  version?: string;
  databaseProvider?: string;
  queueStatus?: string;
  databaseStartupStatus?: string;
  databaseMessage?: string;
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
    queueStatus: "No live queue data"
  },
  connectionCount: 0,
  latestJobName: "No live migration job",
  latestJobStatus: "No live data",
  latestReadinessScore: "No live data",
  latestReadinessStatus: "No live data",
  latestPlanStatus: "No live data",
  latestWorkflowStatus: "No live data",
  reportCount: 0,
  aiRecommendationCount: 0,
  errors: []
};

interface StatusResponse {
  status?: string;
  databaseStartup?: {
    status?: string;
    message?: string;
  };
  database?: {
    healthy?: boolean;
    provider?: string;
    message?: string;
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

  if (runtime.apiStatus !== "Healthy") {
    return {
      ...fallbackSnapshot,
      runtime,
      errors: errors.length > 0 ? errors : ["Live API is not healthy; no fallback records are shown as real data."]
    };
  }

  return {
    source: "api",
    runtime,
    connectionCount: 0,
    latestJobName: fallbackSnapshot.latestJobName,
    latestJobStatus: fallbackSnapshot.latestJobStatus,
    latestReadinessScore: fallbackSnapshot.latestReadinessScore,
    latestReadinessStatus: fallbackSnapshot.latestReadinessStatus,
    latestPlanStatus: fallbackSnapshot.latestPlanStatus,
    latestWorkflowStatus: fallbackSnapshot.latestWorkflowStatus,
    reportCount: 0,
    aiRecommendationCount: 0,
    errors
  };
}

async function loadRuntimeStatus(errors: string[]): Promise<V2RuntimeStatus> {
  const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/+$/, "");
  if (!baseUrl) {
    return fallbackSnapshot.runtime;
  }

  try {
    const [statusResponse, versionResponse] = await Promise.all([
      fetch(`${baseUrl}/api/status`),
      fetch(`${baseUrl}/api/version`)
    ]);

    if (!versionResponse.ok) {
      throw new Error("One or more runtime endpoints returned a non-success status.");
    }

    const status = (await statusResponse.json()) as StatusResponse;
    const version = (await versionResponse.json()) as VersionResponse;
    const healthy = statusResponse.ok && status.status === "Healthy";
    const degraded = status.status === "Degraded" || !statusResponse.ok;

    return {
      apiStatus: healthy ? "Healthy" : degraded ? "Degraded" : "Unavailable",
      version: version.version,
      databaseProvider: status.database?.provider,
      databaseStartupStatus: status.databaseStartup?.status,
      databaseMessage: status.database?.message ?? status.databaseStartup?.message,
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
