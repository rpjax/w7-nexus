import { useCallback, useEffect, useState } from 'react';
import { searchOperationAdministratorOperations } from '../../api/operationAdministrator/operations';
import {
  assignOperationTeamLeader,
  createOperationTeam,
  deleteOperationTeam,
  unassignOperationTeamLeader,
} from '../../api/operationAdministrator/teams';
import { searchAccountsPicker } from '../../api/accountPickerSources';
import type { OperationDetails } from '../../api/types';
import { AdminOperationCard, type AdminOperationCardActions } from '../../components/admin/AdminOperationCard';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

const noop = () => undefined;

type AccountPickerMode = { kind: 'leader'; teamId: string };

export function OperationAdminOperationsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const [actionBusy, setActionBusy] = useState(false);
  const [deleteTeamDialogOpen, setDeleteTeamDialogOpen] = useState(false);
  const [deleteTeamId, setDeleteTeamId] = useState('');
  const [accountPickerMode, setAccountPickerMode] = useState<AccountPickerMode | null>(null);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchOperationAdministratorOperations({
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

  async function refresh() {
    await load(currentPage, query);
  }

  async function runAction(task: () => Promise<{ ok: boolean; error?: string }>, successMessage: string) {
    setActionBusy(true);
    try {
      const result = await task();
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess(successMessage);
      await refresh();
    } finally {
      setActionBusy(false);
    }
  }

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  async function handleAccountPicked(accountId: string) {
    if (!accountPickerMode) return;

    setActionBusy(true);
    try {
      const result = await assignOperationTeamLeader(accountPickerMode.teamId, accountId);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Líder vinculado à equipe.');
      await refresh();
    } finally {
      setActionBusy(false);
      setAccountPickerMode(null);
    }
  }

  const cardActions: AdminOperationCardActions = {
    busy: actionBusy,
    onAssignAdministrator: noop,
    onRemoveAdministrator: noop,
    onDelete: noop,
    onCreateTeam: (operationId, name) => {
      void runAction(() => createOperationTeam(operationId, name), 'Equipe criada.');
    },
    onDeleteTeam: (teamId) => {
      setDeleteTeamId(teamId);
      setDeleteTeamDialogOpen(true);
    },
    onAssignLeader: (teamId) => setAccountPickerMode({ kind: 'leader', teamId }),
    onUnassignLeader: (teamId) => {
      void runAction(() => unassignOperationTeamLeader(teamId), 'Líder removido da equipe.');
    },
    onAssignOperator: noop,
    onUnassignOperator: noop,
    onEditProfitShare: noop,
    onGatewayStrategyChange: noop,
    onAssignStrawMan: noop,
    onUnassignStrawMan: noop,
    onAssignGatewayCredential: noop,
    onUnassignGatewayCredential: noop,
    onAssignGatewayGroup: noop,
    onUnassignGatewayGroup: noop,
  };

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker">Administração de operações</p>
          <h1>Operações administradas</h1>
          <p className="muted page-lead">
            Operações em que você é administrador: crie equipes e defina líderes. Operadores, repasses e gateway ficam com cada líder.
          </p>
        </div>
      </section>

      <section className="card ops-card">
        <div className="toolbar">
          <div className="field grow">
            <label htmlFor="opAdminSearch">Buscar nas suas operações</label>
            <input
              id="opAdminSearch"
              className="nexus-input"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Nome, ID ou descrição…"
            />
          </div>
          <button type="button" className="btn btn-ghost" onClick={() => void handleSearch()}>Buscar</button>
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Atualizar</button>
        </div>

        <div className="card-title-row">
          <div className="card-title-group">
            <h2 className="section-title">Suas operações</h2>
            <span className="post-badge">POST /api/operation-administrator/operations/search</span>
          </div>
          <span className="muted small">{totalItems} registro(s)</span>
        </div>

        {items.length === 0 ? (
          <EmptyState
            title="Nenhuma operação encontrada"
            message="Você ainda não administra nenhuma operação ou o filtro não retornou resultados."
          />
        ) : (
          <>
            <div className="admin-op-list admin-op-list--single">
              {items.map((op) => (
                <AdminOperationCard key={op.id} operation={op} scope="operation-admin" actions={cardActions} />
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
        open={accountPickerMode !== null}
        onClose={() => setAccountPickerMode(null)}
        searchAccounts={searchAccountsPicker}
        title="Vincular líder"
        subtitle="Conta responsável por liderar a equipe."
        onSelected={(row) => void handleAccountPicked(row.id)}
      />

      <ConfirmDialog
        open={deleteTeamDialogOpen}
        title="Excluir equipe"
        message="Esta ação remove a equipe e todos os vínculos associados. Deseja continuar?"
        onCancel={() => { setDeleteTeamDialogOpen(false); setDeleteTeamId(''); }}
        onConfirm={() => {
          setDeleteTeamDialogOpen(false);
          const teamId = deleteTeamId;
          setDeleteTeamId('');
          if (!teamId) return;
          void runAction(() => deleteOperationTeam(teamId), 'Equipe excluída.');
        }}
      />
    </>
  );
}
