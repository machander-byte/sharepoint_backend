import { Bot, CalendarDays, ListTree, Tags, UserRound } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import DataTable from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import StatCard from "../components/StatCard";
import { metadataMappings } from "../data/zmsMockData";
import { zmsApi } from "../services/zmsApi";
import { useZmsState } from "../state/ZmsStateProvider";
import { MetadataFinding, RiskLevel } from "../types/zms";

function mockMetadataFindings(): MetadataFinding[] {
  return metadataMappings.map((mapping) => ({
    id: mapping.id,
    site: "Mock Environment",
    library: mapping.usedIn,
    fieldName: mapping.sourceField,
    fieldType: mapping.fieldType,
    required: mapping.mappingStatus === "Unmapped" || mapping.mappingStatus === "Conflict",
    missingValueCount: mapping.issue ? 12 : 0,
    mappedTargetField: mapping.targetField,
    mappingRisk: mapping.mappingStatus === "Conflict" || mapping.mappingStatus === "Unmapped" ? "High" : mapping.mappingStatus === "Suggested" ? "Medium" : "Low"
  }));
}

export default function MetadataPage(): JSX.Element {
  const state = useZmsState();
  const [findings, setFindings] = useState<MetadataFinding[]>(state.discovery.result?.metadataFindings ?? mockMetadataFindings());
  const [siteFilter, setSiteFilter] = useState("All");
  const [fieldTypeFilter, setFieldTypeFilter] = useState("All");
  const [riskFilter, setRiskFilter] = useState<RiskLevel | "All">("All");

  useEffect(() => {
    let cancelled = false;
    if (state.discovery.result) {
      setFindings(state.discovery.result.metadataFindings);
      return;
    }

    zmsApi.getLatestDiscoveryMetadataFindings().then((result) => {
      if (!cancelled && result) {
        setFindings(result);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [state.discovery.result]);

  const sites = useMemo(() => ["All", ...Array.from(new Set(findings.map((finding) => finding.site))).sort()], [findings]);
  const fieldTypes = useMemo(() => ["All", ...Array.from(new Set(findings.map((finding) => finding.fieldType))).sort()], [findings]);
  const filteredFindings = findings.filter((finding) => {
    return (
      (siteFilter === "All" || finding.site === siteFilter) &&
      (fieldTypeFilter === "All" || finding.fieldType === fieldTypeFilter) &&
      (riskFilter === "All" || finding.mappingRisk === riskFilter)
    );
  });

  const choiceFields = findings.filter((finding) => finding.fieldType === "Choice").length;
  const personFields = findings.filter((finding) => finding.fieldType === "Person").length;
  const dateFields = findings.filter((finding) => finding.fieldType === "Date").length;
  const missingValues = findings.reduce((sum, finding) => sum + finding.missingValueCount, 0);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Metadata Mapping" subtitle="Standardize source metadata before migration." />

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard label="Total Fields" value={findings.length} icon={Tags} />
        <StatCard label="Choice Fields" value={choiceFields} icon={ListTree} />
        <StatCard label="Person Fields" value={personFields} icon={UserRound} />
        <StatCard label="Date Fields" value={dateFields} icon={CalendarDays} />
        <StatCard label="Missing Values" value={missingValues} icon={Tags} tone="warning" />
      </section>

      <section className="flex flex-wrap gap-3">
        <select className="rounded-lg border border-border bg-surface px-3 py-2 text-sm font-semibold" value={siteFilter} onChange={(event) => setSiteFilter(event.target.value)}>
          {sites.map((site) => (
            <option key={site} value={site}>
              {site}
            </option>
          ))}
        </select>
        <select className="rounded-lg border border-border bg-surface px-3 py-2 text-sm font-semibold" value={fieldTypeFilter} onChange={(event) => setFieldTypeFilter(event.target.value)}>
          {fieldTypes.map((fieldType) => (
            <option key={fieldType} value={fieldType}>
              {fieldType}
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
      </section>

      <section className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <DataTable
          rows={filteredFindings}
          getRowKey={(row) => row.id}
          columns={[
            { header: "Site", render: (row) => <span className="font-semibold">{row.site}</span> },
            { header: "Library", render: (row) => row.library },
            { header: "Field Name", render: (row) => <span className="font-mono font-semibold">{row.fieldName}</span> },
            { header: "Field Type", render: (row) => row.fieldType },
            { header: "Required", render: (row) => (row.required ? "Yes" : "No") },
            { header: "Missing Values", render: (row) => <span className="font-mono">{row.missingValueCount}</span> },
            { header: "Target Field", render: (row) => <span className="font-semibold text-primary">{row.mappedTargetField || "-"}</span> },
            { header: "Mapping Risk", render: (row) => <RiskBadge level={row.mappingRisk} /> }
          ]}
        />

        <aside className="h-fit rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-white">
              <Bot className="h-5 w-5" />
            </div>
            <h2 className="font-bold text-text-primary">AI Suggestion</h2>
          </div>
          <p className="mt-4 text-sm leading-6 text-text-muted">
            {findings.find((finding) => finding.mappingRisk === "High")?.fieldName ?? "Department"} should be cleaned before the next migration wave.
          </p>
          <div className="mt-5 rounded-xl bg-surface-container p-4">
            <div className="mb-2 flex justify-between text-sm">
              <span className="font-semibold text-text-muted">Readiness</span>
              <span className="font-mono font-bold text-primary">{Math.max(0, 100 - filteredFindings.length)}%</span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-surface-container-high">
              <div className="h-full rounded-full bg-primary" style={{ width: `${Math.max(0, 100 - filteredFindings.length)}%` }} />
            </div>
          </div>
        </aside>
      </section>
    </div>
  );
}
