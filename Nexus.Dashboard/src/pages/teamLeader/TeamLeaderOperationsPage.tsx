import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { searchTeamLeaderLedTeams } from '@/api/operations/teamLeader/operations';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { createOperationColumns } from '@/features/operations/operation-columns';
import { detailPath } from '@/features/operations/operationPaths';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';

export function TeamLeaderOperationsPage() {
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
    queryKey: ['team-leader-operations'],
    fetchPage: async (params) => adaptSearchResponse(await searchTeamLeaderLedTeams({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    })),
  });

  const columns = useMemo(() => createOperationColumns('team-leader'), []);

  return (
    <ListPageLayout
      kicker="Liderança"
      title="Liderança de equipes"
      description="Operações agrupadas com as equipes que você lidera. Gerencie operadores e regras de repasse."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Liderança de equipes' },
      ]}
      searchId="teamLeaderSearch"
      searchLabel="Buscar operações"
      searchPlaceholder="Nome, ID ou descrição…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={submitSearch}
      onRefresh={() => void refetch()}
      totalLabel={`${totalItems} operação(ões)`}
      isLoading={isLoading}
      error={error}
      isEmpty={!isLoading && !error && items.length === 0}
      emptyTitle="Nenhuma equipe liderada"
      emptyMessage="Você ainda não lidera nenhuma equipe ou o filtro não retornou resultados."
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
        onRowClick={(row) => navigate(detailPath('team-leader', row.id))}
      />
    </ListPageLayout>
  );
}
