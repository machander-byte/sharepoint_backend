import { Bot, KeyRound, Lock, ShieldCheck, UserPlus } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import DataTable from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import StatCard from "../components/StatCard";
import StatusBadge from "../components/StatusBadge";
import { permissionsRisks } from "../data/zmsMockData";
import { zmsApi } from "../services/zmsApi";
import { useZmsState } from "../state/ZmsStateProvider";
import { PermissionRiskFinding, RiskLevel } from "../types/zms";

function mockPermissionFindings(): PermissionRiskFinding[] {
  return permissionsRisks.map((risk) => ({
    id: risk.id,
    site: risk.site,
    libraryOrFolder: risk.location,
    inheritanceStatus: risk.inheritanceStatus,
    groups: risk.groups.split(",").map((group) => group.trim()),
    users: [String(risk.users)],
    accessLevels: [],
    riskLevel: risk.riskLevel,
    recommendedAction: risk.recommendedAction
  }));
}

export default function PermissionsPage(): JSX.Element {
  const state = useZmsState();
  const [findings, setFindings] = useState<PermissionRiskFinding[]>(state.discovery.result?.permissionRisks ?? mockPermissionFindings());
  const [siteFilter, setSiteFilter] = useState("All");
  const [riskFilter, setRiskFilter] = useState<RiskLevel | "All">("All");
  const [inheritanceFilter, setInheritanceFilter] = useState("All");

  useEffect(() => {
    let cancelled = false;
    if (state.discovery.result) {
      setFindings(state.discovery.result.permissionRisks);
      return;
    }

    zmsApi.getLatestDiscoveryPermissionRisks().then((result) => {
      if (!cancelled && result) {
        setFindings(result);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [state.discovery.result]);

  const sites = useMemo(() => ["All", ...Array.from(new Set(findings.map((finding) => finding.site))).sort()], [findings]);
  const filteredFindings = findings.filter((finding) => {
    return (
      (siteFilter === "All" || finding.site === siteFilter) &&
      (riskFilter === "All" || finding.riskLevel === riskFilter) &&
      (inheritanceFilter === "All" || finding.inheritanceStatus === inheritanceFilter)
    );
  });

  const brokenCount = findings.filter((finding) => finding.inheritanceStatus.toLowerCase().includes("broken")).length;
  const restrictedCount = findings.filter((finding) => finding.libraryOrFolder.toLowerCase().includes("restricted") || finding.riskLevel === "High").length;
  const externalUsers = findings.reduce(
    (sum, finding) => sum + finding.users.filter((user) => user.toLowerCase().includes("external") || user.toLowerCase().includes("#ext#")).length,
    0
  );
  const highRiskCount = findings.filter((finding) => finding.riskLevel === "High" || finding.riskLevel === "Critical").length;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Permissions Analysis"
        subtitle="Identify inheritance breaks, restricted content, and migration permission risks."
      />

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard label="Inherited Areas" value={Math.max(0, findings.length - brokenCount)} icon={ShieldCheck} />
        <StatCard label="Broken Inheritance" value={brokenCount} icon={KeyRound} tone="error" />
        <StatCard label="Restricted Areas" value={restrictedCount} icon={Lock} tone="warning" />
        <StatCard label="External Users" value={externalUsers} icon={UserPlus} />
        <StatCard label="High-Risk Areas" value={highRiskCount} icon={KeyRound} tone="error" />
      </section>

      <section className="rounded-xl border border-primary-muted bg-primary-soft/50 p-5 shadow-card">
        <div className="flex gap-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary text-white">
            <Bot className="h-5 w-5" />
          </div>
          <div>
            <h2 className="font-bold text-text-primary">AI Recommendation</h2>
            <p className="mt-1 text-sm leading-6 text-text-muted">
              {findings[0]?.recommendedAction ?? "Validate restricted access before migration."}
            </p>
          </div>
        </div>
      </section>

      <section className="flex flex-wrap gap-3">
        <select className="rounded-lg border border-border bg-surface px-3 py-2 text-sm font-semibold" value={siteFilter} onChange={(event) => setSiteFilter(event.target.value)}>
          {sites.map((site) => (
            <option key={site} value={site}>
              {site}
            </option>
          ))}
        </select>
        <select className="rounded-lg border border-border bg-surface px-3 py-2 text-sm font-semibold" value={riskFilter} onChange={(event) => setRiskFilter(event.target.value as RiskLevel | "All")}>
          {["All", "Low", "Medium", "High", "Critical"].map((risk) => (
            <option key={risk} value={risk}>
              {risk}
            </option>
          ))}
        </select>
        <select className="rounded-lg border border-border bg-surface px-3 py-2 text-sm font-semibold" value={inheritanceFilter} onChange={(event) => setInheritanceFilter(event.target.value)}>
          {["All", ...Array.from(new Set(findings.map((finding) => finding.inheritanceStatus))).sort()].map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </section>

      <DataTable
        rows={filteredFindings}
        getRowKey={(row) => row.id}
        columns={[
          { header: "Site", render: (row) => <span className="font-semibold">{row.site}</span> },
          { header: "Library / Folder", render: (row) => row.libraryOrFolder },
          { header: "Inheritance Status", render: (row) => <StatusBadge status={row.inheritanceStatus} /> },
          { header: "Groups", render: (row) => row.groups.join(", ") || "-" },
          { header: "Users", render: (row) => <span className="font-mono">{row.users.length > 1 ? row.users.length : row.users[0] ?? 0}</span> },
          { header: "Access Levels", render: (row) => row.accessLevels.join(", ") || "-" },
          { header: "Risk Level", render: (row) => <RiskBadge level={row.riskLevel} /> },
          { header: "Recommended Action", render: (row) => <span className="font-semibold text-primary">{row.recommendedAction}</span> }
        ]}
      />
    </div>
  );
}
