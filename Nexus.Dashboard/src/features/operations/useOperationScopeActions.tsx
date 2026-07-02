import { useMemo, useState, type ReactNode } from 'react';
import {
  assignOperationAdministrator,
  deleteAdministratorOperation,
  unassignOperationAdministrator,
} from '../../api/administrator/operations';
import {
  assignGatewayAccountGroupToOperation,
  assignGatewayAccountToOperation,
  assignStrawManToOperation,
  setOperationGatewaySelectionStrategy,
  unassignGatewayAccountFromOperation,
  unassignGatewayAccountGroupFromOperation,
  unassignStrawManFromOperation,
} from '../../api/administrator/operationGateway';
import { createOperationTeam, deleteOperationTeam } from '../../api/administrator/teams';
import {
  searchAdministratorAccountsPicker,
  createTeamLeaderOperatorsPicker,
  createTeamLeaderProfitShareAccountsPicker,
  searchOpAdminStrawMenPicker,
} from '../../api/accountPickerSources';
import {
  assignGatewayAccountGroupToOperation as opAdminAssignGatewayGroup,
  assignGatewayAccountToOperation as opAdminAssignGateway,
  assignStrawManToOperation as opAdminAssignStrawMan,
  setOperationGatewaySelectionStrategy as opAdminSetGatewayStrategy,
  unassignGatewayAccountFromOperation as opAdminUnassignGateway,
  unassignGatewayAccountGroupFromOperation as opAdminUnassignGatewayGroup,
  unassignStrawManFromOperation as opAdminUnassignStrawMan,
} from '../../api/operations/operationAdministrator/operationGateway';
import {
  createOperationTeam as opAdminCreateTeam,
  deleteOperationTeam as opAdminDeleteTeam,
} from '../../api/operations/operationAdministrator/teams';
import {
  assignOperatorToTeam as teamLeaderAssignOperator,
  setOperatorProfitShareRule as teamLeaderSetProfitShare,
  unassignOperatorFromTeam as teamLeaderUnassignOperator,
} from '../../api/operations/teamLeader/teams';
import type { OperationDetails, OperationWithLedTeamsDetails, OperatorDetails, ProfitShareCutInput } from '../../api/types';
import type { AdminOperationCardActions } from '../../components/admin/AdminOperationCard';
import { ProfitShareRuleModal, type ProfitShareCutDraft } from '../../components/admin/ProfitShareRuleModal';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { GatewayCredentialPickerModal } from '../../components/GatewayCredentialPickerModal';
import { useNotifications } from '../../notifications/NotificationContext';
import { fetchOperationById } from './fetchOperationById';
import { cardScope, type OperationScope } from './operationPaths';

const noop = () => undefined;

type AccountPickerMode =
  | { kind: 'admin'; operationId: string }
  | { kind: 'strawMan'; operationId: string }
  | { kind: 'operator'; teamId: string }
  | { kind: 'profitShareCut'; cutIndex: number };

type UseOperationScopeActionsOptions = {
  scope: OperationScope;
  mode: 'list' | 'detail';
  operation?: OperationDetails | OperationWithLedTeamsDetails | null;
  onMutated: () => void | Promise<void>;
  onOperationDeleted?: () => void;
  onTeamCreated?: (teamId: string) => void;
};

const teamPanelNoops: Omit<AdminOperationCardActions, 'busy' | 'onAssignAdministrator' | 'onRemoveAdministrator' | 'onDelete' | 'onCreateTeam' | 'onDeleteTeam'> = {
  onAssignLeader: noop,
  onUnassignLeader: noop,
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

export function useOperationScopeActions({
  scope,
  mode,
  operation = null,
  onMutated,
  onOperationDeleted,
  onTeamCreated,
}: UseOperationScopeActionsOptions) {
  const { notifyError, notifySuccess } = useNotifications();
  const [actionBusy, setActionBusy] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteOperationId, setDeleteOperationId] = useState('');
  const [deleteTeamDialogOpen, setDeleteTeamDialogOpen] = useState(false);
  const [deleteTeamId, setDeleteTeamId] = useState('');
  const [accountPickerMode, setAccountPickerMode] = useState<AccountPickerMode | null>(null);
  const [profitShareOpen, setProfitShareOpen] = useState(false);
  const [profitShareTeamId, setProfitShareTeamId] = useState('');
  const [profitShareOperator, setProfitShareOperator] = useState<OperatorDetails | null>(null);
  const [profitShareCuts, setProfitShareCuts] = useState<ProfitShareCutDraft[]>([]);
  const [gatewayPickerOpen, setGatewayPickerOpen] = useState(false);
  const [unassignOperatorTarget, setUnassignOperatorTarget] = useState<{
    teamId: string;
    operatorId: string;
  } | null>(null);

  const cardScopeValue = cardScope(scope);
  const operationId = operation?.id ?? '';

  async function runAction(task: () => Promise<{ ok: boolean; error?: string }>, successMessage: string) {
    setActionBusy(true);
    try {
      const result = await task();
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess(successMessage);
      await onMutated();
    } finally {
      setActionBusy(false);
    }
  }

  function openDeleteDialog(operationId: string) {
    setDeleteOperationId(operationId);
    setDeleteDialogOpen(true);
  }

  function openDeleteTeamDialog(teamId: string) {
    setDeleteTeamId(teamId);
    setDeleteTeamDialogOpen(true);
  }

  async function confirmDeleteOperation() {
    setDeleteDialogOpen(false);
    const id = deleteOperationId;
    setDeleteOperationId('');
    if (!id) return;
    setActionBusy(true);
    try {
      const result = await deleteAdministratorOperation(id);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Operação excluída do sistema.');
      onOperationDeleted?.();
      await onMutated();
    } finally {
      setActionBusy(false);
    }
  }

  async function confirmDeleteTeam() {
    setDeleteTeamDialogOpen(false);
    const teamId = deleteTeamId;
    setDeleteTeamId('');
    if (!teamId) return;
    const deleteFn = scope === 'global-admin' ? deleteOperationTeam : opAdminDeleteTeam;
    void runAction(() => deleteFn(teamId), 'Equipe excluída.');
  }

  function openUnassignOperatorDialog(teamId: string, operatorId: string) {
    setUnassignOperatorTarget({ teamId, operatorId });
  }

  async function confirmUnassignOperator() {
    if (!unassignOperatorTarget) return;
    const { teamId, operatorId } = unassignOperatorTarget;
    setUnassignOperatorTarget(null);
    await runAction(
      () => teamLeaderUnassignOperator(teamId, operatorId),
      'Operador removido da equipe.',
    );
  }

  const unassignOperatorName = unassignOperatorTarget && operation && 'teams' in operation
    ? operation.teams
      .find((team) => team.id === unassignOperatorTarget.teamId)
      ?.operators.find((op) => op.accountId === unassignOperatorTarget.operatorId)
      ?.username
    : undefined;

  function createTeam(operationId: string, name: string) {
    void (async () => {
      setActionBusy(true);
      try {
        const createFn = scope === 'global-admin' ? createOperationTeam : opAdminCreateTeam;
        const result = await createFn(operationId, name);
        if (!result.ok) {
          notifyError(result.error ?? 'Não foi possível concluir a ação.');
          return;
        }
        notifySuccess('Equipe criada.');
        await onMutated();
        if (onTeamCreated) {
          const op = await fetchOperationById(scope, operationId);
          const team = op?.teams.find((t) => t.name.trim() === name.trim());
          if (team) onTeamCreated(team.id);
        }
      } finally {
        setActionBusy(false);
      }
    })();
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
      const result = await teamLeaderSetProfitShare(
        profitShareTeamId,
        profitShareOperator.accountId,
        cuts,
      );
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Regra de repasse atualizada.');
      setProfitShareOpen(false);
      setProfitShareOperator(null);
      await onMutated();
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
      if (accountPickerMode.kind === 'admin') {
        result = await assignOperationAdministrator(accountPickerMode.operationId, accountId);
      } else if (accountPickerMode.kind === 'strawMan') {
        const assignFn = scope === 'global-admin' ? assignStrawManToOperation : opAdminAssignStrawMan;
        result = await assignFn(accountPickerMode.operationId, accountId);
      } else if (scope === 'team-leader' && accountPickerMode.kind === 'operator') {
        result = await teamLeaderAssignOperator(accountPickerMode.teamId, accountId);
      } else {
        return;
      }

      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }

      const messages: Record<string, string> = {
        admin: 'Administrador vinculado à operação.',
        strawMan: 'Laranja vinculado à operação.',
        operator: 'Operador alocado na equipe.',
      };
      notifySuccess(messages[accountPickerMode.kind] ?? 'Ação concluída.');
      await onMutated();
    } finally {
      setActionBusy(false);
      setAccountPickerMode(null);
    }
  }

  const assignDisabledIds = useMemo(
    () => new Set(
      operation && 'administrators' in operation
        ? (operation.administrators ?? []).map((admin) => admin.accountId)
        : [],
    ),
    [operation],
  );

  const operationGatewayActions = {
    onGatewayStrategyChange: (id: string, strategy: Parameters<typeof setOperationGatewaySelectionStrategy>[1]) => {
      void runAction(
        () => (scope === 'global-admin'
          ? setOperationGatewaySelectionStrategy(id, strategy)
          : opAdminSetGatewayStrategy(id, strategy)),
        'Estratégia de gateway da operação atualizada.',
      );
    },
    onAssignStrawMan: (id: string) => setAccountPickerMode({ kind: 'strawMan', operationId: id }),
    onUnassignStrawMan: (id: string, accountId: string) => {
      void runAction(
        () => (scope === 'global-admin'
          ? unassignStrawManFromOperation(id, accountId)
          : opAdminUnassignStrawMan(id, accountId)),
        'Laranja removido da operação.',
      );
    },
    onAssignGatewayCredential: (_id: string) => setGatewayPickerOpen(true),
    onUnassignGatewayCredential: (id: string, credentialId: string) => {
      void runAction(
        () => (scope === 'global-admin'
          ? unassignGatewayAccountFromOperation(id, credentialId)
          : opAdminUnassignGateway(id, credentialId)),
        'Credencial removida da operação.',
      );
    },
    onAssignGatewayGroup: (id: string, groupId: string) => {
      void runAction(
        () => (scope === 'global-admin'
          ? assignGatewayAccountGroupToOperation(id, groupId)
          : opAdminAssignGatewayGroup(id, groupId)),
        'Grupo de credenciais vinculado.',
      );
    },
    onUnassignGatewayGroup: (id: string, groupId: string) => {
      void runAction(
        () => (scope === 'global-admin'
          ? unassignGatewayAccountGroupFromOperation(id, groupId)
          : opAdminUnassignGatewayGroup(id, groupId)),
        'Grupo de credenciais removido.',
      );
    },
  };

  const assignGatewayFn = scope === 'global-admin'
    ? assignGatewayAccountToOperation
    : opAdminAssignGateway;

  const cardActions: AdminOperationCardActions = (() => {
    if (mode === 'list' || scope === 'operator') {
      return {
        busy: actionBusy,
        onAssignAdministrator: noop,
        onRemoveAdministrator: noop,
        onDelete: noop,
        onCreateTeam: noop,
        onDeleteTeam: noop,
        ...teamPanelNoops,
      };
    }

    if (scope === 'global-admin') {
      return {
        busy: actionBusy,
        onAssignAdministrator: (opId) => setAccountPickerMode({ kind: 'admin', operationId: opId }),
        onRemoveAdministrator: (opId, administratorId) => {
          void runAction(
            () => unassignOperationAdministrator(opId, administratorId),
            'Administrador removido da operação.',
          );
        },
        onDelete: openDeleteDialog,
        onCreateTeam: createTeam,
        onDeleteTeam: openDeleteTeamDialog,
        ...teamPanelNoops,
        ...operationGatewayActions,
      };
    }

    if (scope === 'operation-admin') {
      return {
        busy: actionBusy,
        onAssignAdministrator: noop,
        onRemoveAdministrator: noop,
        onDelete: noop,
        onCreateTeam: createTeam,
        onDeleteTeam: openDeleteTeamDialog,
        ...teamPanelNoops,
        ...operationGatewayActions,
      };
    }

    return {
      busy: actionBusy,
      onAssignAdministrator: noop,
      onRemoveAdministrator: noop,
      onDelete: noop,
      onCreateTeam: noop,
      onDeleteTeam: noop,
      onAssignLeader: noop,
      onUnassignLeader: noop,
      onAssignOperator: (teamId) => setAccountPickerMode({ kind: 'operator', teamId }),
      onUnassignOperator: openUnassignOperatorDialog,
      onEditProfitShare: openProfitShare,
      onGatewayStrategyChange: noop,
      onAssignStrawMan: noop,
      onUnassignStrawMan: noop,
      onAssignGatewayCredential: noop,
      onUnassignGatewayCredential: noop,
      onAssignGatewayGroup: noop,
      onUnassignGatewayGroup: noop,
    };
  })();

  const operatorPickerSearch = useMemo(() => {
    if (scope === 'team-leader' && accountPickerMode?.kind === 'operator') {
      return createTeamLeaderOperatorsPicker(accountPickerMode.teamId);
    }
    return searchAdministratorAccountsPicker;
  }, [accountPickerMode, scope]);

  const profitSharePickerSearch = useMemo(
    () => createTeamLeaderProfitShareAccountsPicker(profitShareTeamId),
    [profitShareTeamId],
  );

  const strawManPickerSearch = scope === 'global-admin'
    ? searchAdministratorAccountsPicker
    : searchOpAdminStrawMenPicker;

  const modals: ReactNode = mode === 'list' ? (
    scope === 'global-admin' ? (
      <ConfirmDialog
        open={deleteDialogOpen}
        title="Excluir operação"
        message="Esta ação remove a operação do sistema. Deseja continuar?"
        onCancel={() => { setDeleteDialogOpen(false); setDeleteOperationId(''); }}
        onConfirm={() => void confirmDeleteOperation()}
      />
    ) : null
  ) : (
    <>
      {scope === 'global-admin' ? (
        <AccountPickerModal
          open={accountPickerMode?.kind === 'admin'}
          onClose={() => setAccountPickerMode(null)}
          searchAccounts={searchAdministratorAccountsPicker}
          title="Vincular administrador"
          subtitle="Conta que administrará esta operação."
          disabledAccountIds={assignDisabledIds}
          disabledBadgeText="Já vinculado"
          onSelected={(row) => void handleAccountPicked(row.id, row.username)}
        />
      ) : null}

      {scope === 'team-leader' ? (
        <>
          <AccountPickerModal
            open={accountPickerMode?.kind === 'operator'}
            onClose={() => setAccountPickerMode(null)}
            searchAccounts={operatorPickerSearch}
            title="Alocar operador"
            subtitle="Conta que operará nesta equipe."
            onSelected={(row) => void handleAccountPicked(row.id, row.username)}
          />
          <AccountPickerModal
            open={accountPickerMode?.kind === 'profitShareCut'}
            onClose={() => setAccountPickerMode(null)}
            searchAccounts={profitSharePickerSearch}
            title="Conta do repasse"
            subtitle="Beneficiário desta fatia da regra de repasse."
            onSelected={(row) => void handleAccountPicked(row.id, row.username)}
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
        </>
      ) : null}

      {scope === 'global-admin' ? (
        <ConfirmDialog
          open={deleteDialogOpen}
          title="Excluir operação"
          message="Esta ação remove a operação do sistema. Deseja continuar?"
          onCancel={() => { setDeleteDialogOpen(false); setDeleteOperationId(''); }}
          onConfirm={() => void confirmDeleteOperation()}
        />
      ) : null}

      {scope === 'global-admin' || scope === 'operation-admin' ? (
        <>
          <AccountPickerModal
            open={accountPickerMode?.kind === 'strawMan'}
            onClose={() => setAccountPickerMode(null)}
            searchAccounts={strawManPickerSearch}
            title="Vincular laranja"
            subtitle="Conta laranja usada na estratégia de gateway da operação."
            onSelected={(row) => void handleAccountPicked(row.id, row.username)}
          />
          <GatewayCredentialPickerModal
            open={gatewayPickerOpen}
            onClose={() => setGatewayPickerOpen(false)}
            title="Vincular credencial"
            subtitle="Credencial de gateway para seleção manual da operação."
            onSelected={(row) => {
              setGatewayPickerOpen(false);
              if (!operationId) return;
              void runAction(
                () => assignGatewayFn(operationId, row.id),
                'Credencial vinculada à operação.',
              );
            }}
          />
        </>
      ) : null}

      {scope === 'global-admin' || scope === 'operation-admin' ? (
        <ConfirmDialog
          open={deleteTeamDialogOpen}
          title="Excluir equipe"
          message="Esta ação remove a equipe e todos os vínculos associados. Deseja continuar?"
          onCancel={() => { setDeleteTeamDialogOpen(false); setDeleteTeamId(''); }}
          onConfirm={() => void confirmDeleteTeam()}
        />
      ) : null}

      {scope === 'team-leader' ? (
        <ConfirmDialog
          open={unassignOperatorTarget !== null}
          title="Remover operador"
          message={`Deseja remover${unassignOperatorName ? ` ${unassignOperatorName}` : ' este operador'} da equipe? A regra de repasse também será excluída.`}
          onCancel={() => setUnassignOperatorTarget(null)}
          onConfirm={() => void confirmUnassignOperator()}
        />
      ) : null}
    </>
  );

  return {
    cardActions,
    modals,
    cardScope: cardScopeValue,
    requestDeleteOperation: openDeleteDialog,
  };
}
