import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { searchOpAdminStrawMenPicker } from '../../api/accountPickerSources';
import { searchAdministratorOperationsPicker } from '../../api/operationPickerSources';
import { searchPayments } from '../../api/payments';
import { createWithdrawal } from '../../api/withdrawals';
import type { BankAccountRow, CryptoWalletRow, WithdrawalType } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { BankAccountPickerModal } from '../../components/finance/BankAccountPickerModal';
import { CryptoWalletPickerModal } from '../../components/finance/CryptoWalletPickerModal';
import { UnsettledPaymentsPicker } from '../../components/finance/UnsettledPaymentsPicker';
import { IconButton } from '../../components/IconButton';
import { OperationPickerModal } from '../../components/OperationPickerModal';
import { PageHeading } from '../../layouts/PageHeading';
import { WITHDRAWAL_TYPE_OPTIONS, formatMoney } from '../../utils/financeLabels';
import { bankAccountPickerLabel, bankAccountPixSummary } from '../../utils/bankAccountDisplay';
import { shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

export function WithdrawalCreatePage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { notifySuccess } = useNotifications();

  const [operationId, setOperationId] = useState('');
  const [operationLabel, setOperationLabel] = useState<string | null>(null);
  const [strawManAccountId, setStrawManAccountId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [withdrawalType, setWithdrawalType] = useState<WithdrawalType>('Pix');
  const [selectedPaymentIds, setSelectedPaymentIds] = useState<Set<string>>(new Set());
  const [paymentsTotal, setPaymentsTotal] = useState(0);
  const [bankAccountId, setBankAccountId] = useState<string | null>(null);
  const [selectedBankAccount, setSelectedBankAccount] = useState<BankAccountRow | null>(null);
  const [cryptoWalletId, setCryptoWalletId] = useState<string | null>(null);
  const [cryptoWalletLabel, setCryptoWalletLabel] = useState<string | null>(null);
  const [costDescription, setCostDescription] = useState('');
  const [costAmount, setCostAmount] = useState(0);
  const [pixTransactionId, setPixTransactionId] = useState('');
  const [pixAuthenticationCode, setPixAuthenticationCode] = useState('');
  const [cryptoTransactionId, setCryptoTransactionId] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [paymentsPickerOpen, setPaymentsPickerOpen] = useState(false);
  const [bankPickerOpen, setBankPickerOpen] = useState(false);
  const [cryptoPickerOpen, setCryptoPickerOpen] = useState(false);

  const netAmount = useMemo(() => Math.max(0, paymentsTotal - costAmount), [paymentsTotal, costAmount]);

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
    void (async () => {
      const result = await searchPayments({ limit: 500, offset: 0, keyword: null });
      if (!result.ok) return;
      const total = (result.data?.items ?? [])
        .filter((p) => selectedPaymentIds.has(p.id))
        .reduce((sum, p) => sum + p.amount, 0);
      setPaymentsTotal(total);
    })();
  }, [selectedPaymentIds]);

  function resetDestination() {
    setBankAccountId(null);
    setSelectedBankAccount(null);
    setCryptoWalletId(null);
    setCryptoWalletLabel(null);
  }

  function goToCreateBankAccount() {
    navigate('/dashboard/withdrawals/bank-accounts', {
      state: {
        strawManAccountId,
        strawLabel,
        openCreate: true,
        returnTo: '/dashboard/withdrawals/new',
      },
    });
  }

  async function handleSubmit() {
    setError('');
    if (!operationId.trim()) {
      setError('Selecione uma operação.');
      return;
    }
    if (!strawManAccountId.trim()) {
      setError('Selecione o laranja.');
      return;
    }
    if (selectedPaymentIds.size === 0) {
      setError('Selecione ao menos um pagamento elegível.');
      return;
    }
    if (withdrawalType === 'Pix' && !bankAccountId) {
      setError('Selecione a conta bancária de destino.');
      return;
    }
    if (withdrawalType === 'Crypto' && !cryptoWalletId) {
      setError('Selecione a carteira crypto de destino.');
      return;
    }

    setBusy(true);
    try {
      const result = await createWithdrawal({
        operationId: operationId.trim(),
        type: withdrawalType,
        strawManAccountId: strawManAccountId.trim(),
        bankAccountId: withdrawalType === 'Pix' ? bankAccountId : null,
        cryptoWalletId: withdrawalType === 'Crypto' ? cryptoWalletId : null,
        paymentIds: [...selectedPaymentIds],
        costDescription: costDescription.trim() || null,
        costAmount,
        pixTransactionId: withdrawalType === 'Pix' ? pixTransactionId.trim() || null : null,
        pixAuthenticationCode: withdrawalType === 'Pix' ? pixAuthenticationCode.trim() || null : null,
        cryptoTransactionId: withdrawalType === 'Crypto' ? cryptoTransactionId.trim() || null : null,
      });
      if (!result.ok) {
        setError(result.error);
        return;
      }
      notifySuccess('Saque registrado com sucesso.');
      if (result.data?.id) {
        navigate(`/dashboard/withdrawals/${result.data.id}`);
        return;
      }
      navigate('/dashboard/withdrawals');
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeading
        kicker="Financeiro"
        title="Novo saque"
        subtitle="Vincule pagamentos pagos e não sacados a uma conta bancária ou carteira crypto do laranja."
        backLink={{ to: '/dashboard/withdrawals', label: 'Lista de saques' }}
      />

      <section className="card ops-card">
        <div className="card-title-row">
          <h2>Dados do saque</h2>
          <span className="post-badge">POST /api/withdrawals</span>
        </div>

        <div className="form-grid form-grid-wide">
          <div className="field span-2">
            <label>Operação</label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setOperationPickerOpen(true)}>
                {operationLabel ?? 'Selecionar operação'}
              </button>
              <button type="button" className="btn-icon btn-icon-green" onClick={() => setOperationPickerOpen(true)} title="Selecionar operação">＋</button>
              {operationId ? (
                <IconButton icon="x" label="Limpar operação" onClick={() => { setOperationId(''); setOperationLabel(null); setSelectedPaymentIds(new Set()); }} />
              ) : null}
            </div>
          </div>

          <div className="field span-2">
            <label>Laranja</label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setStrawPickerOpen(true)}>
                {strawLabel ?? 'Selecionar laranja'}
              </button>
              <button type="button" className="btn-icon btn-icon-warm" onClick={() => setStrawPickerOpen(true)}>＋</button>
              {strawManAccountId ? (
                <IconButton icon="x" label="Limpar laranja" onClick={() => {
                  setStrawManAccountId('');
                  setStrawLabel(null);
                  resetDestination();
                  setSelectedPaymentIds(new Set());
                }} />
              ) : null}
            </div>
          </div>

          <div className="field">
            <label htmlFor="withdrawalType">Tipo de saque</label>
            <select
              id="withdrawalType"
              className="nexus-input"
              value={withdrawalType}
              onChange={(e) => {
                setWithdrawalType(e.target.value as WithdrawalType);
                resetDestination();
              }}
            >
              {WITHDRAWAL_TYPE_OPTIONS.map((opt) => (
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
                disabled={!operationId || !strawManAccountId}
                onClick={() => setPaymentsPickerOpen(true)}
              >
                {selectedPaymentIds.size > 0
                  ? `${selectedPaymentIds.size} pagamento(s) · ${formatMoney(paymentsTotal)}`
                  : 'Selecionar pagamentos elegíveis'}
              </button>
              <button
                type="button"
                className="btn-icon btn-icon-green"
                disabled={!operationId || !strawManAccountId}
                onClick={() => setPaymentsPickerOpen(true)}
              >
                ＋
              </button>
            </div>
          </div>

          {withdrawalType === 'Pix' ? (
            <div className="field span-2">
              <label>Conta bancária</label>
              <div className="account-select-row">
                <button
                  type="button"
                  className="account-select-trigger account-select-trigger--stacked"
                  disabled={!strawManAccountId}
                  onClick={() => setBankPickerOpen(true)}
                >
                  {selectedBankAccount ? (
                    <>
                      <span>{bankAccountPickerLabel(selectedBankAccount)}</span>
                      {bankAccountPixSummary(selectedBankAccount) ? (
                        <span className="account-select-trigger__meta muted small">
                          PIX: <span className="mono">{bankAccountPixSummary(selectedBankAccount)}</span>
                        </span>
                      ) : null}
                    </>
                  ) : (
                    'Selecionar conta bancária'
                  )}
                </button>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  disabled={!strawManAccountId}
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
                  disabled={!strawManAccountId}
                  onClick={() => setCryptoPickerOpen(true)}
                >
                  {cryptoWalletLabel ?? 'Selecionar carteira'}
                </button>
                <Link className="btn btn-ghost btn-sm" to="/dashboard/withdrawals/crypto-wallets">Cadastrar</Link>
              </div>
            </div>
          )}

          <div className="field span-2">
            <label htmlFor="costDescription">Descrição do custo <span className="muted small">opcional</span></label>
            <input id="costDescription" className="nexus-input" value={costDescription} onChange={(e) => setCostDescription(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="costAmount">Valor do custo</label>
            <input id="costAmount" type="number" min={0} step="0.01" className="nexus-input" value={costAmount} onChange={(e) => setCostAmount(Number(e.target.value))} />
          </div>
          <div className="field">
            <label>Valor líquido</label>
            <input className="nexus-input" readOnly value={formatMoney(netAmount)} />
          </div>

          {withdrawalType === 'Pix' ? (
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

      <OperationPickerModal
        open={operationPickerOpen}
        onClose={() => setOperationPickerOpen(false)}
        searchOperations={searchAdministratorOperationsPicker}
        title="Selecionar operação"
        subtitle="Todas as operações do sistema."
        onSelected={(row) => {
          setOperationId(row.id);
          setOperationLabel(`${row.name} (${shortId(row.id)})`);
          setSelectedPaymentIds(new Set());
        }}
      />

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        searchAccounts={searchOpAdminStrawMenPicker}
        title="Conta laranja"
        subtitle="Laranja vinculado à operação para liquidação."
        onSelected={(row) => {
          setStrawManAccountId(row.id);
          setStrawLabel(`${row.username} (${shortId(row.id)})`);
          resetDestination();
          setSelectedPaymentIds(new Set());
        }}
      />

      <UnsettledPaymentsPicker
        open={paymentsPickerOpen}
        onClose={() => setPaymentsPickerOpen(false)}
        operationId={operationId}
        strawManAccountId={strawManAccountId}
        selectedIds={selectedPaymentIds}
        onChange={setSelectedPaymentIds}
      />

      <BankAccountPickerModal
        open={bankPickerOpen}
        onClose={() => setBankPickerOpen(false)}
        strawManAccountId={strawManAccountId}
        onCreateRequested={goToCreateBankAccount}
        onSelected={(row: BankAccountRow) => {
          setBankAccountId(row.id);
          setSelectedBankAccount(row);
        }}
      />

      <CryptoWalletPickerModal
        open={cryptoPickerOpen}
        onClose={() => setCryptoPickerOpen(false)}
        strawManAccountId={strawManAccountId}
        onSelected={(row: CryptoWalletRow) => {
          setCryptoWalletId(row.id);
          setCryptoWalletLabel(`${row.asset} · ${row.chain} · ${shortId(row.address, 16)}`);
        }}
      />
    </>
  );
}
