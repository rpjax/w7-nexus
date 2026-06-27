import { useCallback, useEffect, useState } from 'react';
import { createAccount } from '../api/accounts';
import { searchAdministratorAccounts } from '../api/administrator/accounts';
import type { AccountRow } from '../api/types';
import { AccountCard } from '../components/admin/AccountCard';
import { EmptyState } from '../components/EmptyState';
import { PageHeading } from '../layouts/PageHeading';
import { useNotifications } from '../notifications/NotificationContext';
import { ACCOUNT_ROLE_CATALOG } from '../utils/accountAccess';

const PAGE_SIZE = 20;

export function AccountsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [createBusy, setCreateBusy] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);

  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<AccountRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchAdministratorAccounts({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    setTotalItems(result.data?.total ?? 0);
    setItems(result.data?.items ?? []);
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, query);
  }, [currentPage, query, load]);

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  async function handleRefresh() {
    await load(currentPage, query);
  }

  async function handleCreate() {
    setCreateBusy(true);
    try {
      if (!username.trim() || !password.trim()) {
        notifyError('Usuário e senha são obrigatórios.');
        return;
      }
      const result = await createAccount(username.trim(), password);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Conta criada com sucesso.');
      setUsername('');
      setPassword('');
      setCreateOpen(false);
      setCurrentPage(1);
      setQuery('');
      setSearch('');
      await load(1, '');
    } finally {
      setCreateBusy(false);
    }
  }

  return (
    <div className="accounts-page">
      <PageHeading
        kicker="Administração"
        kickerVariant="admin"
        title="Contas"
        subtitle="Gerencie usuários e ative funções com um clique. Cada conta pode acumular vários papéis."
      />

      <section className="accounts-legend card ops-card admin-surface">
        <div className="accounts-legend__head">
          <h2 className="accounts-legend__title">Funções disponíveis</h2>
          <p className="accounts-legend__lead muted small">
            Referência rápida dos papéis que você pode atribuir a qualquer conta.
          </p>
        </div>
        <ul className="accounts-legend__list">
          {ACCOUNT_ROLE_CATALOG.map((role) => (
            <li key={role.id} className={`accounts-legend__item accounts-legend__item--${role.tone}`}>
              <strong>{role.label}</strong>
              <span className="muted small">{role.description}</span>
            </li>
          ))}
        </ul>
      </section>

      <section className="card ops-card admin-surface accounts-create-panel">
        <button
          type="button"
          className="accounts-create-panel__toggle"
          aria-expanded={createOpen}
          onClick={() => setCreateOpen((open) => !open)}
        >
          <span>
            <strong>Registrar nova conta</strong>
            <span className="muted small">Login e senha inicial — funções são atribuídas depois.</span>
          </span>
          <span aria-hidden="true">{createOpen ? '▾' : '▸'}</span>
        </button>

        {createOpen ? (
          <div className="accounts-create-panel__body">
            <div className="form-grid">
              <div className="field">
                <label htmlFor="accUsername">Usuário</label>
                <input
                  id="accUsername"
                  className="nexus-input"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  autoComplete="off"
                  placeholder="nome.de.login"
                />
              </div>
              <div className="field">
                <label htmlFor="accPassword">Senha</label>
                <input
                  id="accPassword"
                  className="nexus-input"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                  placeholder="Senha inicial"
                />
              </div>
            </div>
            <div className="card-actions">
              <button type="button" className="btn btn-primary" onClick={() => void handleCreate()} disabled={createBusy}>
                {createBusy ? 'Registrando…' : 'Registrar conta'}
              </button>
            </div>
          </div>
        ) : null}
      </section>

      <section className="card ops-card admin-surface accounts-panel">
        <div className="accounts-search-row">
          <input
            id="accSearch"
            className="nexus-input accounts-search-input"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') void handleSearch(); }}
            placeholder="Buscar por @username…"
            aria-label="Buscar contas"
          />
          <button type="button" className="btn btn-primary accounts-search-btn" onClick={() => void handleSearch()}>
            Buscar
          </button>
          <button type="button" className="btn btn-ghost accounts-refresh-btn" onClick={() => void handleRefresh()}>
            Atualizar
          </button>
        </div>

        <div className="accounts-panel-head">
          <div className="accounts-panel-head__copy">
            <h2 className="section-title">Contas cadastradas</h2>
            <p className="muted small">Expanda uma conta para alternar funções e permissões.</p>
          </div>
          <span className="accounts-panel-head__count muted small">{totalItems} registro(s)</span>
        </div>

        {items.length === 0 ? (
          <EmptyState
            title="Nenhuma conta encontrada"
            message="Registre uma conta acima ou ajuste o filtro de busca."
          />
        ) : (
          <>
            <div className="accounts-list">
              {items.map((account, index) => (
                <AccountCard
                  key={account.id}
                  account={account}
                  defaultExpanded={index === 0 && items.length === 1}
                  onMutated={() => {
                    notifySuccess('Conta atualizada.');
                    void load(currentPage, query);
                  }}
                  onError={notifyError}
                />
              ))}
            </div>

            {totalItems > 0 ? (
              <div className="pagination accounts-pagination">
                <button
                  type="button"
                  className="btn btn-ghost btn-small"
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage <= 1}
                >
                  Anterior
                </button>
                <span className="muted accounts-page-indicator">{currentPage} / {totalPages}</span>
                <button
                  type="button"
                  className="btn btn-ghost btn-small"
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  disabled={currentPage >= totalPages}
                >
                  Próxima
                </button>
              </div>
            ) : null}
          </>
        )}
      </section>
    </div>
  );
}
