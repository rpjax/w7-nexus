import { useCallback, useEffect, useState } from 'react';
import {
  assignOperationAdministrator,
  createAdministratorOperation,
  deleteAdministratorOperation,
  searchAdministratorOperations,
  unassignOperationAdministrator,
} from '../../api/administrator/operations';
import { searchAccountsForPicker } from '../../api/accounts';
import type { OperationDetails } from '../../api/types';
import { AdminOperationCard } from '../../components/admin/AdminOperationCard';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function AdminOperationsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [createName, setCreateName] = useState('');
  const [createDescription, setCreateDescription] = useState('');
  const [createBusy, setCreateBusy] = useState(false);

  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationDetails[]>([]);
  const [accountLabels, setAccountLabels] = useState<Record<string, string>>({});
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const [actionBusy, setActionBusy] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteOperationId, setDeleteOperationId] = useState('');

  const [assignPickerOpen, setAssignPickerOpen] = useState(false);
  const [assignOperationId, setAssignOperationId] = useState('');
  const assignOperation = items.find((item) => item.id === assignOperationId) ?? null;
  const assignDisabledIds = new Set(assignOperation?.administratorIds ?? []);

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

  useEffect(() => {
    const adminIds = [...new Set(items.flatMap((item) => item.administratorIds))];
    if (adminIds.length === 0) return;

    void (async () => {
      const result = await searchAccountsForPicker({ limit: 500, offset: 0, keyword: null });
      if (!result.ok) return;

      const labels: Record<string, string> = {};
      for (const account of result.data?.items ?? []) {
        if (adminIds.includes(account.id)) {
          labels[account.id] = account.username;
        }
      }
      setAccountLabels((prev) => ({ ...prev, ...labels }));
    })();
  }, [items]);

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

  function openDeleteDialog(operationId: string) {
    setDeleteOperationId(operationId);
    setDeleteDialogOpen(true);
  }

  async function confirmDelete() {
    setDeleteDialogOpen(false);
    if (!deleteOperationId) return;

    setActionBusy(true);
    try {
      const result = await deleteAdministratorOperation(deleteOperationId);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Operação excluída do sistema.');
      setDeleteOperationId('');
      await load(currentPage, query);
    } finally {
      setActionBusy(false);
    }
  }

  function openAssignPicker(operationId: string) {
    setAssignOperationId(operationId);
    setAssignPickerOpen(true);
  }

  async function handleAssignAdministrator(administratorId: string, username: string) {
    if (!assignOperationId) return;

    setActionBusy(true);
    try {
      const result = await assignOperationAdministrator(assignOperationId, administratorId);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Administrador vinculado à operação.');
      setAccountLabels((prev) => ({ ...prev, [administratorId]: username }));
      await load(currentPage, query);
    } finally {
      setActionBusy(false);
      setAssignPickerOpen(false);
      setAssignOperationId('');
    }
  }

  async function handleRemoveAdministrator(operationId: string, administratorId: string) {
    setActionBusy(true);
    try {
      const result = await unassignOperationAdministrator(operationId, administratorId);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Administrador removido da operação.');
      await load(currentPage, query);
    } finally {
      setActionBusy(false);
    }
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker page-kicker-admin">Administração</p>
          <h1>Todas as operações</h1>
          <p className="muted page-lead">
            Visão global do repositório. Registre operações, vincule administradores responsáveis ou remova registros obsoletos.
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
            <div className="admin-op-list">
              {items.map((op) => (
                <AdminOperationCard
                  key={op.id}
                  operation={op}
                  accountLabels={accountLabels}
                  actionBusy={actionBusy}
                  onAssignAdministrator={openAssignPicker}
                  onRemoveAdministrator={(operationId, administratorId) => void handleRemoveAdministrator(operationId, administratorId)}
                  onDelete={openDeleteDialog}
                />
              ))}
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

      <AccountPickerModal
        open={assignPickerOpen}
        onClose={() => {
          setAssignPickerOpen(false);
          setAssignOperationId('');
        }}
        title="Vincular administrador"
        subtitle="Selecione a conta que passará a administrar esta operação."
        disabledAccountIds={assignDisabledIds}
        disabledBadgeText="Já vinculado"
        onSelected={(row) => void handleAssignAdministrator(row.id, row.username)}
      />

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
