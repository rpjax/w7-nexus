import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { searchOperatorOperations } from '@/api/operations/operator';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { dedupeOperatorListItems } from '@/features/operations/fetchOperationById';
import { createOperationColumns } from '@/features/operations/operation-columns';
import { detailPath } from '@/features/operations/operationPaths';
import { usePaginatedQuery } from '@/hooks/use-paginated-query';

export function OperatorOperationsPage() {
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
    queryKey: ['operator-operations'],
    fetchPage: async ({ limit, offset, keyword }) => {
      const result = await searchOperatorOperations({ limit, offset, keyword });
      if (!result.ok) return result;
      return {
        ok: true as const,
        data: {
          items: dedupeOperatorListItems(result.data?.items ?? []),
          total: result.data?.total ?? 0,
        },
      };
    },
  });

  const columns = useMemo(() => createOperationColumns('operator'), []);

  return (
    <ListPageLayout
      kicker="Operação"
      title="Minhas operações"
      description="Operações vinculadas à sua conta via equipes."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Minhas operações' },
      ]}
      searchId="opSearch"
      searchLabel="Buscar nas minhas operações"
      searchPlaceholder="Nome, ID ou descrição…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={submitSearch}
      onRefresh={() => void refetch()}
      totalLabel={`${totalItems} alocação(ões)`}
      isLoading={isLoading}
      error={error}
      isEmpty={!isLoading && !error && items.length === 0}
      emptyTitle="Nenhuma operação encontrada"
      emptyMessage="Você ainda não está alocado em nenhuma operação ou o filtro não retornou resultados."
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
        onRowClick={(row) => navigate(detailPath('operator', row.id))}
      />
    </ListPageLayout>
  );
}
