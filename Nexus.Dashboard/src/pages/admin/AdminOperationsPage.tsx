import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  assignOperationAdministrator,
  createAdministratorOperation,
  deleteAdministratorOperation,
  searchAdministratorOperations,
  unassignOperationAdministrator,
} from '../../api/administrator/operations';
import { searchAdministratorAccountsPicker } from '../../api/accountPickerSources';
import {
  assignOperationTeamLeader,
  createOperationTeam,
  deleteOperationTeam,
  unassignOperationTeamLeader,
} from '../../api/operationAdministrator/teams';
import {
  assignGatewayAccountToTeam,
  assignGatewayAccountGroupToTeam,
  assignOperatorToTeam,
  assignStrawManToTeam,
  setOperatorProfitShareRule,
  setTeamGatewaySelectionStrategy,
  unassignGatewayAccountFromTeam,
  unassignGatewayAccountGroupFromTeam,
  unassignOperatorFromTeam,
  unassignStrawManFromTeam,
} from '../../api/teamLeader/teams';
import type { OperationDetails, OperatorDetails, ProfitShareCutInput } from '../../api/types';
import { AdminOperationCard, type AdminOperationCardActions } from '../../components/admin/AdminOperationCard';
import { ProfitShareRuleModal, type ProfitShareCutDraft } from '../../components/admin/ProfitShareRuleModal';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { GatewayCredentialPickerModal } from '../../components/GatewayCredentialPickerModal';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

type AccountPickerMode =
  | { kind: 'admin'; operationId: string }
  | { kind: 'leader'; teamId: string }
  | { kind: 'operator'; teamId: string }
  | { kind: 'strawMan'; teamId: string }
  | { kind: 'profitShareCut'; cutIndex: number };

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

  const [actionBusy, setActionBusy] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteOperationId, setDeleteOperationId] = useState('');
  const [deleteTeamDialogOpen, setDeleteTeamDialogOpen] = useState(false);
  const [deleteTeamId, setDeleteTeamId] = useState('');

  const [accountPickerMode, setAccountPickerMode] = useState<AccountPickerMode | null>(null);
  const [gatewayPickerTeamId, setGatewayPickerTeamId] = useState<string | null>(null);

  const [profitShareOpen, setProfitShareOpen] = useState(false);
  const [profitShareTeamId, setProfitShareTeamId] = useState('');
  const [profitShareOperator, setProfitShareOperator] = useState<OperatorDetails | null>(null);
  const [profitShareCuts, setProfitShareCuts] = useState<ProfitShareCutDraft[]>([]);

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

  const assignOperation = useMemo(() => {
    if (accountPickerMode?.kind !== 'admin') return null;
    return items.find((item) => item.id === accountPickerMode.operationId) ?? null;
  }, [accountPickerMode, items]);

  const assignDisabledIds = useMemo(
    () => new Set(assignOperation?.administrators.map((admin) => admin.accountId) ?? []),
    [assignOperation],
  );

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

  async function handleCreate() {
    setCreateBusy(true);
    try {
      const result = await createAdministratorOperation(
        createName.trim(),
        createDescription.trim() || null,
      );
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
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
    await runAction(
      () => deleteAdministratorOperation(deleteOperationId),
      'Operação excluída do sistema.',
    );
    setDeleteOperationId('');
  }

  function openProfitShare(teamId: string, operator: OperatorDetails) {
    setProfitShareTeamId(teamId);
    setProfitShareOperator(operator);
    setProfitShareCuts((operator.profitShareRule?.cuts ?? []).map((cut) => ({
      accountId: cut.accountId,
      percentage: cut.percentage,
      label: cut.username,
    })));
    setProfitShareOpen(true);
  }

  async function saveProfitShare(cuts: ProfitShareCutInput[]) {
    if (!profitShareOperator) return;
    setActionBusy(true);
    try {
      const result = await setOperatorProfitShareRule(profitShareTeamId, profitShareOperator.accountId, cuts);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Regra de repasse atualizada.');
      setProfitShareOpen(false);
      setProfitShareOperator(null);
      await refresh();
    } finally {
      setActionBusy(false);
    }
  }

  async function handleAccountPicked(accountId: string, username: string) {
    if (!accountPickerMode) return;

    if (accountPickerMode.kind === 'profitShareCut') {
      setProfitShareCuts((prev) => prev.map((cut, index) => (
        index === accountPickerMode.cutIndex
          ? { ...cut, accountId, label: username }
          : cut
      )));
      setAccountPickerMode(null);
      return;
    }

    setActionBusy(true);
    try {
      let result: { ok: boolean; error?: string };
      switch (accountPickerMode.kind) {
        case 'admin':
          result = await assignOperationAdministrator(accountPickerMode.operationId, accountId);
          break;
        case 'leader':
          result = await assignOperationTeamLeader(accountPickerMode.teamId, accountId);
          break;
        case 'operator':
          result = await assignOperatorToTeam(accountPickerMode.teamId, accountId);
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
        admin: 'Administrador vinculado à operação.',
        leader: 'Líder vinculado à equipe.',
        operator: 'Operador alocado na equipe.',
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
    onAssignAdministrator: (operationId) => setAccountPickerMode({ kind: 'admin', operationId }),
    onRemoveAdministrator: (operationId, administratorId) => {
      void runAction(
        () => unassignOperationAdministrator(operationId, administratorId),
        'Administrador removido da operação.',
      );
    },
    onDelete: openDeleteDialog,
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
    onAssignOperator: (teamId) => setAccountPickerMode({ kind: 'operator', teamId }),
    onUnassignOperator: (teamId, operatorId) => {
      void runAction(
        () => unassignOperatorFromTeam(teamId, operatorId),
        'Operador removido da equipe.',
      );
    },
    onEditProfitShare: openProfitShare,
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

  const pickerKind = accountPickerMode && accountPickerMode.kind !== 'profitShareCut'
    ? accountPickerMode.kind
    : null;

  const accountPickerTitles = {
    admin: {
      title: 'Vincular administrador',
      subtitle: 'Conta que administrará esta operação.',
    },
    leader: {
      title: 'Vincular líder',
      subtitle: 'Conta responsável por liderar a equipe.',
    },
    operator: {
      title: 'Alocar operador',
      subtitle: 'Conta que operará nesta equipe.',
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
          <p className="page-kicker page-kicker-admin">Administração</p>
          <h1>Todas as operações</h1>
          <p className="muted page-lead">
            Gestão completa do repositório: administradores, equipes, operadores, repasses e configuração de gateway.
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
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Atualizar</button>
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
            <div className="admin-op-list admin-op-list--single">
              {items.map((op) => (
                <AdminOperationCard key={op.id} operation={op} actions={cardActions} />
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
        searchAccounts={searchAdministratorAccountsPicker}
        title={pickerKind ? accountPickerTitles[pickerKind].title : 'Selecionar conta'}
        subtitle={pickerKind ? accountPickerTitles[pickerKind].subtitle : undefined}
        disabledAccountIds={pickerKind === 'admin' ? assignDisabledIds : undefined}
        disabledBadgeText="Já vinculado"
        onSelected={(row) => void handleAccountPicked(row.id, row.username)}
      />

      <AccountPickerModal
        open={accountPickerMode?.kind === 'profitShareCut'}
        onClose={() => setAccountPickerMode(null)}
        searchAccounts={searchAdministratorAccountsPicker}
        title="Conta do repasse"
        subtitle="Beneficiário desta fatia da regra de repasse."
        onSelected={(row) => void handleAccountPicked(row.id, row.username)}
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

      <ProfitShareRuleModal
        open={profitShareOpen}
        operatorName={profitShareOperator?.username ?? 'Operador'}
        cuts={profitShareCuts}
        onCutsChange={setProfitShareCuts}
        busy={actionBusy}
        onClose={() => {
          setProfitShareOpen(false);
          setProfitShareOperator(null);
        }}
        onPickAccount={(cutIndex) => setAccountPickerMode({ kind: 'profitShareCut', cutIndex })}
        onSave={(cuts) => void saveProfitShare(cuts)}
      />

      <ConfirmDialog
        open={deleteDialogOpen}
        title="Excluir operação"
        message="Esta ação remove a operação do sistema. Deseja continuar?"
        onCancel={() => { setDeleteDialogOpen(false); setDeleteOperationId(''); }}
        onConfirm={() => void confirmDelete()}
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
