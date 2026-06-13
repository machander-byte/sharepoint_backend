import { useState } from "react";
import { Outlet } from "react-router-dom";
import ToastContainer from "../components/ToastContainer";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";

export default function AppLayout(): JSX.Element {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const demoMode = import.meta.env.VITE_DEMO_MODE === "true";

  return (
    <div className="min-h-screen w-full bg-background text-text-primary">
      <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div className="min-w-0 md:pl-[280px]">
        <Topbar onMenuClick={() => setSidebarOpen(true)} />
        {demoMode ? (
          <div className="fixed left-0 right-0 top-16 z-30 border-y border-warning bg-warning px-4 py-2 text-center text-xs font-bold uppercase tracking-wide text-white md:left-[280px]">
            Demo Mode - No tenant changes are performed
          </div>
        ) : null}
        <main className={`mx-auto flex w-full max-w-[1680px] flex-col gap-6 px-4 pb-12 ${demoMode ? "pt-32" : "pt-24"} sm:px-6 lg:px-8`}>
          <Outlet />
        </main>
      </div>
      <ToastContainer />
    </div>
  );
}
