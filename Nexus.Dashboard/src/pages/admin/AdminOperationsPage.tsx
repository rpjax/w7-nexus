import { useCallback, useEffect, useState } from 'react';
import {
  createAdministratorOperation,
  deleteAdministratorOperation,
  searchAdministratorOperations,
} from '../../api/administrator/operations';
import type { OperationDetails } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { useNotifications } from '../../notifications/NotificationContext';
import { shortId } from '../../utils/format';

const PAGE_SIZE = 20;

export function AdminOperationsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [createName, setCreateName] = useState('');
  const [createDescription, setCreateDescription] = useState('');
  const [createBusy, setCreateBusy] = useState(false);

  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteOperationId, setDeleteOperationId] = useState('');

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchAdministratorOperations({
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

  async function handleCreate() {
    setCreateBusy(true);
    try {
      const result = await createAdministratorOperation(
        createName.trim(),
        createDescription.trim() || null,
      );
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Operação registrada no sistema.');
      setCreateName('');
      setCreateDescription('');
      setCurrentPage(1);
      setQuery('');
      setSearch('');
      await load(1, '');
    } finally {
      setCreateBusy(false);
    }
  }

  async function confirmDelete() {
    setDeleteDialogOpen(false);
    if (!deleteOperationId) return;

    const result = await deleteAdministratorOperation(deleteOperationId);
    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess('Operação excluída do sistema.');
    setDeleteOperationId('');
    setCurrentPage(1);
    await load(1, query);
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker page-kicker-admin">Administração</p>
          <h1>Todas as operações</h1>
          <p className="muted page-lead">
            Visão global do repositório de operações. Registre novas operações ou remova registros obsoletos do sistema.
          </p>
        </div>
      </section>

      <section className="card ops-card ops-create admin-surface">
        <div className="card-title-row">
          <h2>Registrar operação</h2>
          <span className="post-badge">POST /api/administrator/operations</span>
        </div>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="adminOpName">Nome</label>
            <input
              id="adminOpName"
              className="nexus-input"
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              placeholder="Ex.: Operação Atlas"
            />
          </div>
          <div className="field span-2">
            <label htmlFor="adminOpDesc">Descrição</label>
            <textarea
              id="adminOpDesc"
              className="nexus-input"
              rows={2}
              value={createDescription}
              onChange={(e) => setCreateDescription(e.target.value)}
              placeholder="Contexto e escopo da operação"
            />
          </div>
        </div>
        <div className="card-actions">
          <button type="button" className="btn btn-primary" onClick={() => void handleCreate()} disabled={createBusy}>
            {createBusy ? 'Registrando…' : 'Registrar operação'}
          </button>
        </div>
      </section>

      <section className="card ops-card admin-surface">
        <div className="toolbar">
          <div className="field grow">
            <label htmlFor="adminOpSearch">Buscar no sistema</label>
            <input
              id="adminOpSearch"
              className="nexus-input"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Nome, ID ou descrição…"
            />
          </div>
          <button type="button" className="btn btn-ghost" onClick={() => void handleSearch()}>Buscar</button>
          <button type="button" className="btn btn-ghost" onClick={() => void load(currentPage, query)}>Atualizar</button>
        </div>

        <div className="card-title-row">
          <div className="card-title-group">
            <h2 className="section-title">Operações do sistema</h2>
            <span className="post-badge">POST /api/administrator/operations/search</span>
          </div>
          <span className="muted small">{totalItems} registro(s) no repositório</span>
        </div>

        {items.length === 0 ? (
          <EmptyState
            title="Nenhuma operação encontrada"
            message="Registre uma operação acima ou ajuste o filtro de busca."
          />
        ) : (
          <>
            <div className="table-wrap table-top-gap">
              <table className="responsive-data ops-table">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>ID</th>
                    <th>Descrição</th>
                    <th className="th-actions" scope="col">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((op) => (
                    <tr key={op.id}>
                      <td data-label="Nome"><strong>{op.name}</strong></td>
                      <td data-label="ID"><span className="mono">{shortId(op.id)}</span></td>
                      <td data-label="Descrição">{op.description?.trim() ? op.description : '—'}</td>
                      <td className="cell-actions" data-label="Ações">
                        <button
                          type="button"
                          className="btn btn-danger btn-small"
                          onClick={() => {
                            setDeleteOperationId(op.id);
                            setDeleteDialogOpen(true);
                          }}
                        >
                          Excluir
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalItems > 0 ? (
              <div className="pagination">
                <button type="button" className="btn btn-ghost" onClick={() => setCurrentPage((p) => Math.max(1, p - 1))} disabled={currentPage <= 1}>Anterior</button>
                <span className="muted">Página {currentPage} de {totalPages}</span>
                <button type="button" className="btn btn-ghost" onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))} disabled={currentPage >= totalPages}>Próxima</button>
              </div>
            ) : null}
          </>
        )}
      </section>

      <ConfirmDialog
        open={deleteDialogOpen}
        title="Excluir operação"
        message="Esta ação remove a operação do sistema. Deseja continuar?"
        onCancel={() => { setDeleteDialogOpen(false); setDeleteOperationId(''); }}
        onConfirm={() => void confirmDelete()}
      />
    </>
  );
}
