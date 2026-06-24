import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { createPayoutTransfer } from '../../api/transfers';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

export function PayoutCreatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { notifyError, notifySuccess } = useNotifications();
  const [strawManId, setStrawManId] = useState('');
  const [sourceBankAccountId, setSourceBankAccountId] = useState('');
  const [sourceBalanceId, setSourceBalanceId] = useState('');
  const [sourceAmount, setSourceAmount] = useState('');
  const [participantAccountId, setParticipantAccountId] = useState('');
  const [pixTransactionId, setPixTransactionId] = useState('');
  const [pixAuthenticationCode, setPixAuthenticationCode] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setStrawManId(searchParams.get('strawManId') ?? '');
    setSourceBankAccountId(searchParams.get('sourceBankAccountId') ?? '');
    setSourceBalanceId(searchParams.get('sourceBalanceId') ?? '');
    setSourceAmount(searchParams.get('sourceAmount') ?? '');
    setParticipantAccountId(searchParams.get('participantAccountId') ?? '');
  }, [searchParams]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const amount = Number(sourceAmount.replace(',', '.'));
    if (!strawManId.trim() || !sourceBankAccountId.trim() || !sourceBalanceId.trim() || !participantAccountId.trim() || !(amount > 0)) {
      notifyError('Preencha todos os campos obrigatórios.');
      return;
    }
    if (!pixTransactionId.trim() && !pixAuthenticationCode.trim()) {
      notifyError('O comprovante PIX é obrigatório no repasse.');
      return;
    }

    setBusy(true);
    try {
      const result = await createPayoutTransfer({
        strawManId: strawManId.trim(),
        sourceBankAccountId: sourceBankAccountId.trim(),
        sourceBalanceId: sourceBalanceId.trim(),
        sourceAmount: amount,
        participantAccountId: participantAccountId.trim(),
        pixTransactionId: pixTransactionId.trim() || null,
        pixAuthenticationCode: pixAuthenticationCode.trim() || null,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Repasse registrado.');
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
        title="Novo repasse"
        subtitle={prefilled
          ? 'Dados pré-preenchidos a partir da linha do tempo. Informe o participante e o comprovante PIX.'
          : 'Debita o saldo de origem com comprovante obrigatório. O valor não é creditado no destino.'}
        backLink={{ to: '/dashboard/transfers', label: 'Lista de transferências' }}
      />
      <form className="card form-card" onSubmit={(e) => void handleSubmit(e)}>
        <div className="field">
          <label htmlFor="strawManId">ID do laranja</label>
          <input id="strawManId" className="nexus-input" value={strawManId} onChange={(e) => setStrawManId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="sourceBankAccountId">Conta bancária origem</label>
          <input id="sourceBankAccountId" className="nexus-input" value={sourceBankAccountId} onChange={(e) => setSourceBankAccountId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="sourceBalanceId">ID do saldo origem</label>
          <input id="sourceBalanceId" className="nexus-input mono" value={sourceBalanceId} onChange={(e) => setSourceBalanceId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="sourceAmount">Valor (BRL)</label>
          <input id="sourceAmount" className="nexus-input" value={sourceAmount} onChange={(e) => setSourceAmount(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="participantAccountId">Conta do participante (split)</label>
          <input id="participantAccountId" className="nexus-input" value={participantAccountId} onChange={(e) => setParticipantAccountId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="pixTransactionId">ID transação PIX</label>
          <input id="pixTransactionId" className="nexus-input" value={pixTransactionId} onChange={(e) => setPixTransactionId(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="pixAuthenticationCode">Código autenticação PIX</label>
          <input id="pixAuthenticationCode" className="nexus-input" value={pixAuthenticationCode} onChange={(e) => setPixAuthenticationCode(e.target.value)} />
        </div>
        <div className="form-actions">
          <Link className="btn btn-ghost" to="/dashboard/transfers">Cancelar</Link>
          <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Salvando…' : 'Registrar repasse'}</button>
        </div>
      </form>
    </div>
  );
}
