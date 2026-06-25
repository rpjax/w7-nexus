import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getTransferTimeline } from '../../api/transfers';
import type { ActiveBalanceRow } from '../../api/types';
import { PayoutComposerModal } from '../../components/finance/PayoutComposerModal';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

function balanceFromSearchParams(params: URLSearchParams): ActiveBalanceRow | null {
  const balanceId = params.get('sourceBalanceId');
  if (!balanceId) return null;

  const amount = Number(params.get('sourceAmount') ?? '0');
  const bankId = params.get('sourceBankAccountId');

  if (!bankId) return null;

  return {
    balanceId,
    transferId: params.get('from') ?? '',
    amount: Number.isFinite(amount) ? amount : 0,
    currency: 'BRL',
    account: {
      kind: 'BankAccount',
      id: bankId,
      displayName: 'Conta bancária de origem',
    },
    canMove: false,
    canPayout: true,
  };
}

export function PayoutCreatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { notifyError } = useNotifications();
  const fromTransferId = searchParams.get('from') ?? searchParams.get('fromTransferId');
  const [strawManId, setStrawManId] = useState(searchParams.get('strawManId') ?? '');
  const [strawManUsername, setStrawManUsername] = useState<string | null>(null);
  const [balances, setBalances] = useState<ActiveBalanceRow[]>([]);
  const [loading, setLoading] = useState(Boolean(fromTransferId));

  useEffect(() => {
    if (fromTransferId) {
      void (async () => {
        setLoading(true);
        const result = await getTransferTimeline(fromTransferId);
        if (!result.ok) {
          notifyError(result.error);
          setBalances([]);
        } else {
          setStrawManId(result.data?.strawMan?.id ?? searchParams.get('strawManId') ?? '');
          setStrawManUsername(result.data?.strawMan?.username ?? null);
          setBalances(
            (result.data?.activeBalances ?? []).filter(
              (balance) => balance.canPayout && balance.account.kind === 'BankAccount',
            ),
          );
        }
        setLoading(false);
      })();
      return;
    }

    const legacyBalance = balanceFromSearchParams(searchParams);
    setBalances(legacyBalance ? [legacyBalance] : []);
  }, [fromTransferId, notifyError, searchParams]);

  const initialBalanceId = searchParams.get('sourceBalanceId');

  return (
    <div className="page-stack">
      <PageHeading
        kicker="Financeiro"
        title="Novo repasse"
        subtitle="Selecione o saldo bancário, o destino registrado e o comprovante PIX."
        backLink={{ to: fromTransferId ? `/dashboard/transfers/${fromTransferId}` : '/dashboard/transfers', label: 'Voltar' }}
      />

      {loading ? (
        <p className="muted">Carregando saldos disponíveis…</p>
      ) : balances.length === 0 ? (
        <section className="card ops-card">
          <p className="muted">Nenhum saldo bancário disponível para repasse.</p>
          <p><Link className="btn btn-primary" to="/dashboard/transfers">Ir para transferências</Link></p>
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
