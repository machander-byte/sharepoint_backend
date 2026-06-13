import { Folder, FolderTree, List, LockKeyhole, Shield, Tags } from "lucide-react";
import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import DataTable from "../components/DataTable";
import EmptyState from "../components/EmptyState";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import StatCard from "../components/StatCard";
import StatusBadge from "../components/StatusBadge";
import { siteCollections } from "../data/zmsMockData";

const tabs = ["Structure", "Libraries", "Lists", "Metadata", "Permissions", "Edge Cases"] as const;
type Tab = (typeof tabs)[number];

export default function SiteCollectionDetailPage(): JSX.Element {
  const { siteId } = useParams();
  const [activeTab, setActiveTab] = useState<Tab>("Structure");
  const site = useMemo(() => siteCollections.find((item) => item.id === siteId), [siteId]);

  if (!site) {
    return <EmptyState title="Site collection not found" description="Select a site collection from the environment builder." />;
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title={site.name}
        subtitle={site.description}
        actions={<Link className="rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" to="/environment">Back to Environment</Link>}
      />

      <section className="grid grid-cols-2 gap-4 lg:grid-cols-6">
        <StatCard label="Subsites" value={site.subsites.length} icon={FolderTree} />
        <StatCard label="Libraries" value={site.libraries.length} icon={Folder} />
        <StatCard label="Lists" value={site.lists.length} icon={List} />
        <StatCard label="Metadata Fields" value={site.metadataFields.length} icon={Tags} />
        <StatCard label="Permission Groups" value={site.permissionGroups.length} icon={Shield} />
        <StatCard label="Edge Cases" value={site.edgeCases.length} icon={LockKeyhole} tone="warning" />
      </section>

      <section className="rounded-xl border border-border bg-surface p-4 shadow-card">
        <div className="flex gap-2 overflow-x-auto">
          {tabs.map((tab) => (
            <button
              key={tab}
              type="button"
              className={tab === activeTab
                ? "rounded-lg bg-primary px-3 py-2 text-sm font-bold text-white"
                : "rounded-lg px-3 py-2 text-sm font-semibold text-text-muted hover:bg-surface-container"}
              onClick={() => setActiveTab(tab)}
            >
              {tab}
            </button>
          ))}
        </div>
      </section>

      {activeTab === "Structure" ? (
        <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
          <h2 className="mb-5 text-lg font-bold text-text-primary">{site.name} Structure</h2>
          <div className="rounded-xl bg-surface-container p-5 font-mono text-sm leading-8 text-text-primary">
            <div>{site.name}</div>
            {site.subsites.map((subsite, index) => (
              <div key={subsite.id} className="pl-4">
                {index === site.subsites.length - 1 ? "└──" : "├──"} {subsite.name}
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {activeTab === "Libraries" ? (
        <DataTable
          rows={site.libraries}
          getRowKey={(row) => row.id}
          columns={[
            { header: "Library Name", render: (row) => <span className="font-semibold">{row.name}</span> },
            { header: "Type", render: (row) => row.type },
            { header: "Metadata Count", render: (row) => <span className="font-mono">{row.metadataCount}</span> },
            { header: "Files", render: (row) => <span className="font-mono">{row.files.toLocaleString()}</span> },
            { header: "Permission Status", render: (row) => row.permissionStatus },
            { header: "Risk Level", render: (row) => <RiskBadge level={row.riskLevel} /> }
          ]}
        />
      ) : null}

      {activeTab === "Lists" ? (
        <DataTable
          rows={site.lists}
          getRowKey={(row) => row.id}
          columns={[
            { header: "List Name", render: (row) => <span className="font-semibold">{row.name}</span> },
            { header: "Items", render: (row) => <span className="font-mono">{row.itemCount}</span> },
            { header: "Purpose", render: (row) => row.purpose }
          ]}
        />
      ) : null}

      {activeTab === "Metadata" ? (
        <DataTable
          rows={site.metadataFields}
          getRowKey={(row) => row.id}
          columns={[
            { header: "Field", render: (row) => <span className="font-semibold">{row.name}</span> },
            { header: "Type", render: (row) => row.type },
            { header: "Used In", render: (row) => row.usedIn },
            { header: "Required", render: (row) => (row.required ? "Yes" : "No") }
          ]}
        />
      ) : null}

      {activeTab === "Permissions" ? (
        <DataTable
          rows={site.permissionGroups}
          getRowKey={(row) => row.id}
          columns={[
            { header: "Group", render: (row) => <span className="font-semibold">{row.name}</span> },
            { header: "Role", render: (row) => row.role },
            { header: "Users", render: (row) => <span className="font-mono">{row.users}</span> }
          ]}
        />
      ) : null}

      {activeTab === "Edge Cases" ? (
        <DataTable
          rows={site.edgeCases}
          getRowKey={(row) => row.id}
          columns={[
            { header: "Edge Case", render: (row) => <span className="font-semibold">{row.title}</span> },
            { header: "Description", render: (row) => row.description },
            { header: "Risk", render: (row) => <RiskBadge level={row.riskLevel} /> }
          ]}
        />
      ) : null}
    </div>
  );
}
