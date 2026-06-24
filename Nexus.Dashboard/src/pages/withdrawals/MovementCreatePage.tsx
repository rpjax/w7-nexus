import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { createMovementTransfer } from '../../api/transfers';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

export function MovementCreatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { notifyError, notifySuccess } = useNotifications();
  const [strawManId, setStrawManId] = useState('');
  const [sourceBankAccountId, setSourceBankAccountId] = useState('');
  const [sourceCryptoWalletId, setSourceCryptoWalletId] = useState('');
  const [destinationBankAccountId, setDestinationBankAccountId] = useState('');
  const [sourceBalanceId, setSourceBalanceId] = useState('');
  const [sourceAmount, setSourceAmount] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setStrawManId(searchParams.get('strawManId') ?? '');
    setSourceBankAccountId(searchParams.get('sourceBankAccountId') ?? '');
    setSourceCryptoWalletId(searchParams.get('sourceCryptoWalletId') ?? '');
    setDestinationBankAccountId(searchParams.get('destinationBankAccountId') ?? '');
    setSourceBalanceId(searchParams.get('sourceBalanceId') ?? '');
    setSourceAmount(searchParams.get('sourceAmount') ?? '');
  }, [searchParams]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const amount = Number(sourceAmount.replace(',', '.'));
    const hasBankSource = Boolean(sourceBankAccountId.trim());
    const hasCryptoSource = Boolean(sourceCryptoWalletId.trim());

    if (!strawManId.trim() || !sourceBalanceId.trim() || !(amount > 0)) {
      notifyError('Preencha laranja, saldo e valor.');
      return;
    }

    if (hasBankSource === hasCryptoSource) {
      notifyError('Informe exatamente uma origem: conta bancária ou carteira crypto.');
      return;
    }

    if (!destinationBankAccountId.trim() && !searchParams.get('destinationCryptoWalletId')) {
      notifyError('Informe a conta bancária de destino.');
      return;
    }

    setBusy(true);
    try {
      const result = await createMovementTransfer({
        strawManId: strawManId.trim(),
        sourceBankAccountId: hasBankSource ? sourceBankAccountId.trim() : null,
        sourceCryptoWalletId: hasCryptoSource ? sourceCryptoWalletId.trim() : null,
        destinationBankAccountId: destinationBankAccountId.trim() || null,
        destinationCryptoWalletId: searchParams.get('destinationCryptoWalletId'),
        sourceBalanceId: sourceBalanceId.trim(),
        sourceAmount: amount,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Movimentação registrada.');
      navigate(`/dashboard/transfers/${result.data!.id}`);
    } finally {
      setBusy(false);
    }
  }

  const prefilled = Boolean(searchParams.get('sourceBalanceId'));

  return (
    <div className="page-stack">
      <PageHeading
        kicker="Financeiro"
        title="Nova movimentação"
        subtitle={prefilled
          ? 'Dados pré-preenchidos a partir da linha do tempo. Confira origem, saldo e valor.'
          : 'Transfira saldo parcial entre contas do mesmo laranja.'}
        backLink={{ to: '/dashboard/transfers', label: 'Lista de transferências' }}
      />
      <form className="card form-card" onSubmit={(e) => void handleSubmit(e)}>
        <div className="field">
          <label htmlFor="strawManId">ID do laranja</label>
          <input id="strawManId" className="nexus-input" value={strawManId} onChange={(e) => setStrawManId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="sourceBankAccountId">Conta bancária origem</label>
          <input id="sourceBankAccountId" className="nexus-input" value={sourceBankAccountId} onChange={(e) => setSourceBankAccountId(e.target.value)} disabled={Boolean(sourceCryptoWalletId)} />
        </div>
        <div className="field">
          <label htmlFor="sourceCryptoWalletId">Carteira crypto origem</label>
          <input id="sourceCryptoWalletId" className="nexus-input" value={sourceCryptoWalletId} onChange={(e) => setSourceCryptoWalletId(e.target.value)} disabled={Boolean(sourceBankAccountId)} />
        </div>
        <div className="field">
          <label htmlFor="sourceBalanceId">ID do saldo origem</label>
          <input id="sourceBalanceId" className="nexus-input mono" value={sourceBalanceId} onChange={(e) => setSourceBalanceId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="sourceAmount">Valor</label>
          <input id="sourceAmount" className="nexus-input" value={sourceAmount} onChange={(e) => setSourceAmount(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="destinationBankAccountId">Conta bancária destino</label>
          <input id="destinationBankAccountId" className="nexus-input" value={destinationBankAccountId} onChange={(e) => setDestinationBankAccountId(e.target.value)} />
        </div>
        <div className="form-actions">
          <Link className="btn btn-ghost" to="/dashboard/transfers">Cancelar</Link>
          <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Salvando…' : 'Registrar movimentação'}</button>
        </div>
      </form>
    </div>
  );
}
