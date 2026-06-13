import { create } from "zustand";
import { api, getApiBaseUrl } from "../services/api";
import { formatErrorForToast } from "../utils/errorHelp";
import {
  AppSettings,
  ConnectionRecord,
  ConnectionTestResult,
  CreateConnectionInput,
  CreateJobInput,
  LoadingState,
  MigrationJob,
  NotificationMessage
} from "../utils/models";

export interface AppState {
  jobs: MigrationJob[];
  connections: ConnectionRecord[];
  settings: AppSettings | null;
  notifications: NotificationMessage[];
  loading: LoadingState;
  bootstrap: () => Promise<void>;
  refreshJobs: () => Promise<void>;
  refreshConnections: () => Promise<void>;
  createJob: (input: CreateJobInput) => Promise<MigrationJob | null>;
  startJob: (id: string) => Promise<void>;
  pauseJob: (id: string) => Promise<void>;
  resumeJob: (id: string) => Promise<void>;
  cancelJob: (id: string) => Promise<void>;
  retryJob: (id: string) => Promise<void>;
  createConnection: (input: CreateConnectionInput) => Promise<void>;
  testConnection: (id: string) => Promise<void>;
  deleteConnection: (id: string) => Promise<void>;
  saveSettings: (input: AppSettings) => Promise<void>;
  resetSessionData: () => void;
  dismissNotification: (id: string) => void;
}

const defaultLoading: LoadingState = {
  bootstrap: false,
  jobs: false,
  connections: false,
  jobsMutation: false,
  connectionsMutation: false,
  settings: false
};

function notification(tone: NotificationMessage["tone"], title: string, description: string): NotificationMessage {
  return { id: crypto.randomUUID(), tone, title, description };
}

export const useAppStore = create<AppState>((set, get) => ({
  jobs: [],
  connections: [],
  settings: null,
  notifications: [],
  loading: defaultLoading,

  bootstrap: async () => {
    set((state) => ({ loading: { ...state.loading, bootstrap: true } }));

    try {
      const [jobs, connections, settings] = await Promise.all([
        api.getJobs(),
        api.getConnections(),
        api.getSettings()
      ]);

      set({ jobs, connections, settings });
    } catch (error) {
      set((state) => ({
        jobs: [],
        connections: [],
        settings: state.settings ?? {
          concurrency: 4,
          retryLimit: 3,
          notifyOnFailure: true,
          telemetryEnabled: false
        },
        notifications: [
          notification(
            "error",
            "API not running",
            error instanceof Error
              ? error.message
              : `Start the backend API at ${getApiBaseUrl()} and refresh the page.`
          ),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, bootstrap: false } }));
    }
  },

  refreshJobs: async () => {
    set((state) => ({ loading: { ...state.loading, jobs: true } }));

    try {
      const jobs = await api.getJobs();
      set({ jobs });
    } finally {
      set((state) => ({ loading: { ...state.loading, jobs: false } }));
    }
  },

  refreshConnections: async () => {
    set((state) => ({ loading: { ...state.loading, connections: true } }));

    try {
      const connections = await api.getConnections();
      set({ connections });
    } finally {
      set((state) => ({ loading: { ...state.loading, connections: false } }));
    }
  },

  createJob: async (input) => {
    set((state) => ({ loading: { ...state.loading, jobsMutation: true } }));

    try {
      const job = await api.createJob(input);
      set((state) => ({
        jobs: [job, ...state.jobs],
        notifications: [
          notification("success", "Migration job created", `${job.name} has been added to the queue.`),
          ...state.notifications
        ]
      }));
      return job;
    } catch (error) {
      set((state) => ({
        notifications: [
          notification(
            "error",
            "Job creation failed",
            error instanceof Error ? error.message : "The migration job could not be created."
          ),
          ...state.notifications
        ]
      }));
      return null;
    } finally {
      set((state) => ({ loading: { ...state.loading, jobsMutation: false } }));
    }
  },

  startJob: async (id) => {
    set((state) => ({ loading: { ...state.loading, jobsMutation: true } }));

    try {
      await api.startMigration(id);
      const jobs = await api.getJobs();
      set((state) => ({
        jobs,
        notifications: [
          notification("success", "Migration started", "The selected migration is now running."),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification(
            "error",
            "Migration start failed",
            error instanceof Error ? error.message : "The selected migration could not be started."
          ),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, jobsMutation: false } }));
    }
  },

  pauseJob: async (id) => {
    set((state) => ({ loading: { ...state.loading, jobsMutation: true } }));

    try {
      await api.pauseMigration(id);
      const jobs = await api.getJobs();
      set((state) => ({
        jobs,
        notifications: [
          notification("info", "Migration paused", "The selected migration has been paused."),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification(
            "error",
            "Migration pause failed",
            error instanceof Error ? error.message : "The selected migration could not be paused."
          ),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, jobsMutation: false } }));
    }
  },

  resumeJob: async (id) => {
    set((state) => ({ loading: { ...state.loading, jobsMutation: true } }));

    try {
      await api.resumeMigration(id);
      const jobs = await api.getJobs();
      set((state) => ({
        jobs,
        notifications: [
          notification("success", "Migration resumed", "The selected migration has been requeued."),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification("error", "Migration resume failed", error instanceof Error ? error.message : "The selected migration could not be resumed."),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, jobsMutation: false } }));
    }
  },

  cancelJob: async (id) => {
    set((state) => ({ loading: { ...state.loading, jobsMutation: true } }));

    try {
      await api.cancelMigration(id);
      const jobs = await api.getJobs();
      set((state) => ({
        jobs,
        notifications: [
          notification("info", "Migration cancelled", "The selected migration has been cancelled."),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification("error", "Migration cancel failed", error instanceof Error ? error.message : "The selected migration could not be cancelled."),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, jobsMutation: false } }));
    }
  },

  retryJob: async (id) => {
    set((state) => ({ loading: { ...state.loading, jobsMutation: true } }));

    try {
      await api.retryMigration(id);
      const jobs = await api.getJobs();
      set((state) => ({
        jobs,
        notifications: [
          notification("success", "Migration retry queued", "The selected migration has been queued for retry."),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification("error", "Migration retry failed", error instanceof Error ? error.message : "The selected migration could not be retried."),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, jobsMutation: false } }));
    }
  },

  createConnection: async (input) => {
    set((state) => ({ loading: { ...state.loading, connectionsMutation: true } }));

    try {
      const connection = await api.createConnection(input);
      set((state) => ({
        connections: [connection, ...state.connections],
        notifications: [
          notification("success", "Connection saved", `${connection.name} is now available for migrations.`),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification(
            "error",
            "Connection save failed",
            error instanceof Error ? error.message : "The connection could not be saved."
          ),
          ...state.notifications
        ]
      }));
      throw error;
    } finally {
      set((state) => ({ loading: { ...state.loading, connectionsMutation: false } }));
    }
  },

  testConnection: async (id) => {
    const connection = get().connections.find((item) => item.id === id);

    if (!connection) {
      return;
    }

    let result: ConnectionTestResult;
    try {
      result = await api.testConnection(id);
    } catch (error) {
      set((state) => ({
        notifications: [
          notification(
            "error",
            "Connection test failed",
            error instanceof Error ? error.message : "The selected connection could not be tested."
          ),
          ...state.notifications
        ]
      }));
      return;
    }

    const existing = get().connections.filter((item) => item.id !== id);
    set((state) => ({
      connections: [
        {
          ...connection,
          status: result.isSuccess ? "Healthy" : "Warning",
          lastChecked: result.testedAt,
          lastTestMessage: result.message
        },
        ...existing
      ],
      notifications: [
        notification(
          result.isSuccess ? "success" : "info",
          "Connection tested",
          result.isSuccess
            ? `${connection.name}: ${result.message}`
            : formatErrorForToast(`${connection.name}: ${result.message}`)
        ),
        ...state.notifications
      ]
    }));
  },

  deleteConnection: async (id) => {
    const connection = get().connections.find((item) => item.id === id);
    set((state) => ({ loading: { ...state.loading, connectionsMutation: true } }));

    try {
      await api.deleteConnection(id);
      set((state) => ({
        connections: state.connections.filter((item) => item.id !== id),
        notifications: [
          notification(
            "success",
            "Connection deleted",
            connection ? `${connection.name} was removed from your workspace.` : "The connection was removed."
          ),
          ...state.notifications
        ]
      }));
    } catch (error) {
      set((state) => ({
        notifications: [
          notification(
            "error",
            "Connection delete failed",
            error instanceof Error ? error.message : "The selected connection could not be deleted."
          ),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, connectionsMutation: false } }));
    }
  },

  saveSettings: async (input) => {
    set((state) => ({ loading: { ...state.loading, settings: true } }));

    try {
      const settings = await api.saveSettings(input);
      set((state) => ({
        settings,
        notifications: [
          notification("success", "Settings updated", "Execution defaults have been saved."),
          ...state.notifications
        ]
      }));
    } finally {
      set((state) => ({ loading: { ...state.loading, settings: false } }));
    }
  },

  dismissNotification: (id) =>
    set((state) => ({
      notifications: state.notifications.filter((message) => message.id !== id)
    })),

  resetSessionData: () => {
    api.clearSessionCache();
    set({
      jobs: [],
      connections: [],
      settings: null,
      notifications: [],
      loading: defaultLoading
    });
  }
}));
