import { Navigate, Route, Routes } from "react-router-dom";
import RequireAuth from "../components/auth/RequireAuth";
import AppLayout from "../layout/AppLayout";
import AIRecommendationsPage from "../pages/AIRecommendationsPage";
import AuthCallbackPage from "../pages/AuthCallbackPage";
import AuthPage from "../pages/AuthPage";
import ConnectionsPage from "../pages/ConnectionsPage";
import CopilotReadinessPage from "../pages/CopilotReadinessPage";
import DashboardPage from "../pages/DashboardPage";
import DiscoveryPage from "../pages/DiscoveryPage";
import EnvironmentBuilderPage from "../pages/EnvironmentBuilderPage";
import HelpCenterPage from "../pages/HelpCenterPage";
import JobsPage from "../pages/JobsPage";
import MetadataPage from "../pages/MetadataPage";
import MigrationDetailPage from "../pages/MigrationDetailPage";
import MigrationsPage from "../pages/MigrationsPage";
import MigrationPlannerPage from "../pages/MigrationPlannerPage";
import ModernizationPage from "../pages/ModernizationPage";
import OperatorControlCenterPage from "../pages/OperatorControlCenterPage";
import PackageCenterPage from "../pages/PackageCenterPage";
import PermissionsPage from "../pages/PermissionsPage";
import ReportsPage from "../pages/ReportsPage";
import SettingsPage from "../pages/SettingsPage";
import SiteCollectionDetailPage from "../pages/SiteCollectionDetailPage";
import TeamsDiscoveryPage from "../pages/TeamsDiscoveryPage";
import ValidationPage from "../pages/ValidationPage";
import V2App from "../ui-v2/V2App";

export default function App(): JSX.Element {
  return (
    <Routes>
      <Route path="/login" element={<AuthPage />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route element={<RequireAuth />}>
        <Route path="/v2/*" element={<V2App />} />
        <Route element={<AppLayout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/environment" element={<EnvironmentBuilderPage />} />
          <Route path="/environment/:siteId" element={<SiteCollectionDetailPage />} />
          <Route path="/connections" element={<ConnectionsPage />} />
          <Route path="/discovery" element={<DiscoveryPage />} />
          <Route path="/planner" element={<MigrationPlannerPage />} />
          <Route path="/operator" element={<OperatorControlCenterPage />} />
          <Route path="/permissions" element={<PermissionsPage />} />
          <Route path="/metadata" element={<MetadataPage />} />
          <Route path="/modernization" element={<ModernizationPage />} />
          <Route path="/copilot-readiness" element={<CopilotReadinessPage />} />
          <Route path="/teams" element={<TeamsDiscoveryPage />} />
          <Route path="/migrations" element={<MigrationsPage />} />
          <Route path="/migrations/:id" element={<MigrationDetailPage />} />
          <Route path="/jobs" element={<JobsPage />} />
          <Route path="/validation" element={<ValidationPage />} />
          <Route path="/packages" element={<PackageCenterPage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/ai" element={<AIRecommendationsPage />} />
          <Route path="/help" element={<HelpCenterPage />} />
          <Route path="/settings" element={<SettingsPage />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Route>
    </Routes>
  );
}
