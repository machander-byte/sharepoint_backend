import { createClient as createSupabaseClient, type SupabaseClient } from "@supabase/supabase-js";

let browserClient: SupabaseClient | null = null;

export function createClient() {
  if (!browserClient) {
    const supabaseUrl = (import.meta.env.VITE_SUPABASE_URL as string | undefined)?.trim();
    const supabaseKey = (import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY as string | undefined)?.trim();

    if (!supabaseUrl || !supabaseKey) {
      throw new Error("Supabase frontend URL and publishable key must be configured.");
    }

    browserClient = createSupabaseClient(supabaseUrl, supabaseKey);
  }

  return browserClient;
}
