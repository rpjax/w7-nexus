import { Navigate, Route, Routes } from 'react-router-dom';
import { RedirectIfAuthenticated, RequireAuth } from './auth/RequireAuth';
import { RequireAnyRole, RequireRole } from './auth/RequireRole';
import { ROLES } from './auth/roles';
import { DashboardLayout } from './layouts/DashboardLayout';
import { AdminOperationsPage } from './pages/admin/AdminOperationsPage';
import { AuthPage } from './pages/AuthPage';
import { HomePage } from './pages/HomePage';
import { OperatorOperationsPage } from './pages/operator/OperatorOperationsPage';
import { AccountsPage } from './pages/AccountsPage';
import { PaymentsListPage } from './pages/PaymentsListPage';
import { PaymentsPixPage } from './pages/PaymentsPixPage';
import { GatewaysHubPage } from './pages/GatewaysHubPage';
import { GatewayCredentialsPage } from './pages/GatewayCredentialsPage';
import { GatewayPlaceholderPage } from './pages/GatewayPlaceholderPage';

export default function App() {
  return (
    <Routes>
      <Route element={<RedirectIfAuthenticated />}>
        <Route path="/auth" element={<AuthPage />} />
      </Route>

      <Route element={<RequireAuth />}>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route element={<DashboardLayout />}>
          <Route path="/dashboard" element={<HomePage />} />

          <Route element={<RequireAnyRole roles={[ROLES.Operator, ROLES.Administrator]} />}>
            <Route path="/dashboard/operations" element={<OperatorOperationsPage />} />
            <Route path="/dashboard/payments" element={<PaymentsListPage />} />
            <Route path="/dashboard/payments/pix" element={<PaymentsPixPage />} />
            <Route path="/dashboard/gateways" element={<GatewaysHubPage />} />
            <Route path="/dashboard/gateways/frendz" element={<GatewayCredentialsPage variant="frendz" />} />
            <Route path="/dashboard/gateways/sigilopay" element={<GatewayCredentialsPage variant="sigilopay" />} />
            <Route path="/dashboard/gateways/wintech" element={<GatewayCredentialsPage variant="wintech" />} />
            <Route path="/dashboard/gateways/gateway-2" element={<GatewayPlaceholderPage title="GATEWAY2" />} />
            <Route path="/dashboard/gateways/gateway-3" element={<GatewayPlaceholderPage title="GATEWAY3" />} />
          </Route>

          <Route element={<RequireRole role={ROLES.Administrator} />}>
            <Route path="/dashboard/admin/operations" element={<AdminOperationsPage />} />
            <Route path="/dashboard/accounts" element={<AccountsPage />} />
          </Route>
        </Route>
      </Route>
    </Routes>
  );
}
