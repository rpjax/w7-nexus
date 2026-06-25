import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getTransferTimeline } from '../../api/transfers';
import type { ActiveBalanceRow } from '../../api/types';
import { PayoutComposerModal } from '../../components/finance/PayoutComposerModal';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

export function PayoutCreatePage() {
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
        setBalances(
          (result.data?.activeBalances ?? []).filter(
            (balance) => balance.canPayout && balance.account.kind === 'BankAccount',
          ),
        );
      }
      setLoading(false);
    })();
  }, [fromTransferId, notifyError]);

  const initialBalanceId = searchParams.get('sourceBalanceId');

  return (
    <div className="page-stack">
      <PageHeading
        kicker="Financeiro"
        title="Novo repasse"
        subtitle="Debita saldo bancário com comprovante e registra o destino."
        backLink={{ to: fromTransferId ? `/dashboard/transfers/${fromTransferId}` : '/dashboard/transfers', label: 'Voltar' }}
      />

      {loading ? (
        <p className="muted">Carregando saldos disponíveis…</p>
      ) : !fromTransferId ? (
        <section className="card ops-card finance-guide-card">
          <h2 className="finance-guide-card__title">Abra uma transferência primeiro</h2>
          <p className="muted">
            Repasses partem de saldos BRL ativos na cadeia. No detalhe de uma transferência, use
            <strong> Novo repasse</strong> ou o botão <strong>Repassar</strong> em um saldo.
          </p>
          <Link className="btn btn-primary" to="/dashboard/transfers">Ir para transferências</Link>
        </section>
      ) : balances.length === 0 ? (
        <section className="card ops-card finance-guide-card">
          <h2 className="finance-guide-card__title">Sem saldo para repasse</h2>
          <p className="muted">Não há saldos bancários disponíveis para repasse nesta cadeia.</p>
          <Link className="btn btn-primary" to={`/dashboard/transfers/${fromTransferId}`}>Voltar ao detalhe</Link>
        </section>
      ) : (
        <PayoutComposerModal
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
