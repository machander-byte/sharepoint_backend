import { useEffect } from "react";
import { useAppStore } from "./useAppStore";

export function useJobsPolling(enabled = true, intervalMs = 1800): void {
  const refreshJobs = useAppStore((state) => state.refreshJobs);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const timerId = window.setInterval(() => {
      void refreshJobs().catch(() => undefined);
    }, intervalMs);

    return () => window.clearInterval(timerId);
  }, [enabled, intervalMs, refreshJobs]);
}
