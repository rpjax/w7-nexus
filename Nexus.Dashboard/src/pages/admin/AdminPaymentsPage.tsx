import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchAdministratorPayments } from '../../api/administrator/payments';
import type { PaymentRow } from '../../api/types';
import { EmptyState } from '../../components/EmptyState';
import { PaginationBar } from '../../components/ListControls';
import { PageHeading } from '../../layouts/PageHeading';
import { PaymentListItem } from '../../features/payments/PaymentListItem';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

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

export function AdminPaymentsPage() {
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [settlementFilter, setSettlementFilter] = useState('');
  const [appliedStatus, setAppliedStatus] = useState('');
  const [appliedSettlement, setAppliedSettlement] = useState('');
  const [rows, setRows] = useState<PaymentRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (
    page: number,
    keyword: string,
    status: string,
    settlement: string,
  ) => {
    const result = await searchAdministratorPayments({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
      status: status || null,
      settlementStatus: settlement || null,
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
    void load(currentPage, query, appliedStatus, appliedSettlement);
  }, [currentPage, query, appliedStatus, appliedSettlement, load]);

  function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
    setAppliedStatus(statusFilter);
    setAppliedSettlement(settlementFilter);
  }

  return (
    <div className="ops-page">
      <PageHeading
        kicker="Administração"
        kickerVariant="admin"
        title="Todos os pagamentos"
        subtitle="Visão global do repositório com filtros e transições de domínio (pagar, reembolsar, cancelar)."
        backLink={{ to: '/dashboard', label: 'Visão geral' }}
      />

      <section className="ops-page__toolbar bank-managed-section">
        <div className="ops-page__toolbar-head">
          <p className="ops-page__count muted small">{totalItems} registro(s)</p>
          <Link className="btn btn-primary btn-small" to="/dashboard/payments/pix">
            Gerar PIX
          </Link>
        </div>
        <form
          className="payment-search-form"
          onSubmit={(event) => {
            event.preventDefault();
            handleSearch();
          }}
        >
          <label className="field payment-search-form__keyword">
            <span className="field-label">Buscar</span>
            <input
              className="field-input"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="ID, operação, transação gateway, operador ou laranja…"
            />
          </label>
          <label className="field">
            <span className="field-label">Status</span>
            <select className="field-input" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value || 'all'} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Liquidação</span>
            <select className="field-input" value={settlementFilter} onChange={(event) => setSettlementFilter(event.target.value)}>
              {SETTLEMENT_OPTIONS.map((option) => (
                <option key={option.value || 'all'} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>
          <div className="payment-search-form__actions">
            <button type="submit" className="btn btn-primary btn-small">Buscar</button>
            <button
              type="button"
              className="btn btn-ghost btn-small"
              onClick={() => void load(currentPage, query, appliedStatus, appliedSettlement)}
            >
              Atualizar
            </button>
          </div>
        </form>
      </section>

      {rows.length === 0 ? (
        <EmptyState
          title="Nenhum pagamento encontrado"
          message="Ajuste os filtros ou gere uma cobrança em Gerar PIX."
        />
      ) : (
        <div className="ops-list payment-list">
          {rows.map((row) => (
            <PaymentListItem key={row.id} payment={row} scope="global-admin" />
          ))}
        </div>
      )}

      {totalItems > 0 ? (
        <PaginationBar
          currentPage={currentPage}
          totalPages={totalPages}
          onPrev={() => setCurrentPage((page) => Math.max(1, page - 1))}
          onNext={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
        />
      ) : null}
    </div>
  );
}
