import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { searchStrawManPayments } from '@/api/payments/strawMan';
import type { PaymentRow } from '@/api/types';
import { useAuth } from '@/auth/AuthContext';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { createPaymentColumns } from '@/features/payments/payment-columns';
import { detailPath } from '@/features/payments/paymentPaths';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';

export function StrawManPaymentsPage() {
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
  } = usePaginatedQuery<PaymentRow>({
    queryKey: ['straw-man-payments'],
    fetchPage: async (params) => adaptSearchResponse(await searchStrawManPayments({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    })),
  });

  const columns = useMemo(
    () => createPaymentColumns('straw-man', { highlightAccountId: user?.accountId }),
    [user?.accountId],
  );

  return (
    <ListPageLayout
      kicker="Laranjas"
      title="Meus pagamentos"
      description="Cobranças vinculadas à sua conta laranja — somente leitura."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Configurações', href: '/dashboard/straw-man/settings' },
        { label: 'Meus pagamentos' },
      ]}
      searchId="strawman-payment-search"
      searchLabel="Buscar"
      searchPlaceholder="ID, operação ou transação gateway…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={submitSearch}
      onRefresh={() => void refetch()}
      totalLabel={`${totalItems} registro(s)`}
      isLoading={isLoading}
      error={error}
      isEmpty={!isLoading && !error && items.length === 0}
      emptyTitle="Nenhum pagamento encontrado"
      emptyMessage="Não há pagamentos vinculados ao seu laranja ou o filtro não retornou resultados."
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
        onRowClick={(row) => navigate(detailPath('straw-man', row.id))}
      />
    </ListPageLayout>
  );
}
