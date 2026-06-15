import { useCallback, useEffect, useState } from 'react';
import { createAccount } from '../api/accounts';
import { searchAdministratorAccounts } from '../api/administrator/accounts';
import type { AccountRow } from '../api/types';
import { AccountCard } from '../components/admin/AccountCard';
import { EmptyState } from '../components/EmptyState';
import { useNotifications } from '../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function AccountsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [createBusy, setCreateBusy] = useState(false);

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
      setCurrentPage(1);
      setQuery('');
      setSearch('');
      await load(1, '');
    } finally {
      setCreateBusy(false);
    }
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker page-kicker-admin">Administração</p>
          <h1>Contas</h1>
          <p className="muted page-lead">
            Usuários registrados no sistema — papéis, permissões e histórico de atualização.
          </p>
        </div>
      </section>

      <section className="card ops-card ops-create admin-surface">
        <div className="card-title-row">
          <h2>Registrar conta</h2>
          <span className="post-badge">POST /api/account</span>
        </div>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="accUsername">Usuário</label>
            <input
              id="accUsername"
              className="nexus-input"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="off"
              placeholder="Nome de login"
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
      </section>

      <section className="card ops-card admin-surface accounts-panel">
        <div className="accounts-search-row">
          <input
            id="accSearch"
            className="nexus-input accounts-search-input"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') void handleSearch(); }}
            placeholder="Buscar por nome ou ID…"
            aria-label="Buscar contas"
          />
          <button type="button" className="btn btn-primary accounts-search-btn" onClick={() => void handleSearch()}>
            Buscar
          </button>
          <button type="button" className="btn btn-ghost accounts-refresh-btn" onClick={() => void handleRefresh()}>
            Atualizar
          </button>
        </div>

        <div className="card-title-row accounts-panel-head">
          <div className="card-title-group">
            <h2 className="section-title">Contas cadastradas</h2>
            <span className="post-badge">POST /api/administrator/accounts/search</span>
          </div>
          <span className="muted small">{totalItems} registro(s)</span>
        </div>

        {items.length === 0 ? (
          <EmptyState
            title="Nenhuma conta encontrada"
            message="Registre uma conta acima ou ajuste o filtro de busca."
          />
        ) : (
          <>
            <div className="accounts-list">
              {items.map((account) => (
                <AccountCard key={account.id} account={account} />
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
    </>
  );
}
