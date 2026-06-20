import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchPayments } from '../api/payments';
import type { PaymentRow } from '../api/types';
import { OpsWorkspace } from '../components/admin/OpsWorkspace';
import { EmptyState } from '../components/EmptyState';
import { StatusPill } from '../components/finance/StatusPill';
import { PaginationBar } from '../components/ListControls';
import {
  formatMoney,
  paymentStatusLabel,
  paymentStatusTone,
  settlementStatusLabel,
  settlementStatusTone,
} from '../utils/financeLabels';
import { formatUtc, shortId, shortTx } from '../utils/format';
import { useNotifications } from '../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function PaymentsListPage() {
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [rows, setRows] = useState<PaymentRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchPayments({
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

  async function refresh() {
    await load(currentPage, query);
  }

  function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  return (
    <OpsWorkspace
      kicker="Financeiro"
      title="Pagamentos"
      lead="Registros de cobrança no repositório — status de pagamento e liquidação (saque)."
      searchId="paySearch"
      searchLabel="Buscar pagamentos"
      searchPlaceholder="ID, operação, transação gateway, operador ou laranja…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={handleSearch}
      onRefresh={() => void refresh()}
      totalItems={totalItems}
      totalLabel={`${totalItems} registro(s)`}
      footer={totalItems > 0 ? (
        <PaginationBar
          currentPage={currentPage}
          totalPages={totalPages}
          onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
          onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
        />
      ) : undefined}
    >
      <div className="ops-workspace__actions-row">
        <Link className="btn btn-primary" to="/dashboard/payments/pix">Gerar PIX</Link>
      </div>

      {rows.length === 0 ? (
        <EmptyState
          title="Nenhum pagamento encontrado"
          message="Ajuste a busca ou gere uma cobrança em Gerar PIX."
        />
      ) : (
        <div className="table-wrap table-top-gap">
          <table className="responsive-data ops-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Operação</th>
                <th>Gateway</th>
                <th>Tx gateway</th>
                <th>Valor</th>
                <th>Status</th>
                <th>Liquidação</th>
                <th>Operador</th>
                <th>Laranja</th>
                <th>Criado em</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}>
                  <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                  <td data-label="Operação"><span className="mono">{shortId(row.operationId)}</span></td>
                  <td data-label="Gateway">{row.gateway}</td>
                  <td data-label="Tx gateway">
                    <span className="mono token-mask" title={row.gatewayTransactionId}>{shortTx(row.gatewayTransactionId)}</span>
                  </td>
                  <td data-label="Valor">{formatMoney(row.amount)}</td>
                  <td data-label="Status">
                    <StatusPill label={paymentStatusLabel(row.status)} tone={paymentStatusTone(row.status)} />
                  </td>
                  <td data-label="Liquidação">
                    <StatusPill
                      label={settlementStatusLabel(row.settlementStatus)}
                      tone={settlementStatusTone(row.settlementStatus)}
                    />
                  </td>
                  <td data-label="Operador" className="muted">{row.operatorAccountId ? shortId(row.operatorAccountId) : '—'}</td>
                  <td data-label="Laranja" className="muted">{row.strawManAccountId ? shortId(row.strawManAccountId) : '—'}</td>
                  <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </OpsWorkspace>
  );
}
