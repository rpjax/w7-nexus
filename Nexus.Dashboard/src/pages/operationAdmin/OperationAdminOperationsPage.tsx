import { useCallback, useEffect, useState } from 'react';
import { searchOperationAdministratorOperations } from '../../api/operationAdministrator/operations';
import {
  assignGatewayAccountToTeam,
  assignGatewayAccountGroupToTeam,
  assignOperationTeamLeader,
  assignStrawManToTeam,
  createOperationTeam,
  deleteOperationTeam,
  setTeamGatewaySelectionStrategy,
  unassignGatewayAccountFromTeam,
  unassignGatewayAccountGroupFromTeam,
  unassignOperationTeamLeader,
  unassignStrawManFromTeam,
} from '../../api/operationAdministrator/teams';
import {
  searchOpAdminStrawMenPicker,
  searchOpAdminTeamLeaderCandidatesPicker,
} from '../../api/accountPickerSources';
import type { OperationDetails } from '../../api/types';
import { AdminOperationCard, type AdminOperationCardActions } from '../../components/admin/AdminOperationCard';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { GatewayCredentialPickerModal } from '../../components/GatewayCredentialPickerModal';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

const noop = () => undefined;

type AccountPickerMode =
  | { kind: 'leader'; teamId: string }
  | { kind: 'strawMan'; teamId: string };

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
  const [gatewayPickerTeamId, setGatewayPickerTeamId] = useState<string | null>(null);

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
      let result: { ok: boolean; error?: string };
      switch (accountPickerMode.kind) {
        case 'leader':
          result = await assignOperationTeamLeader(accountPickerMode.teamId, accountId);
          break;
        case 'strawMan':
          result = await assignStrawManToTeam(accountPickerMode.teamId, accountId);
          break;
        default:
          return;
      }

      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }

      const messages = {
        leader: 'Líder vinculado à equipe.',
        strawMan: 'Laranja vinculado à equipe.',
      } as const;
      notifySuccess(messages[accountPickerMode.kind]);
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
    onGatewayStrategyChange: (teamId, strategy) => {
      void runAction(
        () => setTeamGatewaySelectionStrategy(teamId, strategy),
        'Estratégia de gateway atualizada.',
      );
    },
    onAssignStrawMan: (teamId) => setAccountPickerMode({ kind: 'strawMan', teamId }),
    onUnassignStrawMan: (teamId, accountId) => {
      void runAction(
        () => unassignStrawManFromTeam(teamId, accountId),
        'Laranja removido da equipe.',
      );
    },
    onAssignGatewayCredential: (teamId) => setGatewayPickerTeamId(teamId),
    onUnassignGatewayCredential: (teamId, credentialId) => {
      void runAction(
        () => unassignGatewayAccountFromTeam(teamId, credentialId),
        'Credencial removida da equipe.',
      );
    },
    onAssignGatewayGroup: (teamId, groupId) => {
      void runAction(
        () => assignGatewayAccountGroupToTeam(teamId, groupId),
        'Grupo de credenciais vinculado.',
      );
    },
    onUnassignGatewayGroup: (teamId, groupId) => {
      void runAction(
        () => unassignGatewayAccountGroupFromTeam(teamId, groupId),
        'Grupo de credenciais removido.',
      );
    },
  };

  const pickerKind = accountPickerMode?.kind ?? null;

  const accountPickerSearch = pickerKind === 'strawMan'
    ? searchOpAdminStrawMenPicker
    : searchOpAdminTeamLeaderCandidatesPicker;

  const accountPickerTitles = {
    leader: {
      title: 'Vincular líder',
      subtitle: 'Conta responsável por liderar a equipe.',
    },
    strawMan: {
      title: 'Vincular laranja',
      subtitle: 'Conta laranja usada na estratégia de gateway.',
    },
  } as const;

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker">Administração de operações</p>
          <h1>Operações administradas</h1>
          <p className="muted page-lead">
            Operações em que você é administrador: crie equipes, defina líderes e configure laranjas e credenciais de gateway. Operadores e repasses ficam com cada líder.
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
        open={pickerKind !== null}
        onClose={() => setAccountPickerMode(null)}
        searchAccounts={accountPickerSearch}
        title={pickerKind ? accountPickerTitles[pickerKind].title : 'Selecionar conta'}
        subtitle={pickerKind ? accountPickerTitles[pickerKind].subtitle : undefined}
        onSelected={(row) => void handleAccountPicked(row.id)}
      />

      <GatewayCredentialPickerModal
        open={gatewayPickerTeamId !== null}
        onClose={() => setGatewayPickerTeamId(null)}
        title="Vincular credencial"
        subtitle="Credencial de gateway para seleção manual da equipe."
        onSelected={(row) => {
          const teamId = gatewayPickerTeamId;
          setGatewayPickerTeamId(null);
          if (!teamId) return;
          void runAction(
            () => assignGatewayAccountToTeam(teamId, row.id),
            'Credencial vinculada à equipe.',
          );
        }}
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
