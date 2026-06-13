import {
  BuilderOptions,
  Connection,
  DiscoveryScanResult,
  EnvironmentConfig,
  GeneratedPackageResult,
  GeneratedReport,
  PackageGenerationStatus,
  TenantValues,
  ToastNotification,
  ToastTone
} from "../types/zms";

export type ZmsAction =
  | { type: "SET_SELECTED_SITE_COLLECTIONS"; payload: string[] }
  | { type: "TOGGLE_SITE_COLLECTION"; payload: string }
  | { type: "SET_BUILDER_OPTIONS"; payload: BuilderOptions }
  | { type: "SET_BUILDER_OPTION"; payload: { key: keyof BuilderOptions; value: boolean } }
  | { type: "SET_TENANT_VALUES"; payload: Partial<TenantValues> }
  | { type: "SET_ENVIRONMENT_CONFIG"; payload: EnvironmentConfig | null }
  | { type: "GENERATE_CONFIG_STARTED" }
  | { type: "VALIDATION_STARTED" }
  | { type: "VALIDATION_SUCCEEDED"; payload: { warnings: string[]; errors?: string[] } }
  | { type: "VALIDATION_FAILED"; payload: { errors: string[]; warnings?: string[] } }
  | { type: "PACKAGE_GENERATION_STARTED" }
  | { type: "PACKAGE_GENERATION_SUCCEEDED"; payload: GeneratedPackageResult }
  | { type: "PACKAGE_GENERATION_FAILED"; payload: { error: string } }
  | { type: "PACKAGE_DOWNLOAD_STARTED" }
  | { type: "PACKAGE_DOWNLOAD_SUCCEEDED" }
  | { type: "PACKAGE_DOWNLOAD_FAILED"; payload: { error: string } }
  | { type: "SET_PACKAGE_GENERATION_STATUS"; payload: PackageGenerationStatus }
  | { type: "RESET_BUILDER" }
  | { type: "SET_CONNECTIONS"; payload: Connection[] }
  | { type: "UPSERT_CONNECTION"; payload: Connection }
  | { type: "UPDATE_CONNECTION"; payload: { id: string; patch: Partial<Connection> } }
  | { type: "SET_DISCOVERY_PROGRESS"; payload: { status: "idle" | "running" | "completed" | "failed"; progress: number } }
  | { type: "SET_DISCOVERY_RESULT"; payload: DiscoveryScanResult | null }
  | { type: "ADD_GENERATED_REPORT"; payload: GeneratedReport }
  | { type: "ADD_TOAST"; payload: ToastNotification }
  | { type: "REMOVE_TOAST"; payload: string };

export function createToast(tone: ToastTone, title: string, description?: string): ToastNotification {
  return {
    id: typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`,
    tone,
    title,
    description
  };
}
