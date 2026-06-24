import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getTransferTimeline } from '../../api/transfers';
import type { ActiveBalanceRow } from '../../api/types';
import { MovementComposerModal } from '../../components/finance/MovementComposerModal';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

function balanceFromSearchParams(params: URLSearchParams): ActiveBalanceRow | null {
  const balanceId = params.get('sourceBalanceId');
  if (!balanceId) return null;

  const amount = Number(params.get('sourceAmount') ?? '0');
  const bankId = params.get('sourceBankAccountId');
  const cryptoId = params.get('sourceCryptoWalletId');

  return {
    balanceId,
    transferId: params.get('from') ?? '',
    amount: Number.isFinite(amount) ? amount : 0,
    currency: bankId ? 'BRL' : 'CRYPTO',
    account: {
      kind: bankId ? 'BankAccount' : 'CryptoWallet',
      id: bankId ?? cryptoId,
      displayName: bankId ? 'Conta bancária de origem' : 'Carteira crypto de origem',
    },
    canMove: true,
    canPayout: Boolean(bankId),
  };
}

export function MovementCreatePage() {
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
          setBalances((result.data?.activeBalances ?? []).filter((balance) => balance.canMove));
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
        title="Nova movimentação"
        subtitle="Selecione o saldo, o destino e confirme. Sem digitar IDs manualmente."
        backLink={{ to: fromTransferId ? `/dashboard/transfers/${fromTransferId}` : '/dashboard/transfers', label: 'Voltar' }}
      />

      {loading ? (
        <p className="muted">Carregando saldos disponíveis…</p>
      ) : balances.length === 0 ? (
        <section className="card ops-card">
          <p className="muted">Nenhum saldo disponível para movimentar.</p>
          <p><Link className="btn btn-primary" to="/dashboard/transfers">Ir para transferências</Link></p>
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
