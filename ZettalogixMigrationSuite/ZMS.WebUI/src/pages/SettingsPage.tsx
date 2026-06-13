import { Bot, FileDown, KeyRound, Settings2, ShieldCheck, Users } from "lucide-react";
import PageHeader from "../components/PageHeader";

const sections = [
  { title: "Tenant Configuration", icon: Settings2, fields: ["Default tenant", "Target site prefix", "Workspace region"] },
  { title: "Microsoft Graph Permissions", icon: KeyRound, fields: ["Sites.Read.All", "Files.ReadWrite.All", "Group.Read.All"] },
  { title: "Default Migration Options", icon: ShieldCheck, fields: ["Preserve permissions", "Preserve metadata", "Include versions"] },
  { title: "Report Export Settings", icon: FileDown, fields: ["Default format", "Include raw JSON", "Retention window"] },
  { title: "AI Settings", icon: Bot, fields: ["Recommendation confidence threshold", "Metadata suggestions", "Modernization analysis"] },
  { title: "User & Role Management", icon: Users, fields: ["Migration Lead", "Report Viewer", "Environment Admin"] }
];

export default function SettingsPage(): JSX.Element {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Settings" subtitle="Configure tenant defaults, permissions, reports, AI options, and user roles." />

      <section className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        {sections.map((section) => {
          const Icon = section.icon;
          return (
            <article key={section.title} className="rounded-xl border border-border bg-surface p-5 shadow-card">
              <div className="mb-5 flex items-center gap-3 border-b border-border pb-4">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                  <Icon className="h-5 w-5" />
                </div>
                <h2 className="font-bold text-text-primary">{section.title}</h2>
              </div>
              <div className="space-y-4">
                {section.fields.map((field, index) => (
                  <label key={field} className="block">
                    <span className="mb-2 block text-sm font-semibold text-text-muted">{field}</span>
                    {index === 0 ? (
                      <input className="w-full rounded-lg border border-border px-3 py-2" defaultValue={field.includes("tenant") ? "zettalogix.sharepoint.com" : "Enabled"} />
                    ) : (
                      <select className="w-full rounded-lg border border-border px-3 py-2" defaultValue="Enabled">
                        <option>Enabled</option>
                        <option>Disabled</option>
                        <option>Review Required</option>
                      </select>
                    )}
                  </label>
                ))}
              </div>
            </article>
          );
        })}
      </section>
    </div>
  );
}
