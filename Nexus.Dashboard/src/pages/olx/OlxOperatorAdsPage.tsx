import { useCallback, useMemo, useState } from 'react';
import {
  impersonateOlxAd,
  searchOlxOperatorAdPatches,
  unimpersonateOlxAd,
  updateOlxAdPatch,
} from '@/api/olx/operator';
import { searchAdministratorOperationsPicker } from '@/api/operationPickerSources';
import type { OlxOperatorAdPatchRow } from '@/api/olx/types';
import { useAuth } from '@/auth/AuthContext';
import { isAdministrator } from '@/auth/roles';
import { OperationPickerDialog } from '@/components/data/entity-picker-dialog';
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
import { ImpersonateAdModal } from '@/components/olx/ImpersonateAdModal';
import { OlxFilterPanel, OlxHubStrip, OlxPickerField } from '@/components/olx/OlxFilterPanel';
import { UpdatePatchModal } from '@/components/olx/UpdatePatchModal';
import { createAdPatchColumns } from '@/features/olx/ad-patch-columns';
import { useOlxOperationLabels } from '@/features/olx/useOlxOperationLabels';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { useNotifications } from '@/notifications/NotificationContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export function OlxOperatorAdsPage() {
  const { user } = useAuth();
  const adminView = isAdministrator(user);
  const { notifyError, notifySuccess } = useNotifications();
  const [operationId, setOperationId] = useState('');
  const [operationLabel, setOperationLabel] = useState<string | null>(null);
  const [appliedOperationIds, setAppliedOperationIds] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [impersonateOpen, setImpersonateOpen] = useState(false);
  const [impersonateSeed, setImpersonateSeed] = useState<{ operationId?: string; adId?: string; adUrl?: string }>({});
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [editRow, setEditRow] = useState<OlxOperatorAdPatchRow | null>(null);
  const [confirmRelease, setConfirmRelease] = useState<OlxOperatorAdPatchRow | null>(null);

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
  } = usePaginatedQuery<OlxOperatorAdPatchRow>({
    queryKey: ['olx-operator-ads', appliedOperationIds],
    fetchPage: async (params) => adaptSearchResponse(await searchOlxOperatorAdPatches({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
      operationIds: appliedOperationIds,
    })),
  });

  const extraOperationLabels = useMemo(
    () => (operationLabel && operationId ? { [operationId]: operationLabel } : {}),
    [operationId, operationLabel],
  );
  const operationLabels = useOlxOperationLabels(items, extraOperationLabels);

  const activeCount = useMemo(
    () => items.filter((row) => row.isImpersonating).length,
    [items],
  );

  const columns = useMemo(
    () => createAdPatchColumns('operator', {
      operationLabels,
      currentAccountId: user?.accountId,
      busy,
      onImpersonate: (row) => {
        setImpersonateSeed({
          operationId: row.operationId,
          adId: row.adId,
          adUrl: row.adUrl,
        });
        setImpersonateOpen(true);
      },
      onEditPrices: (row) => setEditRow(row as OlxOperatorAdPatchRow),
      onUnimpersonate: (row) => setConfirmRelease(row as OlxOperatorAdPatchRow),
    }),
    [operationLabels, user?.accountId, busy],
  );

  function handleSearch() {
    setAppliedOperationIds(operationId.trim() ? [operationId.trim()] : []);
    submitSearch();
  }

  function clearOperationFilter() {
    setOperationId('');
    setOperationLabel(null);
    setAppliedOperationIds([]);
    submitSearch();
  }

  const activeFilters = appliedOperationIds.length > 0 && operationLabel
    ? [{ id: 'operation', label: `Operação: ${operationLabel}` }]
    : appliedOperationIds.length > 0
      ? [{ id: 'operation', label: `Operação: ${appliedOperationIds[0]}` }]
      : [];

  const runMutation = useCallback(async (task: () => Promise<{ ok: boolean; error?: string }>, successMessage: string) => {
    setBusy(true);
    const result = await task();
    setBusy(false);
    if (!result.ok) {
      notifyError(result.error ?? 'Operação falhou.');
      return false;
    }
    notifySuccess(successMessage);
    await refetch();
    return true;
  }, [notifyError, notifySuccess, refetch]);

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

  async function handleRelease(row: OlxOperatorAdPatchRow) {
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
      () => updateOlxAdPatch({
        operationId: editRow.operationId,
        adId: editRow.adId,
        originalPrice,
        promotionalPrice,
      }),
      'Preços atualizados.',
    );
    if (ok) setEditRow(null);
  }

  function openImpersonate(row?: OlxOperatorAdPatchRow) {
    setImpersonateSeed({
      operationId: row?.operationId,
      adId: row?.adId,
      adUrl: row?.adUrl,
    });
    setImpersonateOpen(true);
  }

  return (
    <>
      <ListPageLayout
        kicker="OLX"
        title="Meus anúncios"
        description="Assuma anúncios livres, defina preços patchados e libere slots quando terminar."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Meus anúncios' },
        ]}
        searchId="olx-op-search"
        searchLabel="Buscar"
        searchPlaceholder="ID do anúncio ou operação…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={handleSearch}
        onRefresh={() => void refetch()}
        totalLabel={`${totalItems} anúncio(s) · ${activeCount} em controle`}
        createAction={(
          <Button type="button" onClick={() => openImpersonate()}>
            Assumir anúncio
          </Button>
        )}
        isLoading={isLoading}
        error={error}
        isEmpty={!isLoading && !error && items.length === 0}
        emptyTitle="Nenhum anúncio patchado"
        emptyMessage="Assuma um anúncio livre para começar. Apenas registros sob seu controle aparecem aqui."
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
            items={[
              { label: 'Em controle', value: activeCount },
              { label: 'Total visível', value: totalItems },
            ]}
          />

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
              <div className="grid gap-2">
                <Label htmlFor="olx-op-operation-filter">Operação</Label>
                <Input
                  id="olx-op-operation-filter"
                  value={operationId}
                  onChange={(e) => setOperationId(e.target.value)}
                  placeholder="Filtrar por ID da operação"
                  onKeyDown={(e) => { if (e.key === 'Enter') handleSearch(); }}
                />
              </div>
            )}
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

      <UpdatePatchModal
        open={editRow !== null}
        busy={busy}
        row={editRow}
        onClose={() => setEditRow(null)}
        onSubmit={handleUpdatePrices}
      />

      <AlertDialog open={confirmRelease !== null} onOpenChange={(open) => { if (!open) setConfirmRelease(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Liberar anúncio</AlertDialogTitle>
            <AlertDialogDescription>
              Encerrar a impersonação deste anúncio? Outro operador OLX poderá assumi-lo.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={() => { if (confirmRelease) void handleRelease(confirmRelease); }}>
              Confirmar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
