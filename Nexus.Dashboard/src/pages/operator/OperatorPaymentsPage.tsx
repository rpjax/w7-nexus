import { useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { searchOperatorPayments } from '@/api/payments/operator';
import { useAuth } from '@/auth/AuthContext';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { createPaymentColumns } from '@/features/payments/payment-columns';
import { detailPath } from '@/features/payments/paymentPaths';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { Button } from '@/components/ui/button';

export function OperatorPaymentsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
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
    queryKey: ['operator-payments'],
    fetchPage: async (params) => adaptSearchResponse(await searchOperatorPayments({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    })),
  });

  const columns = useMemo(
    () => createPaymentColumns('operator', { highlightAccountId: user?.accountId }),
    [user?.accountId],
  );

  return (
    <ListPageLayout
      kicker="Financeiro"
      title="Meus pagamentos"
      description="Pagamentos vinculados à sua conta, repasses e equipes onde você está alocado."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Meus pagamentos' },
      ]}
      searchId="operator-payment-search"
      searchLabel="Buscar"
      searchPlaceholder="ID, operação ou transação gateway…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={submitSearch}
      onRefresh={() => void refetch()}
      totalLabel={`${totalItems} registro(s)`}
      createAction={(
        <Button size="sm" asChild>
          <Link to="/dashboard/payments/pix">Gerar PIX</Link>
        </Button>
      )}
      isLoading={isLoading}
      error={error}
      isEmpty={!isLoading && !error && items.length === 0}
      emptyTitle="Nenhum pagamento encontrado"
      emptyMessage="Você ainda não participa de pagamentos visíveis ou o filtro não retornou resultados."
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
