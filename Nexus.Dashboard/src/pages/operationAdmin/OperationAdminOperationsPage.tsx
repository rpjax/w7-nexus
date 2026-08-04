import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { searchOperationAdministratorOperations } from '@/api/operations/operationAdministrator/operations';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { createOperationColumns } from '@/features/operations/operation-columns';
import { detailPath } from '@/features/operations/operationPaths';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';

export function OperationAdminOperationsPage() {
  const navigate = useNavigate();
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
  } = usePaginatedQuery({
    queryKey: ['operation-admin-operations'],
    fetchPage: async (params) => adaptSearchResponse(await searchOperationAdministratorOperations({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    })),
  });

  const columns = useMemo(() => createOperationColumns('operation-admin'), []);

  return (
    <ListPageLayout
      kicker="Operação"
      title="Administração de operações"
      description="Operações em que você é administrador: crie equipes, defina líderes e configure laranjas e credenciais de gateway. Operadores e repasses ficam com cada líder."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Administração de operações' },
      ]}
      searchId="opAdminSearch"
      searchLabel="Buscar nas suas operações"
      searchPlaceholder="Nome, ID ou descrição…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={submitSearch}
      onRefresh={() => void refetch()}
      totalLabel={`${totalItems} registro(s)`}
      isLoading={isLoading}
      error={error}
      isEmpty={!isLoading && !error && items.length === 0}
      emptyTitle="Nenhuma operação encontrada"
      emptyMessage="Você ainda não administra nenhuma operação ou o filtro não retornou resultados."
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
        onRowClick={(row) => navigate(detailPath('operation-admin', row.id))}
      />
    </ListPageLayout>
  );
}
