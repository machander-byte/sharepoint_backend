import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import RequireAuth from "../components/auth/RequireAuth";
import V2App from "../ui-v2/V2App";

const signOut = vi.fn();
let mockSession: unknown = { user: { id: "reviewer", email: "reviewer@example.com" } };

vi.mock("../hooks/useAuth", () => ({
  useAuth: () => ({
    loading: false,
    session: mockSession,
    user: mockSession ? { id: "reviewer", email: "reviewer@example.com" } : null,
    signOut
  })
}));

function renderV2(path = "/v2/command-center") {
  window.localStorage.setItem("zms_onboarding_completed", "true");

  return render(
    <MemoryRouter initialEntries={[path]}>
      <V2App />
    </MemoryRouter>
  );
}

describe("V2 review shell", () => {
  beforeEach(() => {
    mockSession = { user: { id: "reviewer", email: "reviewer@example.com" } };
  });

  it("redirects unauthenticated users to login before protected content renders", async () => {
    mockSession = null;

    render(
      <MemoryRouter initialEntries={["/v2"]}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/v2" element={<div>Protected V2 content</div>} />
          </Route>
          <Route path="/login" element={<div>Reviewer login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText("Reviewer login")).toBeInTheDocument();
    expect(screen.queryByText("Protected V2 content")).not.toBeInTheDocument();
  });

  it("renders the V2 sidebar and topbar for an authenticated reviewer", async () => {
    renderV2();

    expect(await screen.findByText("Zettalogix")).toBeInTheDocument();
    expect(screen.getAllByText("Command Center").length).toBeGreaterThan(0);
    expect(screen.getByText("Operate")).toBeInTheDocument();
    expect(screen.getByText("Prepare")).toBeInTheDocument();
    expect(screen.getByText("Assure")).toBeInTheDocument();
  });

  it("shows runtime status separately from historical validation evidence", async () => {
    renderV2("/v2/monitor");

    await waitFor(() => {
      expect(screen.getAllByText("Monitor").length).toBeGreaterThan(0);
      expect(screen.getByText("Operator monitoring")).toBeInTheDocument();
    });

    expect(screen.getAllByText(/Adapter|Healthy/).length).toBeGreaterThan(0);
    expect(screen.getByText("Historical validation evidence")).toBeInTheDocument();
  });
});
