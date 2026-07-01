import { useCallback, useEffect, useMemo, useState } from 'react';
import { searchAdministratorOlxOperatorsPicker } from '../../api/accountPickerSources';
import { searchAdministratorOperationsPicker } from '../../api/operationPickerSources';
import { adminUnimpersonateOlxAd, searchOlxAdminAdPatches } from '../../api/olx/admin';
import type { OlxAdminAdPatchRow } from '../../api/olx/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { OlxFilterPanel, OlxPickerField } from '../../components/olx/OlxFilterPanel';
import { OperationPickerModal } from '../../components/OperationPickerModal';
import { PaginationBar } from '../../components/ListControls';
import { AdPatchListItem } from '../../features/olx/AdPatchListItem';
import { useOlxOperationLabels, useOlxOperatorLabels } from '../../features/olx/useOlxOperationLabels';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function OlxAdminAdsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [operationId, setOperationId] = useState('');
  const [operationLabel, setOperationLabel] = useState<string | null>(null);
  const [operatorId, setOperatorId] = useState('');
  const [operatorLabel, setOperatorLabel] = useState<string | null>(null);
  const [appliedOperationIds, setAppliedOperationIds] = useState<string[]>([]);
  const [appliedOperatorIds, setAppliedOperatorIds] = useState<string[]>([]);
  const [rows, setRows] = useState<OlxAdminAdPatchRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [busy, setBusy] = useState(false);
  const [forceRelease, setForceRelease] = useState<OlxAdminAdPatchRow | null>(null);
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);
  const extraOperationLabels = useMemo(
    () => (operationLabel && operationId ? { [operationId]: operationLabel } : {}),
    [operationId, operationLabel],
  );
  const extraOperatorLabels = useMemo(
    () => (operatorLabel && operatorId ? { [operatorId]: operatorLabel } : {}),
    [operatorId, operatorLabel],
  );
  const operationLabels = useOlxOperationLabels(rows, extraOperationLabels);
  const operatorLabels = useOlxOperatorLabels(rows, extraOperatorLabels);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const activeCount = useMemo(
    () => rows.filter((row) => row.isImpersonating).length,
    [rows],
  );

  const load = useCallback(async (
    page: number,
    keyword: string,
    operationIds: string[],
    operatorIds: string[],
  ) => {
    const result = await searchOlxAdminAdPatches({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
      operationIds,
      operatorIds,
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
    void load(currentPage, query, appliedOperationIds, appliedOperatorIds);
  }, [currentPage, query, appliedOperationIds, appliedOperatorIds, load]);

  function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
    setAppliedOperationIds(operationId.trim() ? [operationId.trim()] : []);
    setAppliedOperatorIds(operatorId.trim() ? [operatorId.trim()] : []);
  }

  function clearOperationFilter() {
    setOperationId('');
    setOperationLabel(null);
    setCurrentPage(1);
    setAppliedOperationIds([]);
  }

  function clearOperatorFilter() {
    setOperatorId('');
    setOperatorLabel(null);
    setCurrentPage(1);
    setAppliedOperatorIds([]);
  }

  function clearAllFilters() {
    clearOperationFilter();
    clearOperatorFilter();
  }

  const activeFilters = [
    ...(appliedOperationIds.length > 0
      ? [{ id: 'operation', label: `Operação: ${operationLabel ?? appliedOperationIds[0]}` }]
      : []),
    ...(appliedOperatorIds.length > 0
      ? [{ id: 'operator', label: `Operador: ${operatorLabel ? `@${operatorLabel}` : appliedOperatorIds[0]}` }]
      : []),
  ];

  async function handleForceRelease(row: OlxAdminAdPatchRow) {
    if (!row.operatorId) return;
    setBusy(true);
    const result = await adminUnimpersonateOlxAd({
      operationId: row.operationId,
      adId: row.adId,
      operatorId: row.operatorId,
    });
    setBusy(false);
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    notifySuccess('Impersonação encerrada pelo administrador.');
    setForceRelease(null);
    await load(currentPage, query, appliedOperationIds, appliedOperatorIds);
  }

  return (
    <>
      <OpsWorkspace
        className="admin-surface"
        title="Gestão OLX"
        kicker="Administração"
        kickerVariant="admin"
        lead="Visão global dos patches de anúncios. Filtre por operação ou operador OLX e force liberações quando necessário."
        searchId="olx-admin-search"
        searchLabel="Buscar"
        searchPlaceholder="Anúncio, operação ou operador…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={handleSearch}
        onRefresh={() => void load(currentPage, query, appliedOperationIds, appliedOperatorIds)}
        totalItems={totalItems}
        totalLabel={`${totalItems} registro(s) · ${activeCount} impersonando`}
        footer={totalItems > 0 ? (
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((page) => Math.max(1, page - 1))}
            onNext={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
          />
        ) : undefined}
      >
        <div className="olx-hub-strip olx-hub-strip--admin" aria-label="Resumo OLX admin">
          <div className="olx-hub-strip__card">
            <span className="olx-hub-strip__label">Impersonando agora</span>
            <strong className="olx-hub-strip__value">{activeCount}</strong>
          </div>
          <div className="olx-hub-strip__card">
            <span className="olx-hub-strip__label">Registros filtrados</span>
            <strong className="olx-hub-strip__value">{totalItems}</strong>
          </div>
        </div>

        <OlxFilterPanel
          filters={activeFilters}
          onClearFilter={(id) => {
            if (id === 'operation') clearOperationFilter();
            if (id === 'operator') clearOperatorFilter();
          }}
          onClearAll={activeFilters.length > 1 ? clearAllFilters : undefined}
        >
          <OlxPickerField
            label="Operação"
            value={operationLabel}
            placeholder="Todas as operações"
            onPick={() => setOperationPickerOpen(true)}
            onClear={operationLabel ? clearOperationFilter : undefined}
          />
          <OlxPickerField
            label="Operador OLX"
            value={operatorLabel ? `@${operatorLabel}` : null}
            placeholder="Todos os operadores"
            onPick={() => setOperatorPickerOpen(true)}
            onClear={operatorLabel ? clearOperatorFilter : undefined}
          />
          <button type="button" className="btn btn-secondary btn-sm olx-filter-panel__apply" onClick={handleSearch}>
            Aplicar filtros
          </button>
        </OlxFilterPanel>

        {rows.length === 0 ? (
          <EmptyState
            title="Nenhum registro encontrado"
            message="Ajuste os filtros ou aguarde operadores OLX iniciarem patch de anúncios."
          />
        ) : (
          <div className="olx-ad-list">
            {rows.map((row) => (
              <AdPatchListItem
                key={row.id}
                row={row}
                scope="admin"
                operationLabels={operationLabels}
                operatorLabel={row.operatorId ? operatorLabels[row.operatorId] ?? null : null}
                busy={busy}
                onForceUnimpersonate={() => setForceRelease(row)}
              />
            ))}
          </div>
        )}
      </OpsWorkspace>

      <OperationPickerModal
        open={operationPickerOpen}
        title="Filtrar por operação"
        searchOperations={searchAdministratorOperationsPicker}
        onClose={() => setOperationPickerOpen(false)}
        onSelected={(row) => {
          setOperationId(row.id);
          setOperationLabel(row.name);
        }}
      />

      <AccountPickerModal
        open={operatorPickerOpen}
        title="Operador OLX"
        subtitle="Contas com função Operador OLX."
        searchAccounts={searchAdministratorOlxOperatorsPicker}
        onClose={() => setOperatorPickerOpen(false)}
        onSelected={(row) => {
          setOperatorId(row.id);
          setOperatorLabel(row.username);
        }}
      />

      <ConfirmDialog
        open={forceRelease !== null}
        title="Forçar liberação"
        message={
          forceRelease
            ? `Desimpersonar o anúncio #${forceRelease.adId} do operador ${forceRelease.operatorId && operatorLabels[forceRelease.operatorId] ? `@${operatorLabels[forceRelease.operatorId]}` : 'vinculado'}?`
            : undefined
        }
        onCancel={() => setForceRelease(null)}
        onConfirm={() => { if (forceRelease) void handleForceRelease(forceRelease); }}
      />
    </>
  );
}
