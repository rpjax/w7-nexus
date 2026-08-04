import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createAdministratorOperation, searchAdministratorOperations } from '@/api/administrator/operations';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { CreateOperationModal } from '@/components/admin/CreateOperationModal';
import { createOperationColumns } from '@/features/operations/operation-columns';
import { detailPath } from '@/features/operations/operationPaths';
import { useOperationScopeActions } from '@/features/operations/useOperationScopeActions';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { useNotifications } from '@/notifications/NotificationContext';
import { Button } from '@/components/ui/button';

export function AdminOperationsPage() {
  const navigate = useNavigate();
  const { notifyError, notifySuccess } = useNotifications();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);

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
    clearSearch,
    goPrev,
    goNext,
  } = usePaginatedQuery({
    queryKey: ['admin-operations'],
    fetchPage: async (params) => adaptSearchResponse(await searchAdministratorOperations({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    })),
  });

  const { requestDeleteOperation, modals } = useOperationScopeActions({
    scope: 'global-admin',
    mode: 'list',
    onMutated: () => void refetch(),
    onOperationDeleted: () => void refetch(),
  });

  const columns = useMemo(
    () => createOperationColumns('global-admin', { onDelete: requestDeleteOperation }),
    [requestDeleteOperation],
  );

  async function handleCreate(name: string, description: string | null) {
    setCreateBusy(true);
    try {
      const result = await createAdministratorOperation(name, description);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Operação registrada no sistema.');
      setCreateModalOpen(false);
      if (result.data?.id) {
        navigate(detailPath('global-admin', result.data.id));
        return;
      }
      clearSearch();
      await refetch();
    } finally {
      setCreateBusy(false);
    }
  }

  return (
    <>
      <ListPageLayout
        className="rounded-lg border border-warning/40"
        kicker="Administração"
        kickerVariant="admin"
        title="Todas as operações"
        description="Gestão completa do repositório: administradores, equipes, operadores, repasses e configuração de gateway."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Todas as operações' },
        ]}
        searchId="adminOpSearch"
        searchLabel="Buscar no sistema"
        searchPlaceholder="Nome, ID ou descrição…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={submitSearch}
        onRefresh={() => void refetch()}
        totalLabel={`${totalItems} registro(s) no repositório`}
        createAction={(
          <Button type="button" onClick={() => setCreateModalOpen(true)}>
            Nova operação
          </Button>
        )}
        isLoading={isLoading}
        error={error}
        isEmpty={!isLoading && !error && items.length === 0}
        emptyTitle="Nenhuma operação encontrada"
        emptyMessage="Registre uma operação ou ajuste o filtro de busca."
        footer={totalItems > 0 ? (
          <ListPagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={goPrev}
            onNext={goNext}
          />
        ) : undefined}
      >
        <DataTable
          columns={columns}
          data={items}
          getRowId={(row) => row.id}
          onRowClick={(row) => navigate(detailPath('global-admin', row.id))}
        />
      </ListPageLayout>

      <CreateOperationModal
        open={createModalOpen}
        busy={createBusy}
        onClose={() => setCreateModalOpen(false)}
        onSubmit={(name, description) => void handleCreate(name, description)}
      />

      {modals}
    </>
  );
}
