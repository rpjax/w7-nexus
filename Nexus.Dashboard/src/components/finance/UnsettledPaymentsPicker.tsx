import { useEffect, useId, useMemo, useState } from 'react';
import type { PaymentRow } from '../../api/types';
import { searchEligibleWithdrawalPayments } from '../../features/payments/searchEligibleWithdrawalPayments';
import { formatMoney, paymentStatusLabel } from '../../utils/financeLabels';
import { formatUtc, shortId } from '../../utils/format';
import { IconButton } from '../IconButton';
import { PaginationBar } from '../ListControls';

type UnsettledPaymentsPickerProps = {
  open: boolean;
  onClose: () => void;
  strawManId: string;
  selectedIds: Set<string>;
  onChange: (ids: Set<string>) => void;
};

const FETCH_LIMIT = 300;
const PAGE_SIZE = 8;

export function UnsettledPaymentsPicker({
  open,
  onClose,
  strawManId,
  selectedIds,
  onChange,
}: UnsettledPaymentsPickerProps) {
  const searchInputId = useId();
  const [keyword, setKeyword] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [allRows, setAllRows] = useState<PaymentRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');

  useEffect(() => {
    if (!open) return;
    setKeyword('');
    setCurrentPage(1);
  }, [open, strawManId]);

  useEffect(() => {
    if (!open || !strawManId.trim()) return;
    void loadEligible();
  }, [open, strawManId]);

  async function loadEligible() {
    setLoading(true);
    setLoadError('');
    try {
      const result = await searchEligibleWithdrawalPayments({
        limit: FETCH_LIMIT,
        offset: 0,
        keyword: null,
        strawManId,
      });
      if (!result.ok) {
        setAllRows([]);
        setLoadError(result.error);
        return;
      }
      setAllRows(result.data?.items ?? []);
    } finally {
      setLoading(false);
    }
  }

  const filteredRows = useMemo(() => {
    const term = keyword.trim().toLowerCase();
    if (!term) return allRows;
    return allRows.filter(
      (r) =>
        r.id.toLowerCase().includes(term)
        || r.gatewayTransactionId.toLowerCase().includes(term),
    );
  }, [allRows, keyword]);

  const totalPages = filteredRows.length === 0 ? 1 : Math.ceil(filteredRows.length / PAGE_SIZE);
  const pageRows = filteredRows.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const selectedTotal = useMemo(
    () => allRows.filter((r) => selectedIds.has(r.id)).reduce((sum, r) => sum + r.amount, 0),
    [allRows, selectedIds],
  );

  function toggle(id: string) {
    const next = new Set(selectedIds);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    onChange(next);
  }

  if (!open) return null;

  return (
    <div className="dialog-backdrop dialog-backdrop--picker" onClick={onClose}>
      <div className="dialog-card account-picker finance-picker" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <header className="account-picker-header">
          <div className="account-picker-heading">
            <h3 className="account-picker-title">Pagamentos elegíveis</h3>
            <p className="account-picker-sub">Pagos, não sacados e vinculados ao laranja selecionado.</p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        <div className="account-picker-search-row">
          <input
            id={searchInputId}
            className="nexus-input account-picker-search"
            value={keyword}
            onChange={(e) => { setKeyword(e.target.value); setCurrentPage(1); }}
            placeholder="Buscar por ID ou transação gateway…"
          />
        </div>

        {loadError ? <p className="feedback error">{loadError}</p> : null}
        {loading ? <p className="muted account-picker-hint">Carregando pagamentos…</p> : null}

        {!loading && filteredRows.length === 0 ? (
          <p className="muted account-picker-hint">Nenhum pagamento elegível encontrado.</p>
        ) : (
          <div className="table-wrap finance-picker-table">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th />
                  <th>ID</th>
                  <th>Valor</th>
                  <th>Status</th>
                  <th>Tx gateway</th>
                  <th>Criado em</th>
                </tr>
              </thead>
              <tbody>
                {pageRows.map((row) => (
                  <tr key={row.id}>
                    <td data-label="Selecionar">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(row.id)}
                        onChange={() => toggle(row.id)}
                        aria-label={`Selecionar pagamento ${row.id}`}
                      />
                    </td>
                    <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                    <td data-label="Valor">{formatMoney(row.amount)}</td>
                    <td data-label="Status">{paymentStatusLabel(row.status)}</td>
                    <td data-label="Tx gateway"><span className="mono">{shortId(row.gatewayTransactionId, 18)}</span></td>
                    <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <footer className="account-picker-footer account-picker-footer--split">
          <span className="account-picker-count">
            {selectedIds.size} selecionado(s)
            {selectedIds.size > 0 ? ` · ${formatMoney(selectedTotal)}` : ''}
          </span>
          <div className="account-picker-footer-actions">
            <PaginationBar
              currentPage={currentPage}
              totalPages={totalPages}
              onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
              onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={loading}
            />
            <button type="button" className="btn btn-primary btn-sm" onClick={onClose}>Confirmar</button>
          </div>
        </footer>
      </div>
    </div>
  );
}
