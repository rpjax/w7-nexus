import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { createWithdrawalTransfer } from '../../api/transfers';
import type { BankAccountRow, CryptoWalletRow } from '../../api/types';
import { searchEligibleWithdrawalPayments } from '../../features/payments/searchEligibleWithdrawalPayments';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { BankAccountPickerModal } from '../../components/finance/BankAccountPickerModal';
import { CryptoWalletPickerModal } from '../../components/finance/CryptoWalletPickerModal';
import { UnsettledPaymentsPicker } from '../../components/finance/UnsettledPaymentsPicker';
import { IconButton } from '../../components/IconButton';
import { PageHeading } from '../../layouts/PageHeading';
import { DESTINATION_TYPE_OPTIONS, formatMoney, type DestinationType } from '../../utils/financeLabels';
import { bankAccountPickerLabel } from '../../utils/bankAccountDisplay';
import { cryptoWalletPickerLabel } from '../../utils/cryptoWalletDisplay';
import { shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

export function WithdrawalCreatePage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { notifySuccess } = useNotifications();

  const [strawManId, setStrawManId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [destinationType, setDestinationType] = useState<DestinationType>('Pix');
  const [selectedPaymentIds, setSelectedPaymentIds] = useState<Set<string>>(new Set());
  const [paymentsTotal, setPaymentsTotal] = useState(0);
  const [bankAccountId, setBankAccountId] = useState<string | null>(null);
  const [selectedBankAccount, setSelectedBankAccount] = useState<BankAccountRow | null>(null);
  const [cryptoWalletId, setCryptoWalletId] = useState<string | null>(null);
  const [cryptoWalletLabel, setCryptoWalletLabel] = useState<string | null>(null);
  const [pixTransactionId, setPixTransactionId] = useState('');
  const [pixAuthenticationCode, setPixAuthenticationCode] = useState('');
  const [cryptoTransactionId, setCryptoTransactionId] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [paymentsPickerOpen, setPaymentsPickerOpen] = useState(false);
  const [bankPickerOpen, setBankPickerOpen] = useState(false);
  const [cryptoPickerOpen, setCryptoPickerOpen] = useState(false);

  const netAmount = useMemo(() => paymentsTotal, [paymentsTotal]);

  useEffect(() => {
    const state = location.state as { bankAccount?: BankAccountRow } | null;
    if (!state?.bankAccount?.id) return;
    setBankAccountId(state.bankAccount.id);
    setSelectedBankAccount(state.bankAccount);
    navigate(location.pathname, { replace: true, state: null });
  }, [location.pathname, location.state, navigate]);

  useEffect(() => {
    if (selectedPaymentIds.size === 0) {
      setPaymentsTotal(0);
      return;
    }
    if (!strawManId.trim()) {
      setPaymentsTotal(0);
      return;
    }
    void (async () => {
      const result = await searchEligibleWithdrawalPayments({
        limit: 500,
        offset: 0,
        keyword: null,
        strawManId,
      });
      if (!result.ok) return;
      const total = (result.data?.items ?? [])
        .filter((p) => selectedPaymentIds.has(p.id))
        .reduce((sum, p) => sum + p.amount, 0);
      setPaymentsTotal(total);
    })();
  }, [selectedPaymentIds, strawManId]);

  function resetDestination() {
    setBankAccountId(null);
    setSelectedBankAccount(null);
    setCryptoWalletId(null);
    setCryptoWalletLabel(null);
  }

  function goToCreateBankAccount() {
    navigate('/dashboard/transfers/bank-accounts', {
      state: {
        strawManId,
        strawLabel,
        openCreate: true,
        returnTo: '/dashboard/transfers/new',
      },
    });
  }

  async function handleSubmit() {
    setError('');
    if (!strawManId.trim()) {
      setError('Selecione o laranja.');
      return;
    }
    if (selectedPaymentIds.size === 0) {
      setError('Selecione ao menos um pagamento elegível.');
      return;
    }
    if (destinationType === 'Pix' && !bankAccountId) {
      setError('Selecione a conta bancária de destino.');
      return;
    }
    if (destinationType === 'Crypto' && !cryptoWalletId) {
      setError('Selecione a carteira crypto de destino.');
      return;
    }

    setBusy(true);
    try {
      const result = await createWithdrawalTransfer({
        destinationBankAccountId: destinationType === 'Pix' ? bankAccountId : null,
        destinationCryptoWalletId: destinationType === 'Crypto' ? cryptoWalletId : null,
        paymentIds: [...selectedPaymentIds],
        proof: {
          pixTransactionId: destinationType === 'Pix' ? pixTransactionId.trim() || null : null,
          pixAuthenticationCode: destinationType === 'Pix' ? pixAuthenticationCode.trim() || null : null,
          cryptoTransactionId: destinationType === 'Crypto' ? cryptoTransactionId.trim() || null : null,
        },
      });
      if (!result.ok) {
        setError(result.error);
        return;
      }
      notifySuccess('Transferência de saque registrada com sucesso.');
      if (result.data?.id) {
        navigate(`/dashboard/transfers/${result.data.id}`);
        return;
      }
      navigate('/dashboard/transfers');
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeading
        kicker="Financeiro"
        title="Novo saque"
        subtitle="Selecione o laranja, vincule pagamentos pagos e não sacados e escolha o destino PIX ou crypto."
        backLink={{ to: '/dashboard/transfers', label: 'Lista de transferências' }}
      />

      <section className="card ops-card">
        <div className="card-title-row">
          <h2>Dados do saque</h2>
          <span className="post-badge">POST /api/administrator/transfers/withdrawal</span>
        </div>

        <div className="form-grid form-grid-wide">
          <div className="field span-2">
            <label>Laranja</label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setStrawPickerOpen(true)}>
                {strawLabel ?? 'Selecionar laranja'}
              </button>
              <button type="button" className="btn-icon btn-icon-warm" onClick={() => setStrawPickerOpen(true)}>＋</button>
              {strawManId ? (
                <IconButton icon="x" label="Limpar laranja" onClick={() => {
                  setStrawManId('');
                  setStrawLabel(null);
                  resetDestination();
                  setSelectedPaymentIds(new Set());
                }} />
              ) : null}
            </div>
          </div>

          <div className="field">
            <label htmlFor="destinationType">Destino</label>
            <select
              id="destinationType"
              className="nexus-input"
              value={destinationType}
              onChange={(e) => {
                setDestinationType(e.target.value as DestinationType);
                resetDestination();
              }}
            >
              {DESTINATION_TYPE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>

          <div className="field">
            <label>Pagamentos</label>
            <div className="account-select-row">
              <button
                type="button"
                className="account-select-trigger"
                disabled={!strawManId}
                onClick={() => setPaymentsPickerOpen(true)}
              >
                {selectedPaymentIds.size > 0
                  ? `${selectedPaymentIds.size} pagamento(s) · ${formatMoney(paymentsTotal)}`
                  : 'Selecionar pagamentos elegíveis'}
              </button>
              <button
                type="button"
                className="btn-icon btn-icon-green"
                disabled={!strawManId}
                onClick={() => setPaymentsPickerOpen(true)}
              >
                ＋
              </button>
            </div>
          </div>

          {destinationType === 'Pix' ? (
            <div className="field span-2">
              <label>Conta bancária</label>
              <div className="account-select-row">
                <button
                  type="button"
                  className="account-select-trigger account-select-trigger--stacked"
                  disabled={!strawManId}
                  onClick={() => setBankPickerOpen(true)}
                >
                  {selectedBankAccount ? (
                    <span>{bankAccountPickerLabel(selectedBankAccount)}</span>
                  ) : (
                    'Selecionar conta bancária do laranja'
                  )}
                </button>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  disabled={!strawManId}
                  onClick={goToCreateBankAccount}
                >
                  Cadastrar
                </button>
              </div>
            </div>
          ) : (
            <div className="field span-2">
              <label>Carteira crypto</label>
              <div className="account-select-row">
                <button
                  type="button"
                  className="account-select-trigger"
                  disabled={!strawManId}
                  onClick={() => setCryptoPickerOpen(true)}
                >
                  {cryptoWalletLabel ?? 'Selecionar carteira (laranja ou outra)'}
                </button>
                <Link className="btn btn-ghost btn-sm" to="/dashboard/transfers/crypto-wallets">Cadastrar</Link>
              </div>
            </div>
          )}

          <div className="field">
            <label>Valor total</label>
            <input className="nexus-input" readOnly value={formatMoney(netAmount)} />
          </div>

          {destinationType === 'Pix' ? (
            <>
              <div className="field span-2">
                <label htmlFor="pixTx">ID transação PIX <span className="muted small">opcional</span></label>
                <input id="pixTx" className="nexus-input" value={pixTransactionId} onChange={(e) => setPixTransactionId(e.target.value)} />
              </div>
              <div className="field span-2">
                <label htmlFor="pixAuth">Código autenticação PIX <span className="muted small">opcional</span></label>
                <input id="pixAuth" className="nexus-input" value={pixAuthenticationCode} onChange={(e) => setPixAuthenticationCode(e.target.value)} />
              </div>
            </>
          ) : (
            <div className="field span-2">
              <label htmlFor="cryptoTx">ID transação on-chain <span className="muted small">opcional</span></label>
              <input id="cryptoTx" className="nexus-input" value={cryptoTransactionId} onChange={(e) => setCryptoTransactionId(e.target.value)} />
            </div>
          )}
        </div>

        <div className="card-actions">
          <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void handleSubmit()}>
            {busy ? 'Registrando…' : 'Registrar saque'}
          </button>
        </div>
      </section>

      {error ? (
        <section className="feedback-block error">
          <h3>Não foi possível registrar o saque</h3>
          <p>{error}</p>
        </section>
      ) : null}

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        subtitle="Titular dos pagamentos que serão liquidados neste saque."
        onSelected={(row) => {
          setStrawManId(row.id);
          setStrawLabel(`${row.username} (${shortId(row.id)})`);
          resetDestination();
          setSelectedPaymentIds(new Set());
        }}
      />

      <UnsettledPaymentsPicker
        open={paymentsPickerOpen}
        onClose={() => setPaymentsPickerOpen(false)}
        ownerId={strawManId}
        selectedIds={selectedPaymentIds}
        onChange={setSelectedPaymentIds}
      />

      <BankAccountPickerModal
        open={bankPickerOpen}
        onClose={() => setBankPickerOpen(false)}
        ownerId={strawManId}
        onCreateRequested={goToCreateBankAccount}
        onSelected={(row: BankAccountRow) => {
          setBankAccountId(row.id);
          setSelectedBankAccount(row);
        }}
      />

      <CryptoWalletPickerModal
        open={cryptoPickerOpen}
        onClose={() => setCryptoPickerOpen(false)}
        ownerId={strawManId}
        allowAnyStrawMan
        onSelected={(row: CryptoWalletRow) => {
          setCryptoWalletId(row.id);
          setCryptoWalletLabel(cryptoWalletPickerLabel(row));
        }}
      />
    </>
  );
}
