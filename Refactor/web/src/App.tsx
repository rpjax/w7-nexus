import { Navigate, Route, Routes } from 'react-router-dom';
import { RedirectIfAuthenticated, RequireAuth } from '@/auth/RequireAuth';
import { RequireAdministrator } from '@/auth/RequireAdministrator';
import { AppShell } from '@/layouts/AppShell';
import { AccountsPage } from '@/pages/AccountsPage';
import { AuthPage } from '@/pages/AuthPage';
import { ChargesPage } from '@/pages/ChargesPage';
import { ClaimsPage } from '@/pages/ClaimsPage';
import { DealsPage } from '@/pages/DealsPage';
import { HomePage } from '@/pages/HomePage';
import { OperationsPage } from '@/pages/OperationsPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { ShareholdersPage } from '@/pages/ShareholdersPage';
import { WorldAccountsPage } from '@/pages/WorldAccountsPage';

export default function App() {
  return (
    <Routes>
      <Route element={<RedirectIfAuthenticated />}>
        <Route path="/auth" element={<AuthPage />} />
      </Route>

      <Route element={<RequireAuth />}>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route element={<AppShell />}>
          <Route path="/dashboard" element={<HomePage />} />
          <Route path="/dashboard/profile" element={<ProfilePage />} />
          <Route element={<RequireAdministrator />}>
            <Route path="/dashboard/accounts" element={<AccountsPage />} />
            <Route path="/dashboard/world-accounts" element={<WorldAccountsPage />} />
            <Route path="/dashboard/operations" element={<OperationsPage />} />
            <Route path="/dashboard/charges" element={<ChargesPage />} />
            <Route path="/dashboard/claims" element={<ClaimsPage />} />
            <Route path="/dashboard/deals" element={<DealsPage />} />
            <Route path="/dashboard/shareholders" element={<ShareholdersPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
