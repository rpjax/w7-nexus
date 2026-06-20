import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getWithdrawal } from '../../api/withdrawals';
import type { WithdrawalRow } from '../../api/types';
import { StatusPill } from '../../components/finance/StatusPill';
import { EmptyState } from '../../components/EmptyState';
import { PageHeading } from '../../layouts/PageHeading';
import { formatMoney, withdrawalTypeLabel } from '../../utils/financeLabels';
import { formatUtc, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

export function WithdrawalDetailPage() {
  const { withdrawalId = '' } = useParams();
  const { notifyError } = useNotifications();
  const [row, setRow] = useState<WithdrawalRow | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!withdrawalId) return;
    void (async () => {
      setLoading(true);
      const result = await getWithdrawal(withdrawalId);
      if (!result.ok) {
        notifyError(result.error);
        setRow(null);
      } else {
        setRow(result.data);
      }
      setLoading(false);
    })();
  }, [withdrawalId, notifyError]);

  if (loading) {
    return <p className="muted">Carregando saque…</p>;
  }

  if (!row) {
    return (
      <>
        <EmptyState
          title="Saque não encontrado"
          message="Verifique o identificador ou volte à lista."
        />
        <p><Link className="btn btn-primary" to="/dashboard/withdrawals">Voltar à lista</Link></p>
      </>
    );
  }

  return (
    <>
      <PageHeading
        kicker="Financeiro"
        title="Detalhe do saque"
        subtitle={<span className="mono">{row.id}</span>}
        backLink={{ to: '/dashboard/withdrawals', label: 'Lista de saques' }}
      />

      <section className="card ops-card">
        <div className="detail-grid">
          <div>
            <p className="detail-label">Tipo</p>
            <StatusPill label={withdrawalTypeLabel(row.type)} tone={row.type === 'Pix' ? 'info' : 'warn'} />
          </div>
          <div>
            <p className="detail-label">Operação</p>
            <p className="mono">{row.operationId}</p>
          </div>
          <div>
            <p className="detail-label">Laranja</p>
            <p className="mono">{row.strawManAccountId}</p>
          </div>
          <div>
            <p className="detail-label">Total pagamentos</p>
            <p>{formatMoney(row.paymentsTotalAmount)}</p>
          </div>
          <div>
            <p className="detail-label">Custo</p>
            <p>{formatMoney(row.costAmount)}</p>
          </div>
          <div>
            <p className="detail-label">Líquido</p>
            <p><strong>{formatMoney(row.netAmount)}</strong></p>
          </div>
          <div>
            <p className="detail-label">Criado em</p>
            <p className="muted">{formatUtc(row.createdAt)}</p>
          </div>
          {row.costDescription ? (
            <div className="span-2">
              <p className="detail-label">Descrição do custo</p>
              <p>{row.costDescription}</p>
            </div>
          ) : null}
        </div>
      </section>

      <section className="card ops-card">
        <h2 className="section-title">Destino</h2>
        {row.type === 'Pix' ? (
          <p>Conta bancária: <span className="mono">{row.bankAccountId ?? '—'}</span></p>
        ) : (
          <p>Carteira crypto: <span className="mono">{row.cryptoWalletId ?? '—'}</span></p>
        )}
      </section>

      <section className="card ops-card">
        <h2 className="section-title">Comprovante</h2>
        {row.type === 'Pix' && row.pixProof ? (
          <div className="detail-grid">
            <div>
              <p className="detail-label">ID transação</p>
              <p className="mono">{row.pixProof.transactionId ?? '—'}</p>
            </div>
            <div>
              <p className="detail-label">Código autenticação</p>
              <p className="mono">{row.pixProof.authenticationCode ?? '—'}</p>
            </div>
          </div>
        ) : null}
        {row.type === 'Crypto' && row.cryptoProof ? (
          <div>
            <p className="detail-label">ID transação on-chain</p>
            <p className="mono">{row.cryptoProof.transactionId ?? '—'}</p>
          </div>
        ) : null}
        {!row.pixProof && !row.cryptoProof ? <p className="muted">Nenhum comprovante registrado.</p> : null}
      </section>

      <section className="card ops-card">
        <h2 className="section-title">Pagamentos vinculados ({row.paymentIds.length})</h2>
        <ul className="mono-list">
          {row.paymentIds.map((id) => (
            <li key={id}>{shortId(id, 36)}</li>
          ))}
        </ul>
      </section>
    </>
  );
}
