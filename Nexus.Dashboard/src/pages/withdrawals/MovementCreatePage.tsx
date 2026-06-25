import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getTransferTimeline } from '../../api/transfers';
import type { ActiveBalanceRow } from '../../api/types';
import { MovementComposerModal } from '../../components/finance/MovementComposerModal';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

export function MovementCreatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { notifyError } = useNotifications();
  const fromTransferId = searchParams.get('from') ?? searchParams.get('fromTransferId');
  const [strawManId, setStrawManId] = useState('');
  const [strawManUsername, setStrawManUsername] = useState<string | null>(null);
  const [balances, setBalances] = useState<ActiveBalanceRow[]>([]);
  const [loading, setLoading] = useState(Boolean(fromTransferId));

  useEffect(() => {
    if (!fromTransferId) {
      setBalances([]);
      setLoading(false);
      return;
    }

    void (async () => {
      setLoading(true);
      const result = await getTransferTimeline(fromTransferId);
      if (!result.ok) {
        notifyError(result.error);
        setBalances([]);
      } else {
        setStrawManId(result.data?.strawMan?.id ?? '');
        setStrawManUsername(result.data?.strawMan?.username ?? null);
        setBalances((result.data?.activeBalances ?? []).filter((balance) => balance.canMove));
      }
      setLoading(false);
    })();
  }, [fromTransferId, notifyError]);

  const initialBalanceId = searchParams.get('sourceBalanceId');

  return (
    <div className="page-stack">
      <PageHeading
        kicker="Financeiro"
        title="Nova movimentação"
        subtitle="Transfira saldos disponíveis na cadeia de uma transferência."
        backLink={{ to: fromTransferId ? `/dashboard/transfers/${fromTransferId}` : '/dashboard/transfers', label: 'Voltar' }}
      />

      {loading ? (
        <p className="muted">Carregando saldos disponíveis…</p>
      ) : !fromTransferId ? (
        <section className="card ops-card finance-guide-card">
          <h2 className="finance-guide-card__title">Abra uma transferência primeiro</h2>
          <p className="muted">
            Movimentações partem dos saldos ativos na cadeia. Na lista, abra um saque ou movimentação e use
            <strong> Nova movimentação</strong> no detalhe.
          </p>
          <Link className="btn btn-primary" to="/dashboard/transfers">Ir para transferências</Link>
        </section>
      ) : balances.length === 0 ? (
        <section className="card ops-card finance-guide-card">
          <h2 className="finance-guide-card__title">Sem saldo para movimentar</h2>
          <p className="muted">Nesta cadeia não há saldos disponíveis — o valor já foi movimentado ou repassado.</p>
          <Link className="btn btn-primary" to={`/dashboard/transfers/${fromTransferId}`}>Voltar ao detalhe</Link>
        </section>
      ) : (
        <MovementComposerModal
          open
          variant="embedded"
          onClose={() => navigate(fromTransferId ? `/dashboard/transfers/${fromTransferId}` : '/dashboard/transfers')}
          strawManId={strawManId}
          strawManUsername={strawManUsername}
          activeBalances={balances}
          initialBalanceId={initialBalanceId}
          onSuccess={(transferId) => navigate(`/dashboard/transfers/${transferId}`)}
        />
      )}
    </div>
  );
}
