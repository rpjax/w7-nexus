import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { searchAdministratorPayments } from '@/api/administrator/payments';
import type { PaymentRow } from '@/api/types';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { createPaymentColumns } from '@/features/payments/payment-columns';
import { detailPath } from '@/features/payments/paymentPaths';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const STATUS_OPTIONS = [
  { value: '', label: 'Todos os status' },
  { value: 'Pending', label: 'Pendente' },
  { value: 'Paid', label: 'Pago' },
  { value: 'Refunded', label: 'Reembolsado' },
  { value: 'Dead', label: 'Cancelado' },
];

const SETTLEMENT_OPTIONS = [
  { value: '', label: 'Toda liquidação' },
  { value: 'Unsettled', label: 'Pendente de saque' },
  { value: 'Withdrawn', label: 'Sacado' },
];

const DISTRIBUTION_OPTIONS = [
  { value: '', label: 'Todo repasse' },
  { value: 'Pending', label: 'Pendente de repasse' },
  { value: 'Complete', label: 'Repassado' },
];

const ALL_VALUE = '__all__';

function toSelectValue(value: string): string {
  return value || ALL_VALUE;
}

function fromSelectValue(value: string): string {
  return value === ALL_VALUE ? '' : value;
}

export function AdminPaymentsPage() {
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState('');
  const [settlementFilter, setSettlementFilter] = useState('');
  const [distributionFilter, setDistributionFilter] = useState('');
  const [appliedStatus, setAppliedStatus] = useState('');
  const [appliedSettlement, setAppliedSettlement] = useState('');
  const [appliedDistribution, setAppliedDistribution] = useState('');

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
    queryKey: ['admin-payments', appliedStatus, appliedSettlement, appliedDistribution],
    fetchPage: async (params) => adaptSearchResponse(await searchAdministratorPayments({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
      status: appliedStatus || null,
      settlementStatus: appliedSettlement || null,
      distributionStatus: appliedDistribution || null,
    })),
  });

  const columns = useMemo(() => createPaymentColumns('global-admin'), []);

  function handleSearch() {
    setAppliedStatus(statusFilter);
    setAppliedSettlement(settlementFilter);
    setAppliedDistribution(distributionFilter);
    submitSearch();
  }

  return (
    <ListPageLayout
      className="rounded-lg border border-warning/40"
      kicker="Administração"
      kickerVariant="admin"
      title="Todos os pagamentos"
      description="Visão global do repositório com filtros e transições de domínio (pagar, reembolsar, cancelar)."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Todos os pagamentos' },
      ]}
      searchId="admin-payment-search"
      searchLabel="Buscar"
      searchPlaceholder="ID, operação, transação gateway, operador ou laranja…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={handleSearch}
      onRefresh={() => void refetch()}
      totalLabel={`${totalItems} registro(s)`}
      createAction={(
        <Button size="sm" asChild>
          <Link to="/dashboard/payments/pix">Gerar PIX</Link>
        </Button>
      )}
      toolbarExtra={(
        <>
          <div className="hidden w-40 space-y-2 lg:block">
            <Label>Status</Label>
            <Select value={toSelectValue(statusFilter)} onValueChange={(value) => setStatusFilter(fromSelectValue(value))}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {STATUS_OPTIONS.map((option) => (
                  <SelectItem key={option.value || 'all'} value={toSelectValue(option.value)}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="hidden w-40 space-y-2 lg:block">
            <Label>Liquidação</Label>
            <Select value={toSelectValue(settlementFilter)} onValueChange={(value) => setSettlementFilter(fromSelectValue(value))}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {SETTLEMENT_OPTIONS.map((option) => (
                  <SelectItem key={option.value || 'all'} value={toSelectValue(option.value)}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="hidden w-40 space-y-2 lg:block">
            <Label>Repasse</Label>
            <Select value={toSelectValue(distributionFilter)} onValueChange={(value) => setDistributionFilter(fromSelectValue(value))}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {DISTRIBUTION_OPTIONS.map((option) => (
                  <SelectItem key={option.value || 'all'} value={toSelectValue(option.value)}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </>
      )}
      isLoading={isLoading}
      error={error}
      isEmpty={!isLoading && !error && items.length === 0}
      emptyTitle="Nenhum pagamento encontrado"
      emptyMessage="Ajuste os filtros ou gere uma cobrança em Gerar PIX."
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
  );
}
