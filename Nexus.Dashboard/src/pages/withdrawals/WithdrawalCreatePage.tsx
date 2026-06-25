import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { createWithdrawalTransfer } from '../../api/transfers';
import type { BankAccountRow, CryptoWalletRow } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { BankAccountPickerModal } from '../../components/finance/BankAccountPickerModal';
import { CryptoWalletPickerModal } from '../../components/finance/CryptoWalletPickerModal';
import { UnsettledPaymentsPicker } from '../../components/finance/UnsettledPaymentsPicker';
import { IconButton } from '../../components/IconButton';
import { PageHeading } from '../../layouts/PageHeading';
import {
  CHAIN_OPTIONS,
  CRYPTO_ASSET_OPTIONS,
  DESTINATION_TYPE_OPTIONS,
  ONRAMPING_METHOD_OPTIONS,
  chainEnumName,
  cryptoAssetEnumName,
  formatMoney,
  type DestinationType,
} from '../../utils/financeLabels';
import { bankAccountPickerLabel } from '../../utils/bankAccountDisplay';
import { cryptoWalletPickerLabel } from '../../utils/cryptoWalletDisplay';
import { parseMovementAmount } from '../../utils/movementDisplay';
import { shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

type LocationState = {
  bankAccount?: BankAccountRow;
  cryptoWallet?: CryptoWalletRow;
};

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
  const [selectedCryptoWallet, setSelectedCryptoWallet] = useState<CryptoWalletRow | null>(null);
  const [onrampingMethod, setOnrampingMethod] = useState('Pix');
  const [producedAmountInput, setProducedAmountInput] = useState('');
  const [producedAsset, setProducedAsset] = useState('Usdt');
  const [producedChain, setProducedChain] = useState(3);
  const [pixTransactionId, setPixTransactionId] = useState('');
  const [pixAuthenticationCode, setPixAuthenticationCode] = useState('');
  const [cryptoTransactionId, setCryptoTransactionId] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [paymentsPickerOpen, setPaymentsPickerOpen] = useState(false);
  const [bankPickerOpen, setBankPickerOpen] = useState(false);
  const [cryptoPickerOpen, setCryptoPickerOpen] = useState(false);

  const producedAmount = parseMovementAmount(producedAmountInput);
  const isCryptoDest = destinationType === 'Crypto';

  const step1Done = Boolean(strawManId.trim() && selectedPaymentIds.size > 0);
  const step2Done = isCryptoDest ? Boolean(cryptoWalletId) : Boolean(bankAccountId);
  const step3Done = isCryptoDest
    ? Boolean(onrampingMethod && producedAmount > 0 && producedAsset && producedChain)
    : true;

  const canSubmit = step1Done && step2Done && step3Done;

  const wizardSteps = useMemo(() => ([
    { id: 1, label: 'Laranja e pagamentos', done: step1Done },
    { id: 2, label: 'Destino', done: step2Done },
    { id: 3, label: isCryptoDest ? 'Conversão crypto' : 'Comprovante', done: step3Done },
  ]), [step1Done, step2Done, step3Done, isCryptoDest]);

  useEffect(() => {
    const state = location.state as LocationState | null;
    if (state?.bankAccount?.id) {
      setBankAccountId(state.bankAccount.id);
      setSelectedBankAccount(state.bankAccount);
      setDestinationType('Pix');
      navigate(location.pathname, { replace: true, state: null });
      return;
    }
    if (state?.cryptoWallet?.id) {
      setCryptoWalletId(state.cryptoWallet.id);
      setSelectedCryptoWallet(state.cryptoWallet);
      setDestinationType('Crypto');
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location.pathname, location.state, navigate]);

  function resetDestination() {
    setBankAccountId(null);
    setSelectedBankAccount(null);
    setCryptoWalletId(null);
    setSelectedCryptoWallet(null);
    setProducedAmountInput('');
  }

  function goToCreateBankAccount() {
    navigate('/dashboard/transfers/bank-accounts', {
      state: { strawManId, strawLabel, openCreate: true, returnTo: '/dashboard/transfers/new' },
    });
  }

  function goToCreateCryptoWallet() {
    navigate('/dashboard/transfers/crypto-wallets', {
      state: { ownerId: strawManId, strawLabel, openCreate: true, returnTo: '/dashboard/transfers/new' },
    });
  }

  async function handleSubmit() {
    setError('');
    if (!canSubmit) {
      setError('Complete todas as etapas antes de registrar o saque.');
      return;
    }

    setBusy(true);
    try {
      const result = await createWithdrawalTransfer({
        destinationBankAccountId: !isCryptoDest ? bankAccountId : null,
        destinationCryptoWalletId: isCryptoDest ? cryptoWalletId : null,
        paymentIds: [...selectedPaymentIds],
        onrampingMethod: isCryptoDest ? onrampingMethod : null,
        producedAmount: isCryptoDest ? producedAmount : null,
        producedAsset: isCryptoDest ? cryptoAssetEnumName(producedAsset) : null,
        producedChain: isCryptoDest ? chainEnumName(producedChain) : null,
        proof: {
          pixTransactionId: !isCryptoDest ? pixTransactionId.trim() || null : null,
          pixAuthenticationCode: !isCryptoDest ? pixAuthenticationCode.trim() || null : null,
          cryptoTransactionId: isCryptoDest ? cryptoTransactionId.trim() || null : null,
        },
      });
      if (!result.ok) {
        setError(result.error);
        return;
      }
      notifySuccess('Saque registrado com sucesso.');
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
    <div className="page-stack withdrawal-create-page">
      <PageHeading
        kicker="Financeiro"
        title="Novo saque"
        subtitle="Liquide pagamentos pagos e escolha onde creditar o valor — PIX ou crypto."
        backLink={{ to: '/dashboard/transfers', label: 'Transferências' }}
      />

      <ol className="withdrawal-wizard__steps" aria-label="Etapas do saque">
        {wizardSteps.map((step) => (
          <li
            key={step.id}
            className={`withdrawal-wizard__step${step.done ? ' is-done' : ''}`}
          >
            <span className="withdrawal-wizard__step-num">{step.id}</span>
            <span className="withdrawal-wizard__step-label">{step.label}</span>
          </li>
        ))}
      </ol>

      <section className="card ops-card withdrawal-wizard">
        <div className="withdrawal-wizard__section">
          <header className="withdrawal-wizard__section-head">
            <h2>1. Laranja e pagamentos</h2>
            <p className="muted small">Pagamentos pagos, não sacados, do titular selecionado.</p>
          </header>
          <div className="form-grid form-grid-wide">
            <div className="field span-2">
              <label>Laranja</label>
              <div className="account-select-row">
                <button type="button" className="account-select-trigger" onClick={() => setStrawPickerOpen(true)}>
                  {strawLabel ?? 'Selecionar laranja'}
                </button>
                {strawManId ? (
                  <IconButton icon="x" label="Limpar laranja" onClick={() => {
                    setStrawManId('');
                    setStrawLabel(null);
                    resetDestination();
                    setSelectedPaymentIds(new Set());
                    setPaymentsTotal(0);
                  }} />
                ) : null}
              </div>
            </div>
            <div className="field span-2">
              <label>Pagamentos</label>
              <button
                type="button"
                className="account-select-trigger account-select-trigger--stacked"
                disabled={!strawManId}
                onClick={() => setPaymentsPickerOpen(true)}
              >
                {selectedPaymentIds.size > 0
                  ? `${selectedPaymentIds.size} pagamento(s) selecionado(s)`
                  : 'Selecionar pagamentos elegíveis'}
              </button>
            </div>
          </div>
        </div>

        <div className="withdrawal-wizard__divider" aria-hidden="true" />

        <div className="withdrawal-wizard__section">
          <header className="withdrawal-wizard__section-head">
            <h2>2. Destino do crédito</h2>
            <p className="muted small">O valor será creditado na conta ou carteira escolhida.</p>
          </header>
          <div className="movement-composer__dest-toggle withdrawal-wizard__dest-toggle">
            {DESTINATION_TYPE_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                type="button"
                className={`btn btn-sm ${destinationType === opt.value ? 'btn-primary' : 'btn-ghost'}`}
                onClick={() => {
                  setDestinationType(opt.value);
                  resetDestination();
                }}
              >
                {opt.label}
              </button>
            ))}
          </div>
          {!isCryptoDest ? (
            <div className="account-select-row withdrawal-wizard__dest-row">
              <button
                type="button"
                className="movement-composer__dest-trigger"
                disabled={!strawManId}
                onClick={() => setBankPickerOpen(true)}
              >
                {selectedBankAccount ? bankAccountPickerLabel(selectedBankAccount) : 'Selecionar conta bancária'}
              </button>
              <button type="button" className="btn btn-ghost btn-sm" disabled={!strawManId} onClick={goToCreateBankAccount}>
                Cadastrar conta
              </button>
            </div>
          ) : (
            <div className="account-select-row withdrawal-wizard__dest-row">
              <button
                type="button"
                className="movement-composer__dest-trigger"
                disabled={!strawManId}
                onClick={() => setCryptoPickerOpen(true)}
              >
                {selectedCryptoWallet ? cryptoWalletPickerLabel(selectedCryptoWallet) : 'Selecionar carteira crypto'}
              </button>
              <button type="button" className="btn btn-ghost btn-sm" disabled={!strawManId} onClick={goToCreateCryptoWallet}>
                Cadastrar carteira
              </button>
            </div>
          )}
        </div>

        <div className="withdrawal-wizard__divider" aria-hidden="true" />

        <div className="withdrawal-wizard__section">
          <header className="withdrawal-wizard__section-head">
            <h2>3. {isCryptoDest ? 'Conversão e comprovante' : 'Comprovante PIX'}</h2>
            <p className="muted small">
              {isCryptoDest
                ? 'Informe como o BRL foi convertido em crypto e, se houver, o hash da transação.'
                : 'Opcional — registre o comprovante do PIX de entrada.'}
            </p>
          </header>
          {isCryptoDest ? (
            <div className="form-grid form-grid-wide">
              <div className="field">
                <label htmlFor="onramping">Onramping</label>
                <select id="onramping" className="nexus-input" value={onrampingMethod} onChange={(e) => setOnrampingMethod(e.target.value)}>
                  {ONRAMPING_METHOD_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label htmlFor="producedAmount">Valor em crypto</label>
                <input
                  id="producedAmount"
                  className="nexus-input"
                  inputMode="decimal"
                  value={producedAmountInput}
                  onChange={(e) => setProducedAmountInput(e.target.value)}
                />
              </div>
              <div className="field">
                <label htmlFor="producedAsset">Ativo</label>
                <select id="producedAsset" className="nexus-input" value={producedAsset} onChange={(e) => setProducedAsset(e.target.value)}>
                  {CRYPTO_ASSET_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label htmlFor="producedChain">Rede</label>
                <select id="producedChain" className="nexus-input" value={producedChain} onChange={(e) => setProducedChain(Number(e.target.value))}>
                  {CHAIN_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="field span-2">
                <label htmlFor="cryptoTx">Hash on-chain <span className="muted small">opcional</span></label>
                <input id="cryptoTx" className="nexus-input mono" value={cryptoTransactionId} onChange={(e) => setCryptoTransactionId(e.target.value)} />
              </div>
            </div>
          ) : (
            <div className="form-grid form-grid-wide">
              <div className="field span-2">
                <label htmlFor="pixTx">ID transação PIX <span className="muted small">opcional</span></label>
                <input id="pixTx" className="nexus-input" value={pixTransactionId} onChange={(e) => setPixTransactionId(e.target.value)} />
              </div>
              <div className="field span-2">
                <label htmlFor="pixAuth">Código autenticação PIX <span className="muted small">opcional</span></label>
                <input id="pixAuth" className="nexus-input" value={pixAuthenticationCode} onChange={(e) => setPixAuthenticationCode(e.target.value)} />
              </div>
            </div>
          )}
        </div>

        <footer className="withdrawal-wizard__footer">
          <div className="withdrawal-wizard__summary">
            <span className="withdrawal-wizard__summary-label">Total do saque</span>
            <strong className="withdrawal-wizard__summary-value">{formatMoney(paymentsTotal)}</strong>
          </div>
          <button
            type="button"
            className="btn btn-primary"
            disabled={busy || !canSubmit}
            onClick={() => void handleSubmit()}
          >
            {busy ? 'Registrando…' : 'Registrar saque'}
          </button>
        </footer>
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
          setStrawLabel(`@${row.username} · ${shortId(row.id, 8)}`);
          resetDestination();
          setSelectedPaymentIds(new Set());
          setPaymentsTotal(0);
        }}
      />

      <UnsettledPaymentsPicker
        open={paymentsPickerOpen}
        onClose={() => setPaymentsPickerOpen(false)}
        ownerId={strawManId}
        selectedIds={selectedPaymentIds}
        onChange={setSelectedPaymentIds}
        onSelectedTotalChange={setPaymentsTotal}
      />

      <BankAccountPickerModal
        open={bankPickerOpen}
        onClose={() => setBankPickerOpen(false)}
        ownerId={strawManId}
        onCreateRequested={goToCreateBankAccount}
        onSelected={(row) => {
          setBankAccountId(row.id);
          setSelectedBankAccount(row);
        }}
      />

      <CryptoWalletPickerModal
        open={cryptoPickerOpen}
        onClose={() => setCryptoPickerOpen(false)}
        ownerId={strawManId}
        allowAnyStrawMan={false}
        onSelected={(row) => {
          setCryptoWalletId(row.id);
          setSelectedCryptoWallet(row);
        }}
      />
    </div>
  );
}
