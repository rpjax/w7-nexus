import { Navigate, Route, Routes } from 'react-router-dom';
import { DashboardLayout } from './layouts/DashboardLayout';
import { HomePage } from './pages/HomePage';
import { OperationsPage } from './pages/OperationsPage';
import { AccountsPage } from './pages/AccountsPage';
import { PaymentsListPage } from './pages/PaymentsListPage';
import { PaymentsPixPage } from './pages/PaymentsPixPage';
import { GatewaysHubPage } from './pages/GatewaysHubPage';
import { GatewayCredentialsPage } from './pages/GatewayCredentialsPage';
import { GatewayPlaceholderPage } from './pages/GatewayPlaceholderPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route element={<DashboardLayout />}>
        <Route path="/dashboard" element={<HomePage />} />
        <Route path="/dashboard/operations" element={<OperationsPage />} />
        <Route path="/dashboard/accounts" element={<AccountsPage />} />
        <Route path="/dashboard/payments" element={<PaymentsListPage />} />
        <Route path="/dashboard/payments/pix" element={<PaymentsPixPage />} />
        <Route path="/dashboard/gateways" element={<GatewaysHubPage />} />
        <Route path="/dashboard/gateways/frendz" element={<GatewayCredentialsPage variant="frendz" />} />
        <Route path="/dashboard/gateways/sigilopay" element={<GatewayCredentialsPage variant="sigilopay" />} />
        <Route path="/dashboard/gateways/wintech" element={<GatewayCredentialsPage variant="wintech" />} />
        <Route path="/dashboard/gateways/gateway-2" element={<GatewayPlaceholderPage title="GATEWAY2" />} />
        <Route path="/dashboard/gateways/gateway-3" element={<GatewayPlaceholderPage title="GATEWAY3" />} />
      </Route>
    </Routes>
  );
}
