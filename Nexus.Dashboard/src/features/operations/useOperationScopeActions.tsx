import { useMemo, useState, type ReactNode } from 'react';
import {
  assignOperationAdministrator,
  deleteAdministratorOperation,
  unassignOperationAdministrator,
} from '../../api/administrator/operations';
import { createOperationTeam, deleteOperationTeam } from '../../api/administrator/teams';
import {
  searchAdministratorAccountsPicker,
  createTeamLeaderOperatorsPicker,
  createTeamLeaderProfitShareAccountsPicker,
} from '../../api/accountPickerSources';
import {
  createOperationTeam as opAdminCreateTeam,
  deleteOperationTeam as opAdminDeleteTeam,
} from '../../api/operationAdministrator/teams';
import {
  assignOperatorToTeam as teamLeaderAssignOperator,
  setOperatorProfitShareRule as teamLeaderSetProfitShare,
  unassignOperatorFromTeam as teamLeaderUnassignOperator,
} from '../../api/teamLeader/teams';
import type { OperationDetails, OperationWithLedTeamsDetails, OperatorDetails, ProfitShareCutInput } from '../../api/types';
import type { AdminOperationCardActions } from '../../components/admin/AdminOperationCard';
import { ProfitShareRuleModal, type ProfitShareCutDraft } from '../../components/admin/ProfitShareRuleModal';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useNotifications } from '../../notifications/NotificationContext';
import { fetchOperationById } from './fetchOperationById';
import { cardScope, type OperationScope } from './operationPaths';

const noop = () => undefined;

type AccountPickerMode =
  | { kind: 'admin'; operationId: string }
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

  const cardScopeValue = cardScope(scope);

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
        onAssignAdministrator: (operationId) => setAccountPickerMode({ kind: 'admin', operationId }),
        onRemoveAdministrator: (operationId, administratorId) => {
          void runAction(
            () => unassignOperationAdministrator(operationId, administratorId),
            'Administrador removido da operação.',
          );
        },
        onDelete: openDeleteDialog,
        onCreateTeam: createTeam,
        onDeleteTeam: openDeleteTeamDialog,
        ...teamPanelNoops,
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
      onUnassignOperator: (teamId, operatorId) => {
        void runAction(
          () => teamLeaderUnassignOperator(teamId, operatorId),
          'Operador removido da equipe.',
        );
      },
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
        <ConfirmDialog
          open={deleteTeamDialogOpen}
          title="Excluir equipe"
          message="Esta ação remove a equipe e todos os vínculos associados. Deseja continuar?"
          onCancel={() => { setDeleteTeamDialogOpen(false); setDeleteTeamId(''); }}
          onConfirm={() => void confirmDeleteTeam()}
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
