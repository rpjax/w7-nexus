import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  impersonateOlxAd,
  searchOlxOperatorAdSpoofs,
  unimpersonateOlxAd,
  updateOlxAdSpoof,
} from '../../api/olx/operator';
import { searchAdministratorOperationsPicker } from '../../api/operationPickerSources';
import type { OlxOperatorAdSpoofRow } from '../../api/olx/types';
import { useAuth } from '../../auth/AuthContext';
import { isAdministrator } from '../../auth/roles';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ImpersonateAdModal } from '../../components/olx/ImpersonateAdModal';
import { OlxFilterPanel, OlxPickerField } from '../../components/olx/OlxFilterPanel';
import { UpdateSpoofModal } from '../../components/olx/UpdateSpoofModal';
import { OperationPickerModal } from '../../components/OperationPickerModal';
import { PaginationBar } from '../../components/ListControls';
import { AdSpoofListItem } from '../../features/olx/AdSpoofListItem';
import { useOlxOperationLabels } from '../../features/olx/useOlxOperationLabels';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function OlxOperatorAdsPage() {
  const { user } = useAuth();
  const adminView = isAdministrator(user);
  const { notifyError, notifySuccess } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [operationId, setOperationId] = useState('');
  const [operationLabel, setOperationLabel] = useState<string | null>(null);
  const [appliedOperationIds, setAppliedOperationIds] = useState<string[]>([]);
  const [rows, setRows] = useState<OlxOperatorAdSpoofRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [busy, setBusy] = useState(false);
  const [impersonateOpen, setImpersonateOpen] = useState(false);
  const [impersonateSeed, setImpersonateSeed] = useState<{ operationId?: string; adId?: string; adUrl?: string }>({});
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [editRow, setEditRow] = useState<OlxOperatorAdSpoofRow | null>(null);
  const [confirmRelease, setConfirmRelease] = useState<OlxOperatorAdSpoofRow | null>(null);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const extraOperationLabels = useMemo(
    () => (operationLabel && operationId ? { [operationId]: operationLabel } : {}),
    [operationId, operationLabel],
  );
  const operationLabels = useOlxOperationLabels(rows, extraOperationLabels);

  const activeCount = useMemo(
    () => rows.filter((row) => row.isImpersonating).length,
    [rows],
  );

  const load = useCallback(async (page: number, keyword: string, operationIds: string[]) => {
    const result = await searchOlxOperatorAdSpoofs({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
      operationIds,
    });
    if (!result.ok) {
      notifyError(result.error);
      setRows([]);
      setTotalItems(0);
      return;
    }
    setRows(result.data?.items ?? []);
    setTotalItems(result.data?.total ?? 0);
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, query, appliedOperationIds);
  }, [currentPage, query, appliedOperationIds, load]);

  function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
    setAppliedOperationIds(operationId.trim() ? [operationId.trim()] : []);
  }

  function clearOperationFilter() {
    setOperationId('');
    setOperationLabel(null);
    setCurrentPage(1);
    setAppliedOperationIds([]);
  }

  const activeFilters = appliedOperationIds.length > 0 && operationLabel
    ? [{ id: 'operation', label: `Operação: ${operationLabel}` }]
    : appliedOperationIds.length > 0
      ? [{ id: 'operation', label: `Operação: ${appliedOperationIds[0]}` }]
      : [];

  async function runMutation(task: () => Promise<{ ok: boolean; error?: string }>, successMessage: string) {
    setBusy(true);
    const result = await task();
    setBusy(false);
    if (!result.ok) {
      notifyError(result.error ?? 'Operação falhou.');
      return false;
    }
    notifySuccess(successMessage);
    await load(currentPage, query, appliedOperationIds);
    return true;
  }

  async function handleImpersonate(operationIdValue: string, adId: string, adUrl: string) {
    if (!user?.accountId) return;
    const ok = await runMutation(
      () => impersonateOlxAd({
        operationId: operationIdValue,
        adId,
        adUrl,
        operatorId: user.accountId,
      }),
      'Anúncio assumido com sucesso.',
    );
    if (ok) setImpersonateOpen(false);
  }

  async function handleRelease(row: OlxOperatorAdSpoofRow) {
    if (!user?.accountId) return;
    const ok = await runMutation(
      () => unimpersonateOlxAd({
        operationId: row.operationId,
        adId: row.adId,
        operatorId: user.accountId,
      }),
      'Anúncio liberado.',
    );
    if (ok) setConfirmRelease(null);
  }

  async function handleUpdatePrices(originalPrice: number | null, promotionalPrice: number | null) {
    if (!editRow) return;
    const ok = await runMutation(
      () => updateOlxAdSpoof({
        operationId: editRow.operationId,
        adId: editRow.adId,
        originalPrice,
        promotionalPrice,
      }),
      'Preços atualizados.',
    );
    if (ok) setEditRow(null);
  }

  function openImpersonate(row?: OlxOperatorAdSpoofRow) {
    setImpersonateSeed({
      operationId: row?.operationId,
      adId: row?.adId,
      adUrl: row?.adUrl,
    });
    setImpersonateOpen(true);
  }

  return (
    <>
      <OpsWorkspace
        title="Meus anúncios"
        kicker="OLX"
        lead="Assuma anúncios livres, defina preços spoofados e libere slots quando terminar."
        searchId="olx-op-search"
        searchLabel="Buscar"
        searchPlaceholder="ID do anúncio ou operação…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={handleSearch}
        onRefresh={() => void load(currentPage, query, appliedOperationIds)}
        totalItems={totalItems}
        totalLabel={`${totalItems} anúncio(s) · ${activeCount} em controle`}
        onCreate={() => openImpersonate()}
        createLabel="Assumir anúncio"
        footer={totalItems > 0 ? (
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((page) => Math.max(1, page - 1))}
            onNext={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
          />
        ) : undefined}
      >
        <div className="olx-hub-strip" aria-label="Resumo OLX">
          <div className="olx-hub-strip__card">
            <span className="olx-hub-strip__label">Em controle</span>
            <strong className="olx-hub-strip__value">{activeCount}</strong>
          </div>
          <div className="olx-hub-strip__card">
            <span className="olx-hub-strip__label">Total visível</span>
            <strong className="olx-hub-strip__value">{totalItems}</strong>
          </div>
        </div>

        <OlxFilterPanel
          filters={activeFilters}
          onClearFilter={() => clearOperationFilter()}
          onClearAll={activeFilters.length > 0 ? clearOperationFilter : undefined}
        >
          {adminView ? (
            <OlxPickerField
              label="Operação"
              value={operationLabel}
              placeholder="Todas as operações"
              onPick={() => setOperationPickerOpen(true)}
              onClear={operationLabel ? clearOperationFilter : undefined}
            />
          ) : (
            <div className="field grow">
              <label htmlFor="olx-op-operation-filter">Operação</label>
              <input
                id="olx-op-operation-filter"
                className="nexus-input"
                value={operationId}
                onChange={(e) => setOperationId(e.target.value)}
                placeholder="Filtrar por ID da operação"
                onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
              />
            </div>
          )}
          <button type="button" className="btn btn-secondary btn-sm olx-filter-panel__apply" onClick={handleSearch}>
            Aplicar filtros
          </button>
        </OlxFilterPanel>

        {rows.length === 0 ? (
          <EmptyState
            title="Nenhum anúncio spoofado"
            message="Assuma um anúncio livre para começar. Apenas registros sob seu controle aparecem aqui."
          />
        ) : (
          <div className="olx-ad-list">
            {rows.map((row) => (
              <AdSpoofListItem
                key={row.id}
                row={row}
                scope="operator"
                operationLabels={operationLabels}
                currentAccountId={user?.accountId}
                busy={busy}
                onImpersonate={() => openImpersonate(row)}
                onEditPrices={() => setEditRow(row)}
                onUnimpersonate={() => setConfirmRelease(row)}
              />
            ))}
          </div>
        )}
      </OpsWorkspace>

      <OperationPickerModal
        open={operationPickerOpen}
        title="Filtrar por operação"
        subtitle="Mostra apenas anúncios vinculados à operação escolhida."
        searchOperations={searchAdministratorOperationsPicker}
        onClose={() => setOperationPickerOpen(false)}
        onSelected={(row) => {
          setOperationId(row.id);
          setOperationLabel(row.name);
        }}
      />

      <ImpersonateAdModal
        open={impersonateOpen}
        busy={busy}
        defaultOperationId={impersonateSeed.operationId ?? operationId}
        defaultOperationLabel={impersonateSeed.operationId ? operationLabels[impersonateSeed.operationId] ?? operationLabel : operationLabel}
        defaultAdId={impersonateSeed.adId ?? ''}
        defaultAdUrl={impersonateSeed.adUrl ?? ''}
        onClose={() => setImpersonateOpen(false)}
        onSubmit={handleImpersonate}
      />

      <UpdateSpoofModal
        open={editRow !== null}
        busy={busy}
        row={editRow}
        onClose={() => setEditRow(null)}
        onSubmit={handleUpdatePrices}
      />

      <ConfirmDialog
        open={confirmRelease !== null}
        title="Liberar anúncio"
        message="Encerrar a impersonação deste anúncio? Outro operador OLX poderá assumi-lo."
        onCancel={() => setConfirmRelease(null)}
        onConfirm={() => { if (confirmRelease) void handleRelease(confirmRelease); }}
      />
    </>
  );
}
