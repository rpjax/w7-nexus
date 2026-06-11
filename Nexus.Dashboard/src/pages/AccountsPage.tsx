import { useEffect, useMemo, useState } from 'react';
import { createAccount, searchAccounts } from '../api/accounts';
import type { AccountRow } from '../api/types';
import { EmptyState } from '../components/EmptyState';
import { useNotifications } from '../notifications/NotificationContext';
import { formatUtc, joinList, shortId } from '../utils/format';

export function AccountsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [search, setSearch] = useState('');
  const [rows, setRows] = useState<AccountRow[]>([]);
  const [createBusy, setCreateBusy] = useState(false);

  const filteredRows = useMemo(() => {
    if (!search.trim()) return rows;
    const term = search.trim().toLowerCase();
    return rows.filter(
      (r) => r.id.toLowerCase().includes(term) || r.username.toLowerCase().includes(term),
    );
  }, [rows, search]);

  async function refresh() {
    const result = await searchAccounts({ limit: 500, offset: 0, keyword: null });
    if (!result.ok) {
      notifyError(result.error);
      setRows([]);
      return;
    }
    setRows(result.data?.items ?? []);
  }

  useEffect(() => {
    void refresh();
  }, []);

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
      await refresh();
    } finally {
      setCreateBusy(false);
    }
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <h1>Contas</h1>
          <p className="muted page-lead">Contas do agregado <strong>Account</strong> — usuário, papéis e permissões persistidos no repositório.</p>
        </div>
      </section>

      <section className="card ops-card ops-create">
        <div className="card-title-row">
          <h2>Nova conta</h2>
          <span className="post-badge">POST /api/accounts</span>
        </div>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="accUsername">Usuário</label>
            <input id="accUsername" className="nexus-input" value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="off" placeholder="Nome de login" />
          </div>
          <div className="field">
            <label htmlFor="accPassword">Senha</label>
            <input id="accPassword" className="nexus-input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" placeholder="Senha inicial" />
          </div>
        </div>
        <div className="card-actions">
          <button type="button" className="btn btn-primary" onClick={() => void handleCreate()} disabled={createBusy}>
            {createBusy ? 'Criando…' : 'Criar conta'}
          </button>
        </div>
      </section>

      <section className="card ops-card">
        <div className="toolbar toolbar-tight toolbar-stack-mobile">
          <div className="field grow">
            <label htmlFor="accSearch">Buscar contas</label>
            <input id="accSearch" className="nexus-input" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="ID ou nome de usuário…" />
          </div>
          <div className="toolbar-actions">
            <button type="button" className="btn btn-ghost" onClick={() => setSearch(search)}>Buscar</button>
            <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Atualizar</button>
          </div>
        </div>
        <div className="card-title-row">
          <div className="card-title-group">
            <h2 className="section-title">Contas cadastradas</h2>
            <span className="post-badge">POST /api/accounts/search</span>
          </div>
        </div>

        {filteredRows.length === 0 ? (
          <EmptyState title="Nenhuma conta encontrada" message="Crie uma conta acima ou ajuste a busca." />
        ) : (
          <div className="table-wrap table-top-gap">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th>Usuário</th>
                  <th>ID</th>
                  <th>Papéis</th>
                  <th>Permissões</th>
                  <th>Criado em</th>
                  <th>Atualizado em</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.map((row) => (
                  <tr key={row.id}>
                    <td data-label="Usuário"><strong>{row.username}</strong></td>
                    <td data-label="ID"><span className="mono">{shortId(row.id, 18)}</span></td>
                    <td data-label="Papéis" className="muted small">{joinList(row.roles)}</td>
                    <td data-label="Permissões" className="muted small">{joinList(row.permissions)}</td>
                    <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                    <td data-label="Atualizado em" className="muted small">{formatUtc(row.lastUpdatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
