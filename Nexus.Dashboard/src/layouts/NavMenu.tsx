import { NavLink } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { canUseOperatorPanel, canUseOlxPanel, canUseStrawManPanel, isAdministrator, isOlxOperator } from '../auth/roles';

export function NavMenu() {
  const { user } = useAuth();
  const showOperatorPanel = canUseOperatorPanel(user);
  const showGlobalAdminItems = isAdministrator(user);
  const showStrawManPanel = canUseStrawManPanel(user);
  const showOlxPanel = canUseOlxPanel(user);

  const [operationsOpen, setOperationsOpen] = useState(true);
  const [accountsOpen, setAccountsOpen] = useState(true);
  const [paymentsOpen, setPaymentsOpen] = useState(true);
  const [strawMenOpen, setStrawMenOpen] = useState(true);
  const [gatewaysOpen, setGatewaysOpen] = useState(true);
  const [olxOpen, setOlxOpen] = useState(true);

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
      <div className="brand-wrap">
        <div className="brand-mark" aria-hidden="true" />
        <div>
          <p className="brand">Websete Nexus</p>
          <p className="brand-subtitle">{brandSubtitle}</p>
        </div>
      </div>

      <div className="nav-links">
        <NavLink to="/dashboard" end className={({ isActive }) => (isActive ? 'active' : undefined)}>Visão geral</NavLink>

        {showOperatorPanel || showGlobalAdminItems ? (
          <div className="nav-group nav-group-gateways">
            <button
              type="button"
              className="nav-dropdown nav-dropdown-full"
              onClick={() => setOperationsOpen((v) => !v)}
              aria-expanded={operationsOpen}
              aria-controls="operations-submenu"
            >
              <span className="nav-dropdown-label">Operações</span>
              <span className="nav-caret" aria-hidden="true">{operationsOpen ? '▲' : '▼'}</span>
            </button>
            <div id="operations-submenu" className={`submenu-tree ${operationsOpen ? 'is-open' : 'is-collapsed'}`} aria-hidden={!operationsOpen}>
              <div className="submenu-tree-inner">
                {showOperatorPanel ? (
                  <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/operations" onClick={() => setOperationsOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Minhas operações
                  </NavLink>
                ) : null}
                {showGlobalAdminItems ? (
                  <NavLink className={({ isActive }) => `nav-sublink nav-sublink-admin${isActive ? ' active' : ''}`} to="/dashboard/admin/operations" onClick={() => setOperationsOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Todas as operações
                  </NavLink>
                ) : null}
                {showOperatorPanel ? (
                  <>
                    <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/operation-admin/operations" onClick={() => setOperationsOpen(true)}>
                      <span className="submenu-bullet" aria-hidden="true" />
                      Administração de operações
                    </NavLink>
                    <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/team-leader/operations" onClick={() => setOperationsOpen(true)}>
                      <span className="submenu-bullet" aria-hidden="true" />
                      Liderança de equipes
                    </NavLink>
                  </>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}

        {showGlobalAdminItems ? (
          <div className="nav-group nav-group-gateways">
            <button
              type="button"
              className="nav-dropdown nav-dropdown-full"
              onClick={() => setAccountsOpen((v) => !v)}
              aria-expanded={accountsOpen}
              aria-controls="accounts-submenu"
            >
              <span className="nav-dropdown-label">Contas</span>
              <span className="nav-caret" aria-hidden="true">{accountsOpen ? '▲' : '▼'}</span>
            </button>
            <div id="accounts-submenu" className={`submenu-tree ${accountsOpen ? 'is-open' : 'is-collapsed'}`} aria-hidden={!accountsOpen}>
              <div className="submenu-tree-inner">
                <NavLink className={({ isActive }) => `nav-sublink nav-sublink-admin${isActive ? ' active' : ''}`} to="/dashboard/accounts" onClick={() => setAccountsOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  Gerenciar contas
                </NavLink>
              </div>
            </div>
          </div>
        ) : null}

        {showOperatorPanel || showGlobalAdminItems ? (
          <div className="nav-group nav-group-gateways">
            <button
              type="button"
              className="nav-dropdown nav-dropdown-full"
              onClick={() => setPaymentsOpen((v) => !v)}
              aria-expanded={paymentsOpen}
              aria-controls="payments-submenu"
            >
              <span className="nav-dropdown-label">Pagamentos</span>
              <span className="nav-caret" aria-hidden="true">{paymentsOpen ? '▲' : '▼'}</span>
            </button>
            <div id="payments-submenu" className={`submenu-tree ${paymentsOpen ? 'is-open' : 'is-collapsed'}`} aria-hidden={!paymentsOpen}>
              <div className="submenu-tree-inner">
                {showOperatorPanel ? (
                  <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/payments" onClick={() => setPaymentsOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Meus pagamentos
                  </NavLink>
                ) : null}
                {showGlobalAdminItems ? (
                  <NavLink className={({ isActive }) => `nav-sublink nav-sublink-admin${isActive ? ' active' : ''}`} to="/dashboard/admin/payments" onClick={() => setPaymentsOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Todos os pagamentos
                  </NavLink>
                ) : null}
                {showOperatorPanel ? (
                  <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/payments/pix" onClick={() => setPaymentsOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Gerar PIX
                  </NavLink>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}

        {showStrawManPanel || showGlobalAdminItems ? (
          <div className="nav-group nav-group-straw-men">
            <button
              type="button"
              className="nav-dropdown nav-dropdown-full"
              onClick={() => setStrawMenOpen((v) => !v)}
              aria-expanded={strawMenOpen}
              aria-controls="straw-men-submenu"
            >
              <span className="nav-dropdown-label">Laranjas</span>
              <span className="nav-caret" aria-hidden="true">{strawMenOpen ? '▲' : '▼'}</span>
            </button>
            <div id="straw-men-submenu" className={`submenu-tree ${strawMenOpen ? 'is-open' : 'is-collapsed'}`} aria-hidden={!strawMenOpen}>
              <div className="submenu-tree-inner">
                {showStrawManPanel ? (
                  <>
                    <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/straw-man/payments" onClick={() => setStrawMenOpen(true)}>
                      <span className="submenu-bullet" aria-hidden="true" />
                      Meus pagamentos
                    </NavLink>
                    <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/straw-man/settings" onClick={() => setStrawMenOpen(true)}>
                      <span className="submenu-bullet" aria-hidden="true" />
                      Minhas configurações
                    </NavLink>
                  </>
                ) : null}
                {showGlobalAdminItems ? (
                  <NavLink className={({ isActive }) => `nav-sublink nav-sublink-admin${isActive ? ' active' : ''}`} to="/dashboard/admin/straw-men" onClick={() => setStrawMenOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Gestão de laranjas
                  </NavLink>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}

        {showOlxPanel ? (
          <div className="nav-group nav-group-olx">
            <button
              type="button"
              className="nav-dropdown nav-dropdown-full"
              onClick={() => setOlxOpen((v) => !v)}
              aria-expanded={olxOpen}
              aria-controls="olx-submenu"
            >
              <span className="nav-dropdown-label">OLX</span>
              <span className="nav-caret" aria-hidden="true">{olxOpen ? '▲' : '▼'}</span>
            </button>
            <div id="olx-submenu" className={`submenu-tree ${olxOpen ? 'is-open' : 'is-collapsed'}`} aria-hidden={!olxOpen}>
              <div className="submenu-tree-inner">
                {isOlxOperator(user) || showGlobalAdminItems ? (
                  <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/olx/ads" onClick={() => setOlxOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Meus anúncios
                  </NavLink>
                ) : null}
                {showGlobalAdminItems ? (
                  <NavLink className={({ isActive }) => `nav-sublink nav-sublink-admin${isActive ? ' active' : ''}`} to="/dashboard/olx/admin/ads" onClick={() => setOlxOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Gestão global
                  </NavLink>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}

        {showOperatorPanel ? (
          <div className="nav-group nav-group-gateways">
            <button
              type="button"
              className="nav-dropdown nav-dropdown-full"
              onClick={() => setGatewaysOpen((v) => !v)}
              aria-expanded={gatewaysOpen}
              aria-controls="gateway-submenu"
            >
              <span className="nav-dropdown-label">Gateways</span>
              <span className="nav-caret" aria-hidden="true">{gatewaysOpen ? '▲' : '▼'}</span>
            </button>
            <div id="gateway-submenu" className={`submenu-tree ${gatewaysOpen ? 'is-open' : 'is-collapsed'}`} aria-hidden={!gatewaysOpen}>
              <div className="submenu-tree-inner">
                <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/gateways" onClick={() => setGatewaysOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  Visão geral
                </NavLink>
                <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/gateways/frendz" onClick={() => setGatewaysOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  Frendz
                </NavLink>
                <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/gateways/sigilopay" onClick={() => setGatewaysOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  SigiloPay
                </NavLink>
                <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/gateways/wintech" onClick={() => setGatewaysOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  Wintech
                </NavLink>
              </div>
            </div>
          </div>
        ) : null}
      </div>
    </nav>
  );
}
