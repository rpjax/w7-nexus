import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchPayments } from '../api/payments';
import type { PaymentRow } from '../api/types';
import { EmptyState } from '../components/EmptyState';
import { Feedback } from '../components/Feedback';
import { formatUtc, shortId, shortTx } from '../utils/format';

export function PaymentsListPage() {
  const [search, setSearch] = useState('');
  const [rows, setRows] = useState<PaymentRow[]>([]);
  const [feedback, setFeedback] = useState('');
  const [feedbackIsError, setFeedbackIsError] = useState(false);

  const filteredRows = useMemo(() => {
    if (!search.trim()) return rows;
    const term = search.trim().toLowerCase();
    return rows.filter(
      (r) =>
        r.id.toLowerCase().includes(term)
        || r.operationId.toLowerCase().includes(term)
        || r.gatewayTransactionId.toLowerCase().includes(term)
        || r.gateway.toLowerCase().includes(term)
        || r.status.toLowerCase().includes(term)
        || (r.operatorAccountId?.toLowerCase().includes(term) ?? false)
        || (r.strawManAccountId?.toLowerCase().includes(term) ?? false),
    );
  }, [rows, search]);

  async function refresh() {
    setFeedback('');
    setFeedbackIsError(false);
    const result = await searchPayments({ limit: 500, offset: 0, keyword: null });
    if (!result.ok) {
      setFeedbackIsError(true);
      setFeedback(result.error);
      setRows([]);
      return;
    }
    setRows(result.data?.items ?? []);
  }

  useEffect(() => {
    void refresh();
  }, []);

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <h1>Pagamentos</h1>
          <p className="muted page-lead">Registros do agregado <strong>Payment</strong> no repositório — busca por ID, operação, transação no gateway, operador ou laranja.</p>
        </div>
      </section>

      <Feedback message={feedback} isError={feedbackIsError} />

      <section className="card ops-card">
        <div className="toolbar toolbar-tight toolbar-stack-mobile">
          <div className="field grow">
            <label htmlFor="paySearch">Buscar pagamentos</label>
            <input id="paySearch" className="nexus-input" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="ID, operação, gateway tx, contas…" />
          </div>
          <div className="toolbar-actions">
            <button type="button" className="btn btn-ghost" onClick={() => setSearch(search)}>Buscar</button>
            <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Atualizar</button>
          </div>
        </div>
        <div className="card-title-row">
          <div className="card-title-group">
            <h2 className="section-title">Lista</h2>
            <span className="post-badge">POST /api/payments/search</span>
          </div>
          <Link className="btn btn-primary" to="/dashboard/payments/pix">Gerar PIX</Link>
        </div>

        {filteredRows.length === 0 ? (
          <EmptyState title="Nenhum pagamento encontrado" message="Ajuste a busca ou gere uma cobrança em Gerar PIX." />
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
                  <th>Operador</th>
                  <th>Laranja</th>
                  <th>Criado em</th>
                </tr>
              </thead>
              <tbody>
                {filteredRows.map((row) => (
                  <tr key={row.id}>
                    <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                    <td data-label="Operação"><span className="mono">{shortId(row.operationId)}</span></td>
                    <td data-label="Gateway">{row.gateway}</td>
                    <td data-label="Tx gateway"><span className="mono token-mask" title={row.gatewayTransactionId}>{shortTx(row.gatewayTransactionId)}</span></td>
                    <td data-label="Valor">{row.amount.toFixed(2)}</td>
                    <td data-label="Status">{row.status}</td>
                    <td data-label="Operador" className="muted">{row.operatorAccountId ? shortId(row.operatorAccountId) : '—'}</td>
                    <td data-label="Laranja" className="muted">{row.strawManAccountId ? shortId(row.strawManAccountId) : '—'}</td>
                    <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
