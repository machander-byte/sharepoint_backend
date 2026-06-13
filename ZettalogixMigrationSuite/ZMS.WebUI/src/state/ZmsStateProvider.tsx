import { createContext, Dispatch, PropsWithChildren, useContext, useEffect, useMemo, useReducer } from "react";
import { zmsReducer, defaultZmsState, ZmsState } from "./zmsReducer";
import { ZmsAction } from "./zmsActions";

const STORAGE_KEY = "zms.frontend.state.v1";

interface PersistedZmsState {
  selectedSiteCollectionIds?: ZmsState["selectedSiteCollectionIds"];
  builderOptions?: ZmsState["builderOptions"];
  tenantValues?: ZmsState["tenantValues"];
  generatedEnvironmentConfig?: ZmsState["generatedEnvironmentConfig"];
  generatedPackages?: ZmsState["generatedPackages"];
  lastGeneratedPackage?: ZmsState["lastGeneratedPackage"];
  connections?: ZmsState["connections"];
}

const ZmsStateContext = createContext<ZmsState | null>(null);
const ZmsDispatchContext = createContext<Dispatch<ZmsAction> | null>(null);

function loadInitialState(): ZmsState {
  if (typeof window === "undefined") {
    return defaultZmsState;
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return defaultZmsState;
    }

    const parsed = JSON.parse(raw) as PersistedZmsState;
    return {
      ...defaultZmsState,
      ...parsed,
      discovery: defaultZmsState.discovery,
      generatedReports: [],
      packageGenerationStatus: "idle",
      toasts: []
    };
  } catch {
    return defaultZmsState;
  }
}

export function ZmsStateProvider({ children }: PropsWithChildren): JSX.Element {
  const [state, dispatch] = useReducer(zmsReducer, undefined, loadInitialState);

  useEffect(() => {
    const persisted: PersistedZmsState = {
      selectedSiteCollectionIds: state.selectedSiteCollectionIds,
      builderOptions: state.builderOptions,
      tenantValues: state.tenantValues,
      generatedEnvironmentConfig: state.generatedEnvironmentConfig,
      generatedPackages: state.generatedPackages,
      lastGeneratedPackage: state.lastGeneratedPackage,
      connections: state.connections
    };
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(persisted));
  }, [
    state.builderOptions,
    state.connections,
    state.generatedEnvironmentConfig,
    state.generatedPackages,
    state.lastGeneratedPackage,
    state.selectedSiteCollectionIds,
    state.tenantValues
  ]);

  const stateValue = useMemo(() => state, [state]);

  return (
    <ZmsStateContext.Provider value={stateValue}>
      <ZmsDispatchContext.Provider value={dispatch}>{children}</ZmsDispatchContext.Provider>
    </ZmsStateContext.Provider>
  );
}

export function useZmsState(): ZmsState {
  const value = useContext(ZmsStateContext);
  if (!value) {
    throw new Error("useZmsState must be used within ZmsStateProvider");
  }
  return value;
}

export function useZmsDispatch(): Dispatch<ZmsAction> {
  const value = useContext(ZmsDispatchContext);
  if (!value) {
    throw new Error("useZmsDispatch must be used within ZmsStateProvider");
  }
  return value;
}
