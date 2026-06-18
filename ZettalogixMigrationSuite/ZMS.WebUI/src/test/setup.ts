import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn()
  }))
});

globalThis.fetch = vi.fn().mockResolvedValue({
  ok: true,
  json: async () => ({
    status: "Healthy",
    databaseStartup: { status: "Ready" },
    database: { healthy: true, provider: "Test" },
    queue: { pendingCount: 0 }
  })
});
