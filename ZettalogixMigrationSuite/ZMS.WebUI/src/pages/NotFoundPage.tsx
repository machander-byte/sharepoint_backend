import { Link } from "react-router-dom";
import PageHeader from "../components/PageHeader";

export default function NotFoundPage(): JSX.Element {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Page Not Found"
        subtitle="The requested ZMS workspace route does not exist or is no longer available."
        actions={
          <Link className="rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90" to="/dashboard">
            Back to dashboard
          </Link>
        }
      />
      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <h2 className="text-lg font-bold text-text-primary">Check the route</h2>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-text-muted">
          Use the sidebar to open discovery, readiness, planning, migration jobs, validation, reports, governance, or AI advisor pages.
        </p>
      </section>
    </div>
  );
}
