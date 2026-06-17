import { useCallback, useEffect, useMemo, useState } from 'react';
import { searchTeamLeaderLedTeams } from '../../api/teamLeader/operations';
import {
  assignOperatorToTeam,
  setOperatorProfitShareRule,
  unassignOperatorFromTeam,
} from '../../api/teamLeader/teams';
import {
  createTeamLeaderOperatorsPicker,
  createTeamLeaderProfitShareAccountsPicker,
} from '../../api/accountPickerSources';
import type { OperationWithLedTeamsDetails, OperatorDetails, ProfitShareCutInput } from '../../api/types';
import { AdminOperationCard, type AdminOperationCardActions } from '../../components/admin/AdminOperationCard';
import { ProfitShareRuleModal, type ProfitShareCutDraft } from '../../components/admin/ProfitShareRuleModal';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { EmptyState } from '../../components/EmptyState';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

const noop = () => undefined;

type AccountPickerMode =
  | { kind: 'operator'; teamId: string }
  | { kind: 'profitShareCut'; cutIndex: number };

export function TeamLeaderOperationsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationWithLedTeamsDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const [actionBusy, setActionBusy] = useState(false);
  const [accountPickerMode, setAccountPickerMode] = useState<AccountPickerMode | null>(null);

  const [profitShareOpen, setProfitShareOpen] = useState(false);
  const [profitShareTeamId, setProfitShareTeamId] = useState('');
  const [profitShareOperator, setProfitShareOperator] = useState<OperatorDetails | null>(null);
  const [profitShareCuts, setProfitShareCuts] = useState<ProfitShareCutDraft[]>([]);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchTeamLeaderLedTeams({
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
      const result = await assignOperatorToTeam(accountPickerMode.teamId, accountId);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Operador alocado na equipe.');
      await refresh();
    } finally {
      setActionBusy(false);
      setAccountPickerMode(null);
    }
  }

  const operatorPickerSearch = useMemo(
    () => accountPickerMode?.kind === 'operator'
      ? createTeamLeaderOperatorsPicker(accountPickerMode.teamId)
      : createTeamLeaderOperatorsPicker(''),
    [accountPickerMode],
  );

  const profitSharePickerSearch = useMemo(
    () => createTeamLeaderProfitShareAccountsPicker(profitShareTeamId),
    [profitShareTeamId],
  );

  const cardActions: AdminOperationCardActions = {
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
        () => unassignOperatorFromTeam(teamId, operatorId),
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

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker">Liderança de equipes</p>
          <h1>Equipes lideradas</h1>
          <p className="muted page-lead">
            Operações agrupadas com as equipes que você lidera. Gerencie operadores e regras de repasse.
          </p>
        </div>
      </section>

      <section className="card ops-card">
        <div className="toolbar">
          <div className="field grow">
            <label htmlFor="teamLeaderSearch">Buscar operações</label>
            <input
              id="teamLeaderSearch"
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
            <h2 className="section-title">Operações e equipes</h2>
            <span className="post-badge">POST /api/team-leader/operations/search</span>
          </div>
          <span className="muted small">{totalItems} operação(ões)</span>
        </div>

        {items.length === 0 ? (
          <EmptyState
            title="Nenhuma equipe liderada"
            message="Você ainda não lidera nenhuma equipe ou o filtro não retornou resultados."
          />
        ) : (
          <>
            <div className="admin-op-list admin-op-list--single">
              {items.map((op) => (
                <AdminOperationCard key={op.id} operation={op} scope="team-leader" actions={cardActions} />
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
  );
}
