import { useEffect, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { isOperationDetailPath } from '../features/operations/operationPaths';
import { isPaymentDetailPath } from '../features/payments/paymentPaths';
import { isTeamDetailPath } from '../features/teams/teamPaths';
import { PageTitleProvider, usePageTitle } from './PageTitleContext';
import { NavMenu } from './NavMenu';

function resolvePageTitle(pathname: string): string {
  const relative = pathname.replace(/\/$/, '').toLowerCase();
  if (isPaymentDetailPath(relative)) {
    return 'Detalhe do pagamento';
  }
  if (isTeamDetailPath(relative)) {
    return 'Detalhe da equipe';
  }
  if (isOperationDetailPath(relative)) {
    return 'Detalhe da operação';
  }
  if (/^\/dashboard\/transfers\/[^/]+$/.test(relative) && relative !== '/dashboard/transfers/new') {
    return 'Detalhe da transferência';
  }
  const map: Record<string, string> = {
    '/dashboard': 'Visão geral',
    '/dashboard/operations': 'Minhas operações',
    '/dashboard/admin/operations': 'Todas as operações',
    '/dashboard/operation-admin/operations': 'Administração de operações',
    '/dashboard/team-leader/operations': 'Liderança de equipes',
    '/dashboard/accounts': 'Contas',
    '/dashboard/payments': 'Meus pagamentos',
    '/dashboard/admin/payments': 'Todos os pagamentos',
    '/dashboard/straw-man/payments': 'Meus pagamentos',
    '/dashboard/straw-man/settings': 'Minhas configurações',
    '/dashboard/admin/straw-men': 'Gestão de laranjas',
    '/dashboard/payments/pix': 'Pagamentos — Gerar PIX',
    '/dashboard/transfers': 'Transferências',
    '/dashboard/transfers/new': 'Transferências — Novo saque',
    '/dashboard/transfers/bank-accounts': 'Transferências — Contas bancárias',
    '/dashboard/transfers/crypto-wallets': 'Transferências — Carteiras crypto',
    '/dashboard/gateways': 'Gateways',
    '/dashboard/gateways/frendz': 'Frendz',
    '/dashboard/gateways/sigilopay': 'SigiloPay',
    '/dashboard/gateways/wintech': 'Wintech',
    '/dashboard/gateways/gateway-2': 'GATEWAY2',
    '/dashboard/gateways/gateway-3': 'GATEWAY3',
  };
  return map[relative] ?? 'Websete Nexus';
}

function DashboardLayoutInner() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, signOut } = useAuth();
  const { title: pageTitle, setTitle } = usePageTitle();
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => {
    setDrawerOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    const onNamedDetail = isOperationDetailPath(location.pathname)
      || isTeamDetailPath(location.pathname)
      || isPaymentDetailPath(location.pathname);
    if (!onNamedDetail) {
      setTitle(null);
    }
  }, [location.pathname, setTitle]);

  const topbarTitle = pageTitle ?? resolvePageTitle(location.pathname);

  return (
    <div className="app-root">
      {drawerOpen ? (
        <button type="button" className="drawer-scrim" aria-label="Fechar menu" onClick={() => setDrawerOpen(false)} />
      ) : null}

      <aside className={`app-sidebar ${drawerOpen ? 'is-drawer-open' : ''}`} aria-label="Navegação principal">
        <NavMenu />
      </aside>

      <div className="app-main-column">
        <header className="app-topbar">
          <button
            type="button"
            className="icon-btn icon-btn-ghost app-menu-btn"
            onClick={() => setDrawerOpen((v) => !v)}
            aria-label="Menu"
            aria-expanded={drawerOpen}
          >
            <svg className="topbar-icon" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <path d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <h1 className="app-topbar-title">{topbarTitle}</h1>
          <div className="app-topbar-actions">
            <button type="button" className="icon-btn icon-btn-ghost" aria-label="Notificações">
              <svg className="topbar-icon" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
                <path d="M13.73 21a2 2 0 0 1-3.46 0" />
              </svg>
            </button>
            <span className="topbar-user" title={user?.username}>{user?.username ?? 'Conta'}</span>
            <button
              type="button"
              className="btn btn-ghost btn-small topbar-signout"
              onClick={() => {
                signOut();
                navigate('/auth', { replace: true });
              }}
            >
              Sair
            </button>
          </div>
        </header>
        <main className="app-main app-main--scroll-host" id="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export function DashboardLayout() {
  return (
    <PageTitleProvider>
      <DashboardLayoutInner />
    </PageTitleProvider>
  );
}
