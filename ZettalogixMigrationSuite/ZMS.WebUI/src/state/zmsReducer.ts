import { connections as defaultConnections, siteCollections } from "../data/zmsMockData";
import {
  BuilderOptions,
  Connection,
  DiscoveryScanResult,
  EnvironmentConfig,
  GeneratedPackageResult,
  GeneratedReport,
  PackageGenerationStatus,
  TenantValues,
  ToastNotification
} from "../types/zms";
import { ZmsAction } from "./zmsActions";

export interface ZmsState {
  selectedSiteCollectionIds: string[];
  builderOptions: BuilderOptions;
  tenantValues: TenantValues;
  generatedEnvironmentConfig: EnvironmentConfig | null;
  generatedPackages: GeneratedPackageResult[];
  lastGeneratedPackage: GeneratedPackageResult | null;
  packageGenerationStatus: PackageGenerationStatus;
  validationWarnings: string[];
  validationErrors: string[];
  connections: Connection[];
  discovery: {
    status: "idle" | "running" | "completed" | "failed";
    progress: number;
    result: DiscoveryScanResult | null;
  };
  generatedReports: GeneratedReport[];
  toasts: ToastNotification[];
}

export const defaultBuilderOptions: BuilderOptions = {
  includeDefaultSubsites: true,
  generateSampleDocuments: true,
  includeMetadataColumns: true,
  createPermissionGroups: true,
  addMigrationEdgeCases: true,
  includeArchivedFolders: false,
  includeLongPathExamples: false,
  includeLargeFilePlaceholders: false
};

export const defaultTenantValues: TenantValues = {
  tenantName: "Zettalogix SharePoint Online",
  adminUrl: "https://zettalogix-admin.sharepoint.com",
  rootUrl: "https://zettalogix.sharepoint.com",
  ownerEmail: "migrationlead@zettalogix.com",
  clientIdPlaceholder: "00000000-0000-0000-0000-000000000000",
  targetUrlPrefix: "https://zettalogix.sharepoint.com/sites/",
  generatedBy: "System Architect"
};

export const defaultZmsState: ZmsState = {
  selectedSiteCollectionIds: siteCollections.map((site) => site.id),
  builderOptions: defaultBuilderOptions,
  tenantValues: defaultTenantValues,
  generatedEnvironmentConfig: null,
  generatedPackages: [],
  lastGeneratedPackage: null,
  packageGenerationStatus: "idle",
  validationWarnings: [],
  validationErrors: [],
  connections: defaultConnections,
  discovery: {
    status: "idle",
    progress: 0,
    result: null
  },
  generatedReports: [],
  toasts: []
};

export function zmsReducer(state: ZmsState, action: ZmsAction): ZmsState {
  switch (action.type) {
    case "SET_SELECTED_SITE_COLLECTIONS":
      return { ...state, selectedSiteCollectionIds: action.payload };

    case "TOGGLE_SITE_COLLECTION": {
      const selected = state.selectedSiteCollectionIds.includes(action.payload)
        ? state.selectedSiteCollectionIds.filter((id) => id !== action.payload)
        : [...state.selectedSiteCollectionIds, action.payload];
      return { ...state, selectedSiteCollectionIds: selected };
    }

    case "SET_BUILDER_OPTIONS":
      return { ...state, builderOptions: action.payload };

    case "SET_BUILDER_OPTION":
      return {
        ...state,
        builderOptions: {
          ...state.builderOptions,
          [action.payload.key]: action.payload.value
        }
      };

    case "SET_TENANT_VALUES":
      return {
        ...state,
        tenantValues: {
          ...state.tenantValues,
          ...action.payload
        }
      };

    case "SET_ENVIRONMENT_CONFIG":
      return { ...state, generatedEnvironmentConfig: action.payload };

    case "GENERATE_CONFIG_STARTED":
      return {
        ...state,
        packageGenerationStatus: "running",
        validationErrors: [],
        validationWarnings: []
      };

    case "VALIDATION_STARTED":
      return {
        ...state,
        packageGenerationStatus: "running",
        validationErrors: [],
        validationWarnings: []
      };

    case "VALIDATION_SUCCEEDED":
      return {
        ...state,
        validationWarnings: action.payload.warnings,
        validationErrors: action.payload.errors ?? [],
        packageGenerationStatus: action.payload.warnings.length > 0 ? "warning" : "running"
      };

    case "VALIDATION_FAILED":
      return {
        ...state,
        validationErrors: action.payload.errors,
        validationWarnings: action.payload.warnings ?? [],
        packageGenerationStatus: "error"
      };

    case "PACKAGE_GENERATION_STARTED":
      return { ...state, packageGenerationStatus: "running" };

    case "PACKAGE_GENERATION_SUCCEEDED":
      return {
        ...state,
        packageGenerationStatus: action.payload.source === "mock" ? "warning" : "success",
        lastGeneratedPackage: action.payload,
        generatedPackages: [action.payload, ...state.generatedPackages.filter((pkg) => pkg.packageId !== action.payload.packageId)].slice(0, 20)
      };

    case "PACKAGE_GENERATION_FAILED":
      return {
        ...state,
        packageGenerationStatus: "error",
        validationErrors: [action.payload.error]
      };

    case "PACKAGE_DOWNLOAD_STARTED":
      return { ...state, packageGenerationStatus: "running" };

    case "PACKAGE_DOWNLOAD_SUCCEEDED":
      return { ...state, packageGenerationStatus: state.lastGeneratedPackage?.source === "mock" ? "warning" : "success" };

    case "PACKAGE_DOWNLOAD_FAILED":
      return {
        ...state,
        packageGenerationStatus: "error",
        validationErrors: [action.payload.error]
      };

    case "SET_PACKAGE_GENERATION_STATUS":
      return { ...state, packageGenerationStatus: action.payload };

    case "RESET_BUILDER":
      return {
        ...state,
        selectedSiteCollectionIds: defaultZmsState.selectedSiteCollectionIds,
        builderOptions: defaultBuilderOptions,
        tenantValues: defaultTenantValues,
        generatedEnvironmentConfig: null,
        lastGeneratedPackage: null,
        packageGenerationStatus: "idle",
        validationErrors: [],
        validationWarnings: [],
        discovery: defaultZmsState.discovery
      };

    case "SET_CONNECTIONS":
      return { ...state, connections: action.payload };

    case "UPSERT_CONNECTION": {
      const exists = state.connections.some((connection) => connection.id === action.payload.id);
      return {
        ...state,
        connections: exists
          ? state.connections.map((connection) => (connection.id === action.payload.id ? action.payload : connection))
          : [action.payload, ...state.connections]
      };
    }

    case "UPDATE_CONNECTION":
      return {
        ...state,
        connections: state.connections.map((connection) =>
          connection.id === action.payload.id ? { ...connection, ...action.payload.patch } : connection
        )
      };

    case "SET_DISCOVERY_PROGRESS":
      return {
        ...state,
        discovery: {
          ...state.discovery,
          status: action.payload.status,
          progress: action.payload.progress
        }
      };

    case "SET_DISCOVERY_RESULT":
      return {
        ...state,
        discovery: {
          status:
            action.payload?.status === "completed" || action.payload?.status === "partial"
              ? "completed"
              : action.payload?.status === "failed" || action.payload?.status === "cancelled"
                ? "failed"
                : action.payload
                  ? "running"
                  : "idle",
          progress: action.payload?.status === "completed" || action.payload?.status === "partial" ? 100 : 0,
          result: action.payload
        }
      };

    case "ADD_GENERATED_REPORT":
      return { ...state, generatedReports: [action.payload, ...state.generatedReports] };

    case "ADD_TOAST":
      return { ...state, toasts: [action.payload, ...state.toasts].slice(0, 5) };

    case "REMOVE_TOAST":
      return { ...state, toasts: state.toasts.filter((toast) => toast.id !== action.payload) };

    default:
      return state;
  }
}
