import { Navigate, Route, Routes } from 'react-router-dom';
import { RedirectIfAuthenticated, RequireAuth } from './auth/RequireAuth';
import { RequireAnyRole, RequireRole } from './auth/RequireRole';
import { ROLES } from './auth/roles';
import { DashboardLayout } from './layouts/DashboardLayout';
import { AdminOperationDetailPage } from './pages/admin/AdminOperationDetailPage';
import { AdminPaymentDetailPage } from './pages/admin/AdminPaymentDetailPage';
import { AdminPaymentsPage } from './pages/admin/AdminPaymentsPage';
import { AdminTeamDetailPage } from './pages/admin/AdminTeamDetailPage';
import { AdminOperationsPage } from './pages/admin/AdminOperationsPage';
import { AuthPage } from './pages/AuthPage';
import { HomePage } from './pages/HomePage';
import { OperationAdminOperationDetailPage } from './pages/operationAdmin/OperationAdminOperationDetailPage';
import { OperationAdminTeamDetailPage } from './pages/operationAdmin/OperationAdminTeamDetailPage';
import { OperationAdminOperationsPage } from './pages/operationAdmin/OperationAdminOperationsPage';
import { OperatorOperationDetailPage } from './pages/operator/OperatorOperationDetailPage';
import { OperatorOperationsPage } from './pages/operator/OperatorOperationsPage';
import { OperatorPaymentDetailPage } from './pages/operator/OperatorPaymentDetailPage';
import { OperatorPaymentsPage } from './pages/operator/OperatorPaymentsPage';
import { TeamLeaderOperationDetailPage } from './pages/teamLeader/TeamLeaderOperationDetailPage';
import { TeamLeaderOperationsPage } from './pages/teamLeader/TeamLeaderOperationsPage';
import { AccountsPage } from './pages/AccountsPage';
import { PaymentsPixPage } from './pages/PaymentsPixPage';
import { StrawManPaymentDetailPage } from './pages/strawMan/StrawManPaymentDetailPage';
import { StrawManPaymentsPage } from './pages/strawMan/StrawManPaymentsPage';
import { AdminStrawManSettingsPage, StrawManSettingsPage } from './pages/strawMan/StrawManSettingsPage';
import { BankAccountsPage } from './pages/withdrawals/BankAccountsPage';
import { CryptoWalletsPage } from './pages/withdrawals/CryptoWalletsPage';
import { WithdrawalCreatePage } from './pages/withdrawals/WithdrawalCreatePage';
import { MovementCreatePage } from './pages/withdrawals/MovementCreatePage';
import { PayoutCreatePage } from './pages/withdrawals/PayoutCreatePage';
import { WithdrawalDetailPage } from './pages/withdrawals/WithdrawalDetailPage';
import { WithdrawalsListPage } from './pages/withdrawals/WithdrawalsListPage';
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
            <Route path="/dashboard/operations/:operationId" element={<OperatorOperationDetailPage />} />
            <Route path="/dashboard/operation-admin/operations" element={<OperationAdminOperationsPage />} />
            <Route path="/dashboard/operation-admin/operations/:operationId" element={<OperationAdminOperationDetailPage />} />
            <Route path="/dashboard/operation-admin/operations/:operationId/teams/:teamId" element={<OperationAdminTeamDetailPage />} />
            <Route path="/dashboard/team-leader/operations" element={<TeamLeaderOperationsPage />} />
            <Route path="/dashboard/team-leader/operations/:operationId" element={<TeamLeaderOperationDetailPage />} />
            <Route path="/dashboard/payments/pix" element={<PaymentsPixPage />} />
            <Route path="/dashboard/payments/:paymentId" element={<OperatorPaymentDetailPage />} />
            <Route path="/dashboard/payments" element={<OperatorPaymentsPage />} />
            <Route path="/dashboard/gateways" element={<GatewaysHubPage />} />
            <Route path="/dashboard/gateways/frendz" element={<GatewayCredentialsPage variant="frendz" />} />
            <Route path="/dashboard/gateways/sigilopay" element={<GatewayCredentialsPage variant="sigilopay" />} />
            <Route path="/dashboard/gateways/wintech" element={<GatewayCredentialsPage variant="wintech" />} />
            <Route path="/dashboard/gateways/gateway-2" element={<GatewayPlaceholderPage title="GATEWAY2" />} />
            <Route path="/dashboard/gateways/gateway-3" element={<GatewayPlaceholderPage title="GATEWAY3" />} />
          </Route>

          <Route element={<RequireRole role={ROLES.Administrator} />}>
            <Route path="/dashboard/admin/operations" element={<AdminOperationsPage />} />
            <Route path="/dashboard/admin/operations/:operationId" element={<AdminOperationDetailPage />} />
            <Route path="/dashboard/admin/operations/:operationId/teams/:teamId" element={<AdminTeamDetailPage />} />
            <Route path="/dashboard/admin/payments" element={<AdminPaymentsPage />} />
            <Route path="/dashboard/admin/payments/:paymentId" element={<AdminPaymentDetailPage />} />
            <Route path="/dashboard/accounts" element={<AccountsPage />} />
            <Route path="/dashboard/transfers" element={<WithdrawalsListPage />} />
            <Route path="/dashboard/transfers/new" element={<WithdrawalCreatePage />} />
            <Route path="/dashboard/transfers/movement" element={<MovementCreatePage />} />
            <Route path="/dashboard/transfers/payout" element={<PayoutCreatePage />} />
            <Route path="/dashboard/transfers/bank-accounts" element={<BankAccountsPage />} />
            <Route path="/dashboard/transfers/crypto-wallets" element={<CryptoWalletsPage />} />
            <Route path="/dashboard/transfers/:transferId" element={<WithdrawalDetailPage />} />
            <Route path="/dashboard/admin/straw-man-settings" element={<AdminStrawManSettingsPage />} />
          </Route>

          <Route element={<RequireRole role={ROLES.StrawMan} />}>
            <Route path="/dashboard/straw-man/payments" element={<StrawManPaymentsPage />} />
            <Route path="/dashboard/straw-man/payments/:paymentId" element={<StrawManPaymentDetailPage />} />
            <Route path="/dashboard/straw-man/settings" element={<StrawManSettingsPage />} />
          </Route>
        </Route>
      </Route>
    </Routes>
  );
}
