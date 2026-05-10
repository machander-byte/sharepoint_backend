import {
  AppSettings,
  ConnectionRecord,
  ConnectionStatus,
  ConnectionTestResult,
  CreateConnectionInput,
  CreateJobInput,
  JobEvent,
  MigrationJob
} from "../utils/models";
import { formatErrorForToast } from "../utils/errorHelp";
import { createClient } from "../lib/client";

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5206").replace(/\/+$/, "");
const settingsStorageKey = "zms-web-ui-settings";

const connectionHealth = new Map<string, ConnectionTestResult>();

interface ApiConnectionResponse {
  id: string;
  name: string;
  type: ConnectionRecord["type"];
  url: string;
  rootPath?: string;
  documentLibraryName?: string;
  hasClientSecret: boolean;
  hasRefreshToken: boolean;
  isEnabled: boolean;
  createdUtc: string;
  updatedUtc: string;
}

interface ApiConnectionTestResponse {
  isSuccess: boolean;
  message: string;
  testedUtc: string;
}

interface ApiMigrationJobResponse {
  id: string;
  name: string;
  sourceConnectionId: string;
  targetConnectionId: string;
  sourceLocation: string;
  sourceLibraryName?: string;
  targetSiteUrl: string;
  targetLibraryName: string;
  targetLibraryUrlSegment?: string;
  targetRootPath?: string;
  preserveMetadata: boolean;
  batchSize: number;
  maxRetryCount: number;
  status: MigrationJob["status"];
  totalItems: number;
  completedItems: number;
  failedItems: number;
  lastError?: string;
  createdUtc: string;
  startedUtc?: string;
  finishedUtc?: string;
  updatedUtc: string;
}

interface ApiJobReportResponse {
  progressPercentage: number;
  recentLogs: ApiLogEntry[];
}

interface ApiLogEntry {
  id: string;
  severity: "Information" | "Warning" | "Error";
  message: string;
  details?: string;
  createdUtc: string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function flattenProblemErrors(errors: unknown): string[] {
  if (Array.isArray(errors)) {
    return errors.filter((item): item is string => typeof item === "string");
  }

  if (!isRecord(errors)) {
    return typeof errors === "string" ? [errors] : [];
  }

  return Object.entries(errors).flatMap(([field, fieldErrors]) => {
    if (Array.isArray(fieldErrors)) {
      return fieldErrors
        .filter((item): item is string => typeof item === "string")
        .map((item) => `${field}: ${item}`);
    }

    return typeof fieldErrors === "string" ? [`${field}: ${fieldErrors}`] : [];
  });
}

async function readErrorMessage(response: Response, path: string): Promise<string> {
  const fallback = `Request to '${path}' failed with status ${response.status}.`;
  const payload = await response.text();
  if (!payload) {
    return fallback;
  }

  try {
    const parsed = JSON.parse(payload) as unknown;
    if (!isRecord(parsed)) {
      return payload;
    }

    const title = typeof parsed.title === "string" ? parsed.title : "";
    const detail = typeof parsed.detail === "string" ? parsed.detail : "";
    const message = typeof parsed.message === "string" ? parsed.message : "";
    const errors = flattenProblemErrors(parsed.errors);
    const parts = [detail, message, title, ...errors].filter(Boolean);

    return parts.length > 0 ? parts.join(" ") : fallback;
  } catch {
    return payload;
  }
}

const defaultSettings: AppSettings = {
  concurrency: 4,
  retryLimit: 3,
  notifyOnFailure: true,
  telemetryEnabled: false
};

export function getApiBaseUrl(): string {
  return apiBaseUrl;
}

export function getReportDownloadUrl(path: string): string {
  return `${apiBaseUrl}/api/reports${path}`;
}

async function getAuthorizationHeaders(): Promise<Record<string, string>> {
  const { data } = await createClient().auth.getSession();
  const accessToken = data.session?.access_token;

  return accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
}

function extractGoogleDriveFolderId(candidate?: string): string {
  const trimmed = candidate?.trim() ?? "";
  if (!trimmed) {
    return "";
  }

  const folderMatch = trimmed.match(/\/drive\/(?:u\/\d+\/)?folders\/([A-Za-z0-9_-]+)/i);
  if (folderMatch?.[1]) {
    return folderMatch[1];
  }

  return /^[A-Za-z0-9_-]{10,}$/.test(trimmed) ? trimmed : "";
}

function buildGoogleDriveFolderUrl(folderId: string): string {
  return `https://drive.google.com/drive/folders/${folderId}`;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const authHeaders = await getAuthorizationHeaders();
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}/api${path}`, {
      headers: {
        "Content-Type": "application/json",
        ...authHeaders,
        ...(init?.headers ?? {})
      },
      ...init
    });
  } catch {
    throw new Error(
      formatErrorForToast(`API is not reachable at ${apiBaseUrl}. Start the backend and refresh the page.`)
    );
  }

  if (!response.ok) {
    const message = await readErrorMessage(response, path);
    throw new Error(formatErrorForToast(message));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await response.text();
  if (!payload) {
    return undefined as T;
  }

  return JSON.parse(payload) as T;
}

async function downloadReportFile(path: string): Promise<void> {
  const authHeaders = await getAuthorizationHeaders();
  let response: Response;

  try {
    response = await fetch(getReportDownloadUrl(path), {
      headers: authHeaders
    });
  } catch {
    throw new Error(
      formatErrorForToast(`API is not reachable at ${apiBaseUrl}. Start the backend and refresh the page.`)
    );
  }

  if (!response.ok) {
    const message = await readErrorMessage(response, `/reports${path}`);
    throw new Error(formatErrorForToast(message));
  }

  const blob = await response.blob();
  const disposition = response.headers.get("content-disposition") ?? "";
  const fileNameMatch = disposition.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
  const fileName = fileNameMatch?.[1] ? decodeURIComponent(fileNameMatch[1]) : "zms-report.csv";
  const objectUrl = window.URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = objectUrl;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(objectUrl);
}

function getConnectionSummary(connection: ApiConnectionResponse): string {
  switch (connection.type) {
    case "SharePointOnPrem":
      return "Legacy SharePoint source connection.";
    case "SharePointOnline":
      return connection.documentLibraryName
        ? `Targets SharePoint library '${connection.documentLibraryName}'.`
        : "Target Microsoft 365 document library connection.";
    case "FileShare":
      return connection.rootPath
        ? `Reads files from '${connection.rootPath}'.`
        : "Reads files from a network file share.";
    case "GoogleDrive":
      return connection.rootPath
        ? `Uses Google Drive folder scope '${connection.rootPath}'.`
        : "Uses the configured Google Drive root.";
    default:
      return "Migration connection profile.";
  }
}

function getConnectionStatus(connectionId: string, updatedUtc: string): Pick<ConnectionRecord, "status" | "lastChecked" | "lastTestMessage"> {
  const health = connectionHealth.get(connectionId);
  if (!health) {
    return {
      status: "Disconnected",
      lastChecked: updatedUtc
    };
  }

  return {
    status: health.isSuccess ? "Healthy" : "Warning",
    lastChecked: health.testedAt,
    lastTestMessage: health.message
  };
}

function mapConnection(connection: ApiConnectionResponse): ConnectionRecord {
  return {
    id: connection.id,
    name: connection.name,
    type: connection.type,
    url: connection.url,
    rootPath: connection.rootPath,
    documentLibraryName: connection.documentLibraryName,
    hasClientSecret: Boolean(connection.hasClientSecret),
    hasRefreshToken: Boolean(connection.hasRefreshToken),
    summary: getConnectionSummary(connection),
    ...getConnectionStatus(connection.id, connection.updatedUtc)
  };
}

function mapEventLevel(severity: ApiLogEntry["severity"]): JobEvent["level"] {
  switch (severity) {
    case "Error":
      return "error";
    case "Warning":
      return "warning";
    default:
      return "info";
  }
}

function mapJob(job: ApiMigrationJobResponse, report?: ApiJobReportResponse | null): MigrationJob {
  const totalFiles = job.totalItems;
  const migratedFiles = job.completedItems;
  const progress =
    report?.progressPercentage
    ?? (totalFiles > 0 ? Math.round((migratedFiles / totalFiles) * 100) : job.status === "Completed" ? 100 : 0);

  return {
    id: job.id,
    name: job.name,
    sourceConnectionId: job.sourceConnectionId,
    targetConnectionId: job.targetConnectionId,
    sourcePath: job.sourceLocation,
    sourceLibraryName: job.sourceLibraryName,
    targetSite: job.targetSiteUrl,
    targetLibrary: job.targetLibraryName,
    targetLibraryUrlSegment: job.targetLibraryUrlSegment,
    targetRootPath: job.targetRootPath,
    preserveMetadata: job.preserveMetadata,
    totalFiles,
    migratedFiles,
    failedFiles: job.failedItems,
    progress,
    status: job.status,
    createdAt: job.createdUtc,
    updatedAt: job.updatedUtc,
    startedAt: job.startedUtc,
    lastError: job.lastError,
    history:
      report?.recentLogs.map((log) => ({
        id: log.id,
        timestamp: log.createdUtc,
        level: mapEventLevel(log.severity),
        message: log.message,
        details: log.details
      })) ?? []
  };
}

async function getJobReport(jobId: string): Promise<ApiJobReportResponse | null> {
  try {
    return await request<ApiJobReportResponse>(`/reports/jobs/${jobId}`);
  } catch {
    return null;
  }
}

async function getSettingsStorageKey(): Promise<string> {
  const { data } = await createClient().auth.getSession();
  const userId = data.session?.user.id;
  return userId ? `${settingsStorageKey}:${userId}` : settingsStorageKey;
}

async function loadSettings(): Promise<AppSettings> {
  const stored = window.localStorage.getItem(await getSettingsStorageKey());
  if (!stored) {
    return defaultSettings;
  }

  try {
    return { ...defaultSettings, ...(JSON.parse(stored) as Partial<AppSettings>) };
  } catch {
    return defaultSettings;
  }
}

async function saveSettingsToStorage(settings: AppSettings): Promise<void> {
  window.localStorage.setItem(await getSettingsStorageKey(), JSON.stringify(settings));
}

export const api = {
  async getJobs(): Promise<MigrationJob[]> {
    const jobs = await request<ApiMigrationJobResponse[]>("/jobs");
    const reports = await Promise.all(jobs.map((job) => getJobReport(job.id)));

    return jobs
      .map((job, index) => mapJob(job, reports[index]))
      .sort((left, right) => right.updatedAt.localeCompare(left.updatedAt));
  },

  async getJob(id: string): Promise<MigrationJob | undefined> {
    try {
      const [job, report] = await Promise.all([
        request<ApiMigrationJobResponse>(`/jobs/${id}`),
        getJobReport(id)
      ]);

      return mapJob(job, report);
    } catch {
      return undefined;
    }
  },

  async createJob(input: CreateJobInput): Promise<MigrationJob> {
    const job = await request<ApiMigrationJobResponse>("/jobs", {
      method: "POST",
      body: JSON.stringify({
        name: input.name,
        sourceConnectionId: input.sourceConnectionId,
        targetConnectionId: input.targetConnectionId,
        sourceLocation: input.sourcePath,
        sourceLibraryName: input.sourceLibraryName || null,
        targetSiteUrl: input.targetSite,
        targetLibraryName: input.targetLibrary,
        targetLibraryUrlSegment: input.targetLibraryUrlSegment || null,
        targetRootPath: input.targetRootPath || null,
        preserveMetadata: input.preserveMetadata,
        batchSize: 20,
        maxRetryCount: 3
      })
    });

    return mapJob(job, await getJobReport(job.id));
  },

  async startMigration(id: string): Promise<void> {
    await request<void>(`/jobs/${id}/start`, { method: "POST", body: "{}" });
  },

  async pauseMigration(id: string): Promise<void> {
    await request<void>(`/jobs/${id}/pause`, { method: "POST", body: "{}" });
  },

  async getConnections(): Promise<ConnectionRecord[]> {
    const connections = await request<ApiConnectionResponse[]>("/connections");
    return connections.map(mapConnection);
  },

  async createConnection(input: CreateConnectionInput): Promise<ConnectionRecord> {
    const googleFolderId =
      input.type === "GoogleDrive"
        ? input.folderId || input.rootPath || extractGoogleDriveFolderId(input.folderUrl || input.url)
        : "";
    const rawGoogleFolderValue = input.type === "GoogleDrive" ? (input.folderUrl || input.url).trim() : "";
    const googleFolderUrl =
      input.type === "GoogleDrive"
        ? rawGoogleFolderValue === googleFolderId && googleFolderId
          ? buildGoogleDriveFolderUrl(googleFolderId)
          : rawGoogleFolderValue || (googleFolderId ? buildGoogleDriveFolderUrl(googleFolderId) : "")
        : input.url;
    const additionalSettings: Record<string, string> = {};

    if (input.type === "GoogleDrive") {
      if (googleFolderId) {
        additionalSettings.FolderId = googleFolderId;
      }

      if (googleFolderUrl) {
        additionalSettings.FolderUrl = googleFolderUrl;
      }

      if (input.folderName) {
        additionalSettings.FolderName = input.folderName;
      }
    }

    if (input.type === "SharePointOnline" && input.documentLibraryName) {
      additionalSettings.DocumentLibraryName = input.documentLibraryName.trim();
    }

    const connection = await request<ApiConnectionResponse>("/connections", {
      method: "POST",
      body: JSON.stringify(input.type === "GoogleDrive" ? {
        name: input.name,
        type: input.type,
        url: googleFolderUrl,
        rootPath: googleFolderId || null,
        additionalSettings
      } : {
        name: input.name,
        type: input.type,
        url: input.url,
        username: input.username || null,
        password: input.password || null,
        clientId: input.clientId || null,
        clientSecret: input.clientSecret || null,
        tenantId: input.tenantId || null,
        rootPath: input.rootPath || null,
        additionalSettings
      })
    });

    return mapConnection(connection);
  },

  async testConnection(id: string): Promise<ConnectionTestResult> {
    const result = await request<ApiConnectionTestResponse>(`/connections/${id}/test`, {
      method: "POST",
      body: "{}"
    });

    const mappedResult: ConnectionTestResult = {
      isSuccess: result.isSuccess,
      message: result.message,
      testedAt: result.testedUtc
    };

    connectionHealth.set(id, mappedResult);
    return mappedResult;
  },

  async deleteConnection(id: string): Promise<void> {
    await request<void>(`/connections/${id}`, { method: "DELETE" });
    connectionHealth.delete(id);
  },

  async getSettings(): Promise<AppSettings> {
    return loadSettings();
  },

  async saveSettings(input: AppSettings): Promise<AppSettings> {
    await saveSettingsToStorage(input);
    return input;
  },

  async downloadReport(path: string): Promise<void> {
    try {
      await downloadReportFile(path);
    } catch (error) {
      window.alert(error instanceof Error ? error.message : "Report download failed.");
    }
  },

  clearSessionCache(): void {
    connectionHealth.clear();
  }
};
