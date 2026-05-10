import { useEffect } from "react";
import { useAuth } from "./useAuth";
import { useAppStore } from "./useAppStore";

export function useAppBootstrap(): void {
  const { user } = useAuth();
  const bootstrap = useAppStore((state) => state.bootstrap);
  const resetSessionData = useAppStore((state) => state.resetSessionData);

  useEffect(() => {
    if (!user?.id) {
      resetSessionData();
      return;
    }

    void bootstrap();
  }, [bootstrap, resetSessionData, user?.id]);
}
