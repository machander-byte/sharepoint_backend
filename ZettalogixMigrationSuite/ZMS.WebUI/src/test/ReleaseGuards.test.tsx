import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import NotFoundPage from "../pages/NotFoundPage";

afterEach(() => {
  vi.unstubAllEnvs();
  vi.resetModules();
});

describe("release guards", () => {
  it("renders a useful protected-workspace not-found page", () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: "Page Not Found" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to dashboard" })).toHaveAttribute("href", "/dashboard");
  });

  it("normalizes the configured API origin", async () => {
    vi.stubEnv("VITE_API_BASE_URL", "https://api.example.test///");

    const { getApiBaseUrl, getReportDownloadUrl } = await import("../services/api");

    expect(getApiBaseUrl()).toBe("https://api.example.test");
    expect(getReportDownloadUrl("/summary.csv")).toBe("https://api.example.test/api/reports/summary.csv");
  });

  it("fails clearly when the API origin is missing", async () => {
    vi.stubEnv("VITE_API_BASE_URL", "");

    const { getApiBaseUrl, getReportDownloadUrl } = await import("../services/api");

    expect(getApiBaseUrl()).toBe("VITE_API_BASE_URL not configured");
    expect(() => getReportDownloadUrl("/summary.csv")).toThrow("VITE_API_BASE_URL is not configured");
  });
});
