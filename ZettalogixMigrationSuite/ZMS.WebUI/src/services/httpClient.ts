import { BackendApiError } from "../types/zms";
import { createClient } from "../lib/client";

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/+$/, "");
const DEFAULT_TIMEOUT_MS = 20_000;

export function hasBackendBaseUrl(): boolean {
  return Boolean(API_BASE_URL);
}

function buildUrl(path: string): string {
  if (!API_BASE_URL) {
    throw { message: "VITE_API_BASE_URL is not configured." } satisfies BackendApiError;
  }
  return `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}

async function request<TResponse>(path: string, init: RequestInit = {}): Promise<TResponse> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), DEFAULT_TIMEOUT_MS);
  const isFormData = typeof FormData !== "undefined" && init.body instanceof FormData;
  const authHeaders = await getAuthHeaders();

  try {
    const response = await fetch(buildUrl(path), {
      ...init,
      signal: controller.signal,
      headers: {
        Accept: "application/json",
        ...(init.body && !isFormData ? { "Content-Type": "application/json" } : {}),
        ...authHeaders,
        ...init.headers
      }
    });

    if (!response.ok) {
      let details: unknown;
      try {
        details = await response.json();
      } catch {
        details = await response.text();
      }
      throw {
        status: response.status,
        message: `Request failed with status ${response.status}`,
        details
      } satisfies BackendApiError;
    }

    if (response.status === 204) {
      return undefined as TResponse;
    }

    return (await response.json()) as TResponse;
  } catch (error) {
    if ((error as DOMException).name === "AbortError") {
      throw { message: "Backend request timed out." } satisfies BackendApiError;
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}

export function apiGet<TResponse>(path: string): Promise<TResponse> {
  return request<TResponse>(path, { method: "GET" });
}

export function apiPost<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  return request<TResponse>(path, {
    method: "POST",
    body: JSON.stringify(body)
  });
}

export function apiPut<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  return request<TResponse>(path, {
    method: "PUT",
    body: JSON.stringify(body)
  });
}

export function apiPostForm<TResponse>(path: string, body: FormData): Promise<TResponse> {
  return request<TResponse>(path, {
    method: "POST",
    body,
    headers: {
      Accept: "application/json"
    }
  });
}

export async function apiGetBlob(path: string): Promise<Blob> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), DEFAULT_TIMEOUT_MS);

  try {
    const response = await fetch(buildUrl(path), {
      method: "GET",
      signal: controller.signal,
      headers: await getAuthHeaders()
    });

    if (!response.ok) {
      throw {
        status: response.status,
        message: `Request failed with status ${response.status}`
      } satisfies BackendApiError;
    }

    return await response.blob();
  } catch (error) {
    if ((error as DOMException).name === "AbortError") {
      throw { message: "Backend request timed out." } satisfies BackendApiError;
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}

async function getAuthHeaders(): Promise<Record<string, string>> {
  const supabaseUrl = import.meta.env.VITE_SUPABASE_URL as string | undefined;
  const supabaseKey = import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY as string | undefined;
  if (!supabaseUrl || !supabaseKey) {
    return {};
  }

  try {
    const supabase = createClient();
    const { data } = await supabase.auth.getSession();
    const token = data.session?.access_token;
    return token ? { Authorization: `Bearer ${token}` } : {};
  } catch {
    return {};
  }
}
