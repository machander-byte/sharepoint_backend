import { createClient as createSupabaseClient, type SupabaseClient } from "@supabase/supabase-js";

let browserClient: SupabaseClient | null = null;
const authStorageKeyPattern = /^(sb-.+-auth-token|supabase\.auth\.token)$/i;

export function createClient() {
  if (!browserClient) {
    const supabaseUrl = (import.meta.env.VITE_SUPABASE_URL as string | undefined)?.trim();
    const supabaseKey = (import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY as string | undefined)?.trim();

    if (!supabaseUrl || !supabaseKey) {
      throw new Error("Supabase frontend URL and publishable key must be configured.");
    }

    browserClient = createSupabaseClient(supabaseUrl, supabaseKey, {
      auth: {
        autoRefreshToken: true,
        detectSessionInUrl: true,
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
