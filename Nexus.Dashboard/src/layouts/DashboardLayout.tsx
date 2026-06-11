import { useEffect, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { NavMenu } from './NavMenu';

function resolvePageTitle(pathname: string): string {
  const relative = pathname.replace(/\/$/, '').toLowerCase();
  const map: Record<string, string> = {
    '/dashboard': 'Visão geral',
    '/dashboard/operations': 'Operações',
    '/dashboard/accounts': 'Contas',
    '/dashboard/payments': 'Pagamentos',
    '/dashboard/payments/pix': 'Pagamentos — Gerar PIX',
    '/dashboard/gateways': 'Gateways',
    '/dashboard/gateways/frendz': 'Frendz',
    '/dashboard/gateways/sigilopay': 'SigiloPay',
    '/dashboard/gateways/wintech': 'Wintech',
    '/dashboard/gateways/gateway-2': 'GATEWAY2',
    '/dashboard/gateways/gateway-3': 'GATEWAY3',
  };
  return map[relative] ?? 'Websete Nexus';
}

export function DashboardLayout() {
  const location = useLocation();
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => {
    setDrawerOpen(false);
  }, [location.pathname]);

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
          <h1 className="app-topbar-title">{resolvePageTitle(location.pathname)}</h1>
          <div className="app-topbar-actions">
            <button type="button" className="icon-btn icon-btn-ghost" aria-label="Notificações">
              <svg className="topbar-icon" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
                <path d="M13.73 21a2 2 0 0 1-3.46 0" />
              </svg>
            </button>
            <button type="button" className="icon-btn icon-btn-ghost" aria-label="Conta">
              <svg className="topbar-icon" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
            </button>
          </div>
        </header>
        <main className="app-main" id="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
