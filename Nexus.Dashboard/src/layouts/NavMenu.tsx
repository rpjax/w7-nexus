import { NavLink } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { canUseOperatorPanel, isAdministrator } from '../auth/roles';

export function NavMenu() {
  const { user } = useAuth();
  const showOperatorPanel = canUseOperatorPanel(user);
  const showAdminItems = isAdministrator(user);

  const [operationsOpen, setOperationsOpen] = useState(true);
  const [accountsOpen, setAccountsOpen] = useState(true);
  const [paymentsOpen, setPaymentsOpen] = useState(true);
  const [gatewaysOpen, setGatewaysOpen] = useState(true);

  const showOperationsMenu = showOperatorPanel || showAdminItems;

  return (
    <nav className="nav-shell">
      <div className="brand-wrap">
        <div className="brand-mark" aria-hidden="true" />
        <div>
          <p className="brand">Websete Nexus</p>
          <p className="brand-subtitle">
            {showAdminItems && showOperatorPanel
              ? 'Operações, pagamentos e administração'
              : showAdminItems
                ? 'Administração e operação'
                : showOperatorPanel
                  ? 'Operações e pagamentos'
                  : 'Dashboard'}
          </p>
        </div>
      </div>

      <div className="nav-links">
        <NavLink to="/dashboard" end className={({ isActive }) => (isActive ? 'active' : undefined)}>Visão geral</NavLink>

        {showOperationsMenu ? (
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
                {showAdminItems ? (
                  <NavLink className={({ isActive }) => `nav-sublink nav-sublink-admin${isActive ? ' active' : ''}`} to="/dashboard/admin/operations" onClick={() => setOperationsOpen(true)}>
                    <span className="submenu-bullet" aria-hidden="true" />
                    Todas as operações
                  </NavLink>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}

        {showAdminItems ? (
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

        {showOperatorPanel ? (
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
                <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/payments" onClick={() => setPaymentsOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  Registros
                </NavLink>
                <NavLink className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`} to="/dashboard/payments/pix" onClick={() => setPaymentsOpen(true)}>
                  <span className="submenu-bullet" aria-hidden="true" />
                  Gerar PIX
                </NavLink>
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
