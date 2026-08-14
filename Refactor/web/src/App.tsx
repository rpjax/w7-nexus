import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { RedirectIfAuthenticated, RequireAuth } from '@/auth/RequireAuth';
import { RequireCapability } from '@/auth/RequireCapability';
import { PageHeader } from '@/components/layout/page-header';
import { Button } from '@/components/ui/button';
import { AppShell } from '@/layouts/AppShell';
import { AccountsPage } from '@/pages/AccountsPage';
import { AuthPage } from '@/pages/AuthPage';
import { CarteiraPage } from '@/pages/CarteiraPage';
import { ChargesPage } from '@/pages/ChargesPage';
import { ClaimsPage } from '@/pages/ClaimsPage';
import { DealsPage } from '@/pages/DealsPage';
import { HomePage } from '@/pages/HomePage';
import { OperationsPage } from '@/pages/OperationsPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { ShareholdersPage } from '@/pages/ShareholdersPage';
import { StatementPage } from '@/pages/StatementPage';
import { WorldAccountsPage } from '@/pages/WorldAccountsPage';

function NotFoundPage() {
  const location = useLocation();
  const path = `${location.pathname}${location.search}`;

  return (
    <div className="min-w-0 space-y-6">
      <PageHeader
        kicker="Sessão"
        title="Página não encontrada"
        description={
          <>
            Não existe uma tela em{' '}
            <span className="break-all font-medium text-foreground">{path}</span>.
            Confira o endereço ou volte ao início.
          </>
        }
      />
      <Button asChild>
        <Link to="/dashboard">Voltar ao início</Link>
      </Button>
    </div>
  );
}

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
          <Route path="/dashboard/statement" element={<StatementPage />} />
          <Route
            element={<RequireCapability allow={(a) => a.canRecruit || a.admin} />}
          >
            <Route path="/dashboard/carteira" element={<CarteiraPage />} />
          </Route>
          <Route element={<RequireCapability allow={(a) => a.canGrant || a.admin} />}>
            <Route path="/dashboard/accounts" element={<AccountsPage />} />
          </Route>
          <Route
            element={
              <RequireCapability allow={(a) => a.canSeeFinance || a.canManageGateways || a.admin} />
            }
          >
            <Route path="/dashboard/world-accounts" element={<WorldAccountsPage />} />
          </Route>
          <Route
            element={<RequireCapability allow={(a) => a.canManageOperations || a.admin} />}
          >
            <Route path="/dashboard/operations" element={<OperationsPage />} />
          </Route>
          <Route
            element={
              <RequireCapability
                allow={(a) => a.canActAsOperator || a.canSeeFinance || a.canManageOperations || a.admin}
              />
            }
          >
            <Route path="/dashboard/charges" element={<ChargesPage />} />
          </Route>
          <Route element={<RequireCapability allow={(a) => a.canSeeFinance || a.admin} />}>
            <Route path="/dashboard/claims" element={<ClaimsPage />} />
          </Route>
          <Route element={<RequireCapability allow={(a) => a.canRecruit || a.admin} />}>
            <Route path="/dashboard/deals" element={<DealsPage />} />
          </Route>
          <Route element={<RequireCapability allow={(a) => a.admin} />}>
            <Route path="/dashboard/shareholders" element={<ShareholdersPage />} />
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
