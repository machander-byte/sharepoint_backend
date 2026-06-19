import { createClient as createSupabaseClient, type SupabaseClient } from "@supabase/supabase-js";

let browserClient: SupabaseClient | null = null;
const authStorageKeyPattern = /^(sb-.+-auth-token.*|supabase\.auth\.token)$/i;

export function createClient() {
  if (!browserClient) {
    const supabaseUrl = normalizeFrontendEnvValue(import.meta.env.VITE_SUPABASE_URL as string | undefined);
    const supabaseKey =
      normalizeFrontendEnvValue(import.meta.env.VITE_SUPABASE_ANON_KEY as string | undefined) ||
      normalizeFrontendEnvValue(import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY as string | undefined);

    if (!supabaseUrl || !supabaseKey) {
      throw new Error("Supabase frontend URL and public auth key must be configured.");
    }

    browserClient = createSupabaseClient(supabaseUrl, supabaseKey, {
      auth: {
        autoRefreshToken: true,
        detectSessionInUrl: false,
        flowType: "implicit",
        persistSession: true
      }
    });
  }

  return browserClient;
}

export function clearSupabaseAuthStorage(): void {
  if (typeof window === "undefined") {
    return;
  }

  clearStorage(window.localStorage);
  clearStorage(window.sessionStorage);
}

function clearStorage(storage: Storage): void {
  const keysToRemove: string[] = [];

  for (let index = 0; index < storage.length; index++) {
    const key = storage.key(index);
    if (key && authStorageKeyPattern.test(key)) {
      keysToRemove.push(key);
    }
  }

  for (const key of keysToRemove) {
    storage.removeItem(key);
  }
}

function normalizeFrontendEnvValue(value: string | undefined): string {
  return (value ?? "").trim().replace(/[^\x20-\x7E]/g, "");
}
