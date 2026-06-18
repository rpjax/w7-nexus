import { useMemo, useState, type ReactNode } from 'react';
import {
  assignOperationTeamLeader,
  assignGatewayAccountToTeam,
  assignGatewayAccountGroupToTeam,
  assignStrawManToTeam,
  assignOperatorToTeam,
  deleteOperationTeam,
  setOperatorProfitShareRule,
  setTeamGatewaySelectionStrategy,
  unassignGatewayAccountFromTeam,
  unassignGatewayAccountGroupFromTeam,
  unassignOperationTeamLeader,
  unassignOperatorFromTeam,
  unassignStrawManFromTeam,
} from '../../api/administrator/teams';
import {
  searchAdministratorAccountsPicker,
  searchAdministratorOperatorsPicker,
  searchAdministratorProfitShareAccountsPicker,
  searchOpAdminStrawMenPicker,
  searchOpAdminTeamLeaderCandidatesPicker,
} from '../../api/accountPickerSources';
import {
  assignGatewayAccountToTeam as opAdminAssignGateway,
  assignGatewayAccountGroupToTeam as opAdminAssignGatewayGroup,
  assignOperationTeamLeader as opAdminAssignLeader,
  assignStrawManToTeam as opAdminAssignStrawMan,
  deleteOperationTeam as opAdminDeleteTeam,
  setTeamGatewaySelectionStrategy as opAdminSetGatewayStrategy,
  unassignGatewayAccountFromTeam as opAdminUnassignGateway,
  unassignGatewayAccountGroupFromTeam as opAdminUnassignGatewayGroup,
  unassignOperationTeamLeader as opAdminUnassignLeader,
  unassignStrawManFromTeam as opAdminUnassignStrawMan,
} from '../../api/operationAdministrator/teams';
import type { OperatorDetails, ProfitShareCutInput, TeamDetails } from '../../api/types';
import type { AdminTeamPanelActions } from '../../components/admin/adminTeamTypes';
import { ProfitShareRuleModal, type ProfitShareCutDraft } from '../../components/admin/ProfitShareRuleModal';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { GatewayCredentialPickerModal } from '../../components/GatewayCredentialPickerModal';
import { useNotifications } from '../../notifications/NotificationContext';
import { teamPanelScope, type TeamScope } from './teamPaths';

type TeamAccountPickerMode =
  | { kind: 'leader'; teamId: string }
  | { kind: 'operator'; teamId: string }
  | { kind: 'strawMan'; teamId: string }
  | { kind: 'profitShareCut'; cutIndex: number };

type UseTeamScopeActionsOptions = {
  scope: TeamScope;
  team: TeamDetails | null;
  onMutated: () => void | Promise<void>;
  onTeamDeleted?: () => void;
};

export function useTeamScopeActions({
  scope,
  team,
  onMutated,
  onTeamDeleted,
}: UseTeamScopeActionsOptions) {
  const { notifyError, notifySuccess } = useNotifications();
  const [actionBusy, setActionBusy] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [accountPickerMode, setAccountPickerMode] = useState<TeamAccountPickerMode | null>(null);
  const [gatewayPickerOpen, setGatewayPickerOpen] = useState(false);
  const [profitShareOpen, setProfitShareOpen] = useState(false);
  const [profitShareOperator, setProfitShareOperator] = useState<OperatorDetails | null>(null);
  const [profitShareCuts, setProfitShareCuts] = useState<ProfitShareCutDraft[]>([]);

  const teamId = team?.id ?? '';

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

  function openDeleteDialog() {
    setDeleteDialogOpen(true);
  }

  async function confirmDeleteTeam() {
    setDeleteDialogOpen(false);
    if (!teamId) return;
    const deleteFn = scope === 'global-admin' ? deleteOperationTeam : opAdminDeleteTeam;
    setActionBusy(true);
    try {
      const result = await deleteFn(teamId);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Equipe excluída.');
      onTeamDeleted?.();
      await onMutated();
    } finally {
      setActionBusy(false);
    }
  }

  function openProfitShare(operator: OperatorDetails) {
    setProfitShareOperator(operator);
    setProfitShareCuts((operator.profitShareRule?.cuts ?? []).map((cut) => ({
      accountId: cut.accountId,
      percentage: cut.percentage,
      label: cut.username,
    })));
    setProfitShareOpen(true);
  }

  async function saveProfitShare(cuts: ProfitShareCutInput[]) {
    if (!profitShareOperator || !teamId) return;
    setActionBusy(true);
    try {
      const result = await setOperatorProfitShareRule(teamId, profitShareOperator.accountId, cuts);
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
      if (scope === 'global-admin') {
        switch (accountPickerMode.kind) {
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
      } else {
        switch (accountPickerMode.kind) {
          case 'leader':
            result = await opAdminAssignLeader(accountPickerMode.teamId, accountId);
            break;
          case 'strawMan':
            result = await opAdminAssignStrawMan(accountPickerMode.teamId, accountId);
            break;
          default:
            return;
        }
      }

      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }

      const messages: Record<string, string> = {
        leader: 'Líder vinculado à equipe.',
        operator: 'Operador alocado na equipe.',
        strawMan: 'Laranja vinculado à equipe.',
      };
      notifySuccess(messages[accountPickerMode.kind] ?? 'Ação concluída.');
      await onMutated();
    } finally {
      setActionBusy(false);
      setAccountPickerMode(null);
    }
  }

  const panelScope = teamPanelScope(scope);
  const assignGatewayFn = scope === 'global-admin' ? assignGatewayAccountToTeam : opAdminAssignGateway;

  const teamActions: AdminTeamPanelActions = {
    busy: actionBusy,
    onDeleteTeam: openDeleteDialog,
    onAssignLeader: (id) => setAccountPickerMode({ kind: 'leader', teamId: id }),
    onUnassignLeader: (id) => {
      void runAction(
        () => (scope === 'global-admin' ? unassignOperationTeamLeader(id) : opAdminUnassignLeader(id)),
        'Líder removido da equipe.',
      );
    },
    onAssignOperator: (id) => setAccountPickerMode({ kind: 'operator', teamId: id }),
    onUnassignOperator: (id, operatorId) => {
      void runAction(
        () => unassignOperatorFromTeam(id, operatorId),
        'Operador removido da equipe.',
      );
    },
    onEditProfitShare: (_id, operator) => openProfitShare(operator),
    onGatewayStrategyChange: (id, strategy) => {
      void runAction(
        () => (scope === 'global-admin'
          ? setTeamGatewaySelectionStrategy(id, strategy)
          : opAdminSetGatewayStrategy(id, strategy)),
        'Estratégia de gateway atualizada.',
      );
    },
    onAssignStrawMan: (id) => setAccountPickerMode({ kind: 'strawMan', teamId: id }),
    onUnassignStrawMan: (id, accountId) => {
      void runAction(
        () => (scope === 'global-admin'
          ? unassignStrawManFromTeam(id, accountId)
          : opAdminUnassignStrawMan(id, accountId)),
        'Laranja removido da equipe.',
      );
    },
    onAssignGatewayCredential: (_teamId) => setGatewayPickerOpen(true),
    onUnassignGatewayCredential: (id, credentialId) => {
      void runAction(
        () => (scope === 'global-admin'
          ? unassignGatewayAccountFromTeam(id, credentialId)
          : opAdminUnassignGateway(id, credentialId)),
        'Credencial removida da equipe.',
      );
    },
    onAssignGatewayGroup: (id, groupId) => {
      void runAction(
        () => (scope === 'global-admin'
          ? assignGatewayAccountGroupToTeam(id, groupId)
          : opAdminAssignGatewayGroup(id, groupId)),
        'Grupo de credenciais vinculado.',
      );
    },
    onUnassignGatewayGroup: (id, groupId) => {
      void runAction(
        () => (scope === 'global-admin'
          ? unassignGatewayAccountGroupFromTeam(id, groupId)
          : opAdminUnassignGatewayGroup(id, groupId)),
        'Grupo de credenciais removido.',
      );
    },
  };

  const pickerKind = accountPickerMode && accountPickerMode.kind !== 'profitShareCut'
    ? accountPickerMode.kind
    : null;

  const accountPickerSearch = useMemo(() => {
    if (scope === 'global-admin') {
      return pickerKind === 'operator' ? searchAdministratorOperatorsPicker : searchAdministratorAccountsPicker;
    }
    return pickerKind === 'strawMan' ? searchOpAdminStrawMenPicker : searchOpAdminTeamLeaderCandidatesPicker;
  }, [pickerKind, scope]);

  const accountPickerTitles = {
    leader: { title: 'Vincular líder', subtitle: 'Conta responsável por liderar a equipe.' },
    operator: { title: 'Alocar operador', subtitle: 'Conta que operará nesta equipe.' },
    strawMan: { title: 'Vincular laranja', subtitle: 'Conta laranja usada na estratégia de gateway.' },
  } as const;

  const modals: ReactNode = (
    <>
      <AccountPickerModal
        open={pickerKind !== null}
        onClose={() => setAccountPickerMode(null)}
        searchAccounts={accountPickerSearch}
        title={pickerKind ? accountPickerTitles[pickerKind].title : 'Selecionar conta'}
        subtitle={pickerKind ? accountPickerTitles[pickerKind].subtitle : undefined}
        onSelected={(row) => void handleAccountPicked(row.id, row.username)}
      />

      {scope === 'global-admin' ? (
        <>
          <AccountPickerModal
            open={accountPickerMode?.kind === 'profitShareCut'}
            onClose={() => setAccountPickerMode(null)}
            searchAccounts={searchAdministratorProfitShareAccountsPicker}
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

      <GatewayCredentialPickerModal
        open={gatewayPickerOpen}
        onClose={() => setGatewayPickerOpen(false)}
        title="Vincular credencial"
        subtitle="Credencial de gateway para seleção manual da equipe."
        onSelected={(row) => {
          setGatewayPickerOpen(false);
          if (!teamId) return;
          void runAction(
            () => assignGatewayFn(teamId, row.id),
            'Credencial vinculada à equipe.',
          );
        }}
      />

      <ConfirmDialog
        open={deleteDialogOpen}
        title="Excluir equipe"
        message="Esta ação remove a equipe e todos os vínculos associados. Deseja continuar?"
        onCancel={() => setDeleteDialogOpen(false)}
        onConfirm={() => void confirmDeleteTeam()}
      />
    </>
  );

  return {
    teamActions,
    modals,
    panelScope,
    requestDeleteTeam: openDeleteDialog,
    actionBusy,
  };
}
