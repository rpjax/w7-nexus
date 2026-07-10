import { NavLink, useLocation } from 'react-router-dom';
import { useEffect, useState, type ReactNode } from 'react';
import { useAuth } from '../auth/AuthContext';
import { canUseOperatorPanel, canUseOlxPanel, canUseStrawManPanel, isAdministrator, isOlxOperator } from '../auth/roles';

type NavSectionId = 'operations' | 'accounts' | 'payments' | 'strawMen' | 'olx' | 'scripts' | 'gateways' | 'dev';

function sectionForPath(pathname: string): NavSectionId | null {
  const path = pathname.replace(/\/$/, '').toLowerCase();
  if (path.startsWith('/dashboard/gateways')) return 'gateways';
  if (path.startsWith('/dashboard/admin/api-docs')) return 'dev';
  if (path.startsWith('/dashboard/admin/scripts')) return 'scripts';
  if (path.startsWith('/dashboard/olx')) return 'olx';
  if (path.startsWith('/dashboard/straw-man') || path.startsWith('/dashboard/admin/straw-men')) return 'strawMen';
  if (path.startsWith('/dashboard/payments') || path.startsWith('/dashboard/admin/payments')) return 'payments';
  if (path.startsWith('/dashboard/accounts')) return 'accounts';
  if (
    path.startsWith('/dashboard/operations')
    || path.startsWith('/dashboard/admin/operations')
    || path.startsWith('/dashboard/operation-admin')
    || path.startsWith('/dashboard/team-leader')
  ) {
    return 'operations';
  }
  return null;
}

function allClosedSections(): Record<NavSectionId, boolean> {
  return {
    operations: false,
    accounts: false,
    payments: false,
    strawMen: false,
    olx: false,
    scripts: false,
    gateways: false,
    dev: false,
  };
}

function defaultOpenSections(pathname: string): Record<NavSectionId, boolean> {
  const active = sectionForPath(pathname);
  return {
    operations: active === 'operations',
    accounts: active === 'accounts',
    payments: active === 'payments',
    strawMen: active === 'strawMen',
    olx: active === 'olx',
    scripts: active === 'scripts',
    gateways: active === 'gateways',
    dev: active === 'dev',
  };
}

function NavChevron({ open }: { open: boolean }) {
  return (
    <svg
      className={`nav-chevron${open ? ' is-open' : ''}`}
      viewBox="0 0 16 16"
      width="14"
      height="14"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 6l4 4 4-4" />
    </svg>
  );
}

function NavSection({
  id,
  label,
  open,
  active,
  onToggle,
  variant,
  children,
}: {
  id: string;
  label: string;
  open: boolean;
  active?: boolean;
  onToggle: () => void;
  variant?: 'straw-men' | 'olx';
  children: ReactNode;
}) {
  return (
    <div className={`nav-section${variant ? ` nav-section--${variant}` : ''}`}>
      <button
        type="button"
        className={`nav-section-trigger${open ? ' is-open' : ''}${active ? ' has-active' : ''}`}
        onClick={onToggle}
        aria-expanded={open}
        aria-controls={`${id}-submenu`}
      >
        <span className="nav-section-label">{label}</span>
        <NavChevron open={open} />
      </button>
      <div
        id={`${id}-submenu`}
        className={`nav-section-panel${open ? ' is-open' : ''}`}
        aria-hidden={!open}
      >
        <div className="nav-section-panel-inner">
          {children}
        </div>
      </div>
    </div>
  );
}

function NavSublink({
  to,
  admin,
  children,
  onNavigate,
}: {
  to: string;
  admin?: boolean;
  children: ReactNode;
  onNavigate?: () => void;
}) {
  return (
    <NavLink
      className={({ isActive }) => `nav-sublink${admin ? ' nav-sublink-admin' : ''}${isActive ? ' active' : ''}`}
      to={to}
      onClick={onNavigate}
    >
      <span className="nav-sublink-label">{children}</span>
      {admin ? <span className="nav-admin-badge">Admin</span> : null}
    </NavLink>
  );
}

export function NavMenu() {
  const { user } = useAuth();
  const location = useLocation();
  const showOperatorPanel = canUseOperatorPanel(user);
  const showGlobalAdminItems = isAdministrator(user);
  const showStrawManPanel = canUseStrawManPanel(user);
  const showOlxPanel = canUseOlxPanel(user);

  const [openSections, setOpenSections] = useState(() => defaultOpenSections(location.pathname));
  const activeSection = sectionForPath(location.pathname);

  useEffect(() => {
    setOpenSections(defaultOpenSections(location.pathname));
  }, [location.pathname]);

  const toggle = (id: NavSectionId) => {
    setOpenSections((prev) => {
      if (prev[id]) {
        return { ...prev, [id]: false };
      }
      return { ...allClosedSections(), [id]: true };
    });
  };
  const keepOpen = (id: NavSectionId) => () => setOpenSections((prev) => ({ ...prev, [id]: true }));

  const brandSubtitle = showGlobalAdminItems && showOperatorPanel
    ? 'Operações, pagamentos e administração'
    : showGlobalAdminItems
      ? 'Administração do sistema'
      : showOperatorPanel
        ? 'Operações e pagamentos'
        : showOlxPanel
          ? 'Painel OLX'
          : showStrawManPanel
            ? 'Painel do laranja'
            : 'Dashboard';

  return (
    <nav className="nav-shell">
      <header className="nav-brand">
        <div className="brand-mark" aria-hidden="true" />
        <div className="nav-brand-text">
          <p className="brand">Websete Nexus</p>
          <p className="brand-subtitle">{brandSubtitle}</p>
        </div>
      </header>

      <div className="nav-scroll">
        <div className="nav-primary">
          <NavLink to="/dashboard" end className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}>
            <svg className="nav-item-icon" viewBox="0 0 20 20" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
              <path d="M3 8.5L10 3l7 5.5V16a1.5 1.5 0 0 1-1.5 1.5H4.5A1.5 1.5 0 0 1 3 16V8.5z" />
              <path d="M8 17.5V11h4v6.5" />
            </svg>
            Visão geral
          </NavLink>
        </div>

        <div className="nav-divider" role="separator" aria-hidden="true" />

        <div className="nav-sections">
          {showOperatorPanel || showGlobalAdminItems ? (
            <NavSection
              id="operations"
              label="Operações"
              open={openSections.operations}
              active={activeSection === 'operations'}
              onToggle={() => toggle('operations')}
            >
              {showOperatorPanel ? (
                <NavSublink to="/dashboard/operations" onNavigate={keepOpen('operations')}>
                  Minhas operações
                </NavSublink>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/admin/operations" admin onNavigate={keepOpen('operations')}>
                  Todas as operações
                </NavSublink>
              ) : null}
              {showOperatorPanel ? (
                <>
                  <NavSublink to="/dashboard/operation-admin/operations" onNavigate={keepOpen('operations')}>
                    Administração de operações
                  </NavSublink>
                  <NavSublink to="/dashboard/team-leader/operations" onNavigate={keepOpen('operations')}>
                    Liderança de equipes
                  </NavSublink>
                </>
              ) : null}
            </NavSection>
          ) : null}

          {showGlobalAdminItems ? (
            <NavSection
              id="accounts"
              label="Contas"
              open={openSections.accounts}
              active={activeSection === 'accounts'}
              onToggle={() => toggle('accounts')}
            >
              <NavSublink to="/dashboard/accounts" admin onNavigate={keepOpen('accounts')}>
                Gerenciar contas
              </NavSublink>
            </NavSection>
          ) : null}

          {showOperatorPanel || showGlobalAdminItems ? (
            <NavSection
              id="payments"
              label="Pagamentos"
              open={openSections.payments}
              active={activeSection === 'payments'}
              onToggle={() => toggle('payments')}
            >
              {showOperatorPanel ? (
                <NavSublink to="/dashboard/payments" onNavigate={keepOpen('payments')}>
                  Meus pagamentos
                </NavSublink>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/admin/payments" admin onNavigate={keepOpen('payments')}>
                  Todos os pagamentos
                </NavSublink>
              ) : null}
              {showOperatorPanel ? (
                <NavSublink to="/dashboard/payments/pix" onNavigate={keepOpen('payments')}>
                  Gerar PIX
                </NavSublink>
              ) : null}
            </NavSection>
          ) : null}

          {showStrawManPanel || showGlobalAdminItems ? (
            <NavSection
              id="straw-men"
              label="Laranjas"
              variant="straw-men"
              open={openSections.strawMen}
              active={activeSection === 'strawMen'}
              onToggle={() => toggle('strawMen')}
            >
              {showStrawManPanel ? (
                <>
                  <NavSublink to="/dashboard/straw-man/payments" onNavigate={keepOpen('strawMen')}>
                    Meus pagamentos
                  </NavSublink>
                  <NavSublink to="/dashboard/straw-man/settings" onNavigate={keepOpen('strawMen')}>
                    Minhas configurações
                  </NavSublink>
                </>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/admin/straw-men" admin onNavigate={keepOpen('strawMen')}>
                  Gestão de laranjas
                </NavSublink>
              ) : null}
            </NavSection>
          ) : null}

          {showOlxPanel ? (
            <NavSection
              id="olx"
              label="OLX"
              variant="olx"
              open={openSections.olx}
              active={activeSection === 'olx'}
              onToggle={() => toggle('olx')}
            >
              {isOlxOperator(user) || showGlobalAdminItems ? (
                <NavSublink to="/dashboard/olx/ads" onNavigate={keepOpen('olx')}>
                  Meus anúncios
                </NavSublink>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/olx/admin/ads" admin onNavigate={keepOpen('olx')}>
                  Gestão global
                </NavSublink>
              ) : null}
            </NavSection>
          ) : null}

          {showGlobalAdminItems ? (
            <NavSection
              id="scripts"
              label="Scripts"
              open={openSections.scripts}
              active={activeSection === 'scripts'}
              onToggle={() => toggle('scripts')}
            >
              <NavSublink to="/dashboard/admin/scripts" admin onNavigate={keepOpen('scripts')}>
                Inventário
              </NavSublink>
            </NavSection>
          ) : null}

          {showGlobalAdminItems ? (
            <NavSection
              id="dev"
              label="Desenvolvimento"
              open={openSections.dev}
              active={activeSection === 'dev'}
              onToggle={() => toggle('dev')}
            >
              <NavSublink to="/dashboard/admin/api-docs" admin onNavigate={keepOpen('dev')}>
                Documentação da API
              </NavSublink>
            </NavSection>
          ) : null}

          {showOperatorPanel ? (
            <NavSection
              id="gateways"
              label="Gateways"
              open={openSections.gateways}
              active={activeSection === 'gateways'}
              onToggle={() => toggle('gateways')}
            >
              <NavSublink to="/dashboard/gateways" onNavigate={keepOpen('gateways')}>
                Visão geral
              </NavSublink>
              <NavSublink to="/dashboard/gateways/frendz" onNavigate={keepOpen('gateways')}>
                Frendz
              </NavSublink>
              <NavSublink to="/dashboard/gateways/sigilopay" onNavigate={keepOpen('gateways')}>
                SigiloPay
              </NavSublink>
              <NavSublink to="/dashboard/gateways/wintech" onNavigate={keepOpen('gateways')}>
                Wintech
              </NavSublink>
            </NavSection>
          ) : null}
        </div>
      </div>
    </nav>
  );
}
