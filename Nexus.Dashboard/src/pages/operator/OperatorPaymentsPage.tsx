import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchOperatorPayments } from '../../api/operator/payments';
import type { PaymentRow } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { EmptyState } from '../../components/EmptyState';
import { PaginationBar } from '../../components/ListControls';
import { PageHeading } from '../../layouts/PageHeading';
import { PaymentListItem } from '../../features/payments/PaymentListItem';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function OperatorPaymentsPage() {
  const { user } = useAuth();
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [rows, setRows] = useState<PaymentRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchOperatorPayments({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
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
    void load(currentPage, query);
  }, [currentPage, query, load]);

  return (
    <div className="ops-page">
      <PageHeading
        kicker="Financeiro"
        title="Meus pagamentos"
        subtitle="Pagamentos vinculados à sua conta, repasses e equipes onde você está alocado."
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
          className="payment-search-form payment-search-form--compact"
          onSubmit={(event) => {
            event.preventDefault();
            setCurrentPage(1);
            setQuery(search);
          }}
        >
          <label className="field payment-search-form__keyword">
            <span className="field-label">Buscar</span>
            <input
              className="field-input"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="ID, operação ou transação gateway…"
            />
          </label>
          <div className="payment-search-form__actions">
            <button type="submit" className="btn btn-primary btn-small">Buscar</button>
            <button type="button" className="btn btn-ghost btn-small" onClick={() => void load(currentPage, query)}>
              Atualizar
            </button>
          </div>
        </form>
      </section>

      {rows.length === 0 ? (
        <EmptyState
          title="Nenhum pagamento encontrado"
          message="Você ainda não participa de pagamentos visíveis ou o filtro não retornou resultados."
        />
      ) : (
        <div className="ops-list payment-list">
          {rows.map((row) => (
            <PaymentListItem
              key={row.id}
              payment={row}
              scope="operator"
              highlightAccountId={user?.accountId}
            />
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
