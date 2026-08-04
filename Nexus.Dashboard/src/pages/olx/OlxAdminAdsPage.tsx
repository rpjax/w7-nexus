import { useMemo, useState } from 'react';
import { searchAdministratorOlxOperatorsPicker } from '@/api/accountPickerSources';
import { searchAdministratorOperationsPicker } from '@/api/operationPickerSources';
import { adminUnimpersonateOlxAd, searchOlxAdminAdPatches } from '@/api/olx/admin';
import type { OlxAdminAdPatchRow } from '@/api/olx/types';
import { AccountPickerDialog, OperationPickerDialog } from '@/components/data/entity-picker-dialog';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { OlxFilterPanel, OlxHubStrip, OlxPickerField } from '@/components/olx/OlxFilterPanel';
import { createAdPatchColumns } from '@/features/olx/ad-patch-columns';
import { useOlxOperationLabels, useOlxOperatorLabels } from '@/features/olx/useOlxOperationLabels';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { useNotifications } from '@/notifications/NotificationContext';
import { Button } from '@/components/ui/button';

export function OlxAdminAdsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [operationId, setOperationId] = useState('');
  const [operationLabel, setOperationLabel] = useState<string | null>(null);
  const [operatorId, setOperatorId] = useState('');
  const [operatorLabel, setOperatorLabel] = useState<string | null>(null);
  const [appliedOperationIds, setAppliedOperationIds] = useState<string[]>([]);
  const [appliedOperatorIds, setAppliedOperatorIds] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [forceRelease, setForceRelease] = useState<OlxAdminAdPatchRow | null>(null);
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);

  const {
    search,
    setSearch,
    currentPage,
    totalItems,
    totalPages,
    items,
    isLoading,
    error,
    refetch,
    submitSearch,
    goPrev,
    goNext,
  } = usePaginatedQuery<OlxAdminAdPatchRow>({
    queryKey: ['olx-admin-ads', appliedOperationIds, appliedOperatorIds],
    fetchPage: async (params) => adaptSearchResponse(await searchOlxAdminAdPatches({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
      operationIds: appliedOperationIds,
      operatorIds: appliedOperatorIds,
    })),
  });

  const extraOperationLabels = useMemo(
    () => (operationLabel && operationId ? { [operationId]: operationLabel } : {}),
    [operationId, operationLabel],
  );
  const extraOperatorLabels = useMemo(
    () => (operatorLabel && operatorId ? { [operatorId]: operatorLabel } : {}),
    [operatorId, operatorLabel],
  );
  const operationLabels = useOlxOperationLabels(items, extraOperationLabels);
  const operatorLabels = useOlxOperatorLabels(items, extraOperatorLabels);

  const activeCount = useMemo(
    () => items.filter((row) => row.isImpersonating).length,
    [items],
  );

  const columns = useMemo(
    () => createAdPatchColumns('admin', {
      operationLabels,
      operatorLabels,
      busy,
      onForceUnimpersonate: (row) => setForceRelease(row as OlxAdminAdPatchRow),
    }),
    [operationLabels, operatorLabels, busy],
  );

  function handleSearch() {
    setAppliedOperationIds(operationId.trim() ? [operationId.trim()] : []);
    setAppliedOperatorIds(operatorId.trim() ? [operatorId.trim()] : []);
    submitSearch();
  }

  function clearOperationFilter() {
    setOperationId('');
    setOperationLabel(null);
    setAppliedOperationIds([]);
    submitSearch();
  }

  function clearOperatorFilter() {
    setOperatorId('');
    setOperatorLabel(null);
    setAppliedOperatorIds([]);
    submitSearch();
  }

  function clearAllFilters() {
    setOperationId('');
    setOperationLabel(null);
    setOperatorId('');
    setOperatorLabel(null);
    setAppliedOperationIds([]);
    setAppliedOperatorIds([]);
    submitSearch();
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
    await refetch();
  }

  return (
    <>
      <ListPageLayout
        className="admin-surface"
        kicker="Administração"
        kickerVariant="admin"
        title="Gestão OLX"
        description="Visão global dos patches de anúncios. Filtre por operação ou operador OLX e force liberações quando necessário."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Gestão OLX' },
        ]}
        searchId="olx-admin-search"
        searchLabel="Buscar"
        searchPlaceholder="Anúncio, operação ou operador…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={handleSearch}
        onRefresh={() => void refetch()}
        totalLabel={`${totalItems} registro(s) · ${activeCount} impersonando`}
        isLoading={isLoading}
        error={error}
        isEmpty={!isLoading && !error && items.length === 0}
        emptyTitle="Nenhum registro encontrado"
        emptyMessage="Ajuste os filtros ou aguarde operadores OLX iniciarem patch de anúncios."
        footer={totalItems > 0 ? (
          <ListPagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={goPrev}
            onNext={goNext}
          />
        ) : undefined}
      >
        <div className="mb-6 space-y-4">
          <OlxHubStrip
            variant="admin"
            items={[
              { label: 'Impersonando agora', value: activeCount },
              { label: 'Registros filtrados', value: totalItems },
            ]}
          />

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
            <Button type="button" variant="secondary" size="sm" onClick={handleSearch}>
              Aplicar filtros
            </Button>
          </OlxFilterPanel>
        </div>

        <DataTable columns={columns} data={items} getRowId={(row) => row.id} />
      </ListPageLayout>

      <OperationPickerDialog
        open={operationPickerOpen}
        title="Filtrar por operação"
        searchOperations={searchAdministratorOperationsPicker}
        onClose={() => setOperationPickerOpen(false)}
        onSelected={(row) => {
          setOperationId(row.id);
          setOperationLabel(row.name);
        }}
      />

      <AccountPickerDialog
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

      <AlertDialog open={forceRelease !== null} onOpenChange={(open) => { if (!open) setForceRelease(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Forçar liberação</AlertDialogTitle>
            <AlertDialogDescription>
              {forceRelease
                ? `Desimpersonar o anúncio #${forceRelease.adId} do operador ${forceRelease.operatorId && operatorLabels[forceRelease.operatorId] ? `@${operatorLabels[forceRelease.operatorId]}` : 'vinculado'}?`
                : undefined}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={() => { if (forceRelease) void handleForceRelease(forceRelease); }}>
              Confirmar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
