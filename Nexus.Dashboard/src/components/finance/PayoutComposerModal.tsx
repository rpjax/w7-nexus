import { useCallback, useEffect, useMemo, useState } from 'react';
import { createPayoutTransfer } from '../../api/transfers';
import type { ActiveBalanceRow, BankAccountRow, CryptoWalletRow } from '../../api/types';
import { BankAccountPickerModal } from './BankAccountPickerModal';
import { CryptoWalletPickerModal } from './CryptoWalletPickerModal';
import { IconButton } from '../IconButton';
import { bankAccountPickerLabel } from '../../utils/bankAccountDisplay';
import { cryptoWalletPickerLabel } from '../../utils/cryptoWalletDisplay';
import { DESTINATION_TYPE_OPTIONS } from '../../utils/financeLabels';
import {
  formatActiveBalanceAmount,
  formatActiveBalanceAmountInput,
  formatActiveBalanceSource,
  isMovementAmountWithinLimit,
  parseMovementAmount,
} from '../../utils/movementDisplay';
import { shortId } from '../../utils/format';

type PayoutComposerModalProps = {
  open: boolean;
  onClose: () => void;
  strawManId: string;
  strawManUsername?: string | null;
  activeBalances: ActiveBalanceRow[];
  initialBalanceId?: string | null;
  onSuccess: (transferId: string) => void;
  variant?: 'modal' | 'embedded';
};

export function PayoutComposerModal({
  open,
  onClose,
  strawManId,
  strawManUsername,
  activeBalances,
  initialBalanceId,
  onSuccess,
  variant = 'modal',
}: PayoutComposerModalProps) {
  const payoutBalances = useMemo(
    () => activeBalances.filter((balance) => balance.canPayout && balance.account.kind === 'BankAccount'),
    [activeBalances],
  );

  const [selectedBalanceId, setSelectedBalanceId] = useState<string | null>(null);
  const [amountInput, setAmountInput] = useState('');
  const [destinationType, setDestinationType] = useState<'Pix' | 'Crypto'>('Pix');
  const [destBank, setDestBank] = useState<BankAccountRow | null>(null);
  const [destCrypto, setDestCrypto] = useState<CryptoWalletRow | null>(null);
  const [pixTransactionId, setPixTransactionId] = useState('');
  const [pixAuthenticationCode, setPixAuthenticationCode] = useState('');
  const [cryptoTransactionId, setCryptoTransactionId] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const [bankPickerOpen, setBankPickerOpen] = useState(false);
  const [cryptoPickerOpen, setCryptoPickerOpen] = useState(false);

  const selectedBalance = payoutBalances.find((balance) => balance.balanceId === selectedBalanceId) ?? null;
  const sourceAccountId = selectedBalance?.account.id ?? null;
  const destIsBank = destinationType === 'Pix';
  const destIsCrypto = destinationType === 'Crypto';
  const maxAmount = selectedBalance?.amount ?? 0;
  const amount = parseMovementAmount(amountInput);
  const amountValid = isMovementAmountWithinLimit(amount, maxAmount);
  const hasProof = destIsCrypto
    ? Boolean(cryptoTransactionId.trim() || pixTransactionId.trim() || pixAuthenticationCode.trim())
    : Boolean(pixTransactionId.trim() || pixAuthenticationCode.trim());

  const selectBalance = useCallback((balance: ActiveBalanceRow) => {
    setSelectedBalanceId(balance.balanceId);
    setAmountInput(formatActiveBalanceAmountInput(balance));
    setDestBank(null);
    setDestCrypto(null);
    setError('');
  }, []);

  useEffect(() => {
    if (!open) return;
    setError('');
    setBusy(false);
    setPixTransactionId('');
    setPixAuthenticationCode('');
    setCryptoTransactionId('');
    setDestinationType('Pix');
    const defaultBalance = payoutBalances.find((b) => b.balanceId === initialBalanceId)
      ?? payoutBalances[0]
      ?? null;
    if (defaultBalance) {
      selectBalance(defaultBalance);
    } else {
      setSelectedBalanceId(null);
      setAmountInput('');
    }
  }, [open, initialBalanceId, payoutBalances, selectBalance]);

  const canSubmit = useMemo(() => {
    if (!selectedBalance || !sourceAccountId || !strawManId.trim()) return false;
    if (!amountValid) return false;
    if (!hasProof) return false;
    if (destIsBank && !destBank) return false;
    if (destIsCrypto && !destCrypto) return false;
    return true;
  }, [
    selectedBalance,
    sourceAccountId,
    strawManId,
    amountValid,
    hasProof,
    destIsBank,
    destBank,
    destIsCrypto,
    destCrypto,
  ]);

  async function handleSubmit() {
    if (!selectedBalance || !sourceAccountId || !canSubmit) return;
    setError('');
    setBusy(true);
    try {
      const result = await createPayoutTransfer({
        sourceBalanceId: selectedBalance.balanceId,
        amount,
        destinationBankAccountId: destIsBank ? destBank?.id ?? null : null,
        destinationCryptoWalletId: destIsCrypto ? destCrypto?.id ?? null : null,
        proof: {
          pixTransactionId: pixTransactionId.trim() || null,
          pixAuthenticationCode: pixAuthenticationCode.trim() || null,
          cryptoTransactionId: cryptoTransactionId.trim() || null,
        },
      });
      if (!result.ok) {
        setError(result.error);
        return;
      }
      if (result.data?.id) {
        onSuccess(result.data.id);
      }
    } finally {
      setBusy(false);
    }
  }

  if (!open) return null;

  const composerBody = (
    <>
      <header className="movement-composer__header">
        <div>
          <h2 id="payout-composer-title" className="movement-composer__title">Novo repasse</h2>
          <p className="movement-composer__sub muted small">
            {strawManUsername ? `@${strawManUsername}` : shortId(strawManId, 12)}
            {' · '}debita saldo BRL com comprovante PIX obrigatório
          </p>
        </div>
        {variant === 'modal' ? <IconButton icon="x" label="Fechar" onClick={onClose} /> : null}
      </header>

      {payoutBalances.length === 0 ? (
        <p className="movement-composer__empty muted">
          Não há saldos bancários disponíveis para repasse nesta cadeia.
        </p>
      ) : (
        <div className="movement-composer__body">
          <section className="movement-composer__section">
            <h3 className="movement-composer__section-title">1. Saldo de origem</h3>
            <ul className="movement-composer__balance-list">
              {payoutBalances.map((balance) => {
                const selected = balance.balanceId === selectedBalanceId;
                return (
                  <li key={balance.balanceId}>
                    <button
                      type="button"
                      className={`movement-composer__balance-card${selected ? ' is-selected' : ''}`}
                      onClick={() => selectBalance(balance)}
                    >
                      <strong>{formatActiveBalanceAmount(balance)}</strong>
                      <span className="muted small">{formatActiveBalanceSource(balance)}</span>
                    </button>
                  </li>
                );
              })}
            </ul>
          </section>

          {selectedBalance ? (
            <>
              <section className="movement-composer__section">
                <h3 className="movement-composer__section-title">2. Valor</h3>
                <div className="movement-composer__amount-row">
                  <input
                    className="nexus-input"
                    inputMode="decimal"
                    value={amountInput}
                    onChange={(e) => setAmountInput(e.target.value)}
                    aria-label="Valor do repasse"
                  />
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() => setAmountInput(formatActiveBalanceAmountInput(selectedBalance))}
                  >
                    Usar total
                  </button>
                </div>
              </section>

              <section className="movement-composer__section">
                <h3 className="movement-composer__section-title">3. Destino registrado</h3>
                <p className="muted small movement-composer__hint">
                  O destino é registrado no repasse; o valor não é creditado automaticamente na conta.
                </p>
                <div className="movement-composer__dest-toggle">
                  {DESTINATION_TYPE_OPTIONS.map((opt) => (
                    <button
                      key={opt.value}
                      type="button"
                      className={`btn btn-sm ${destinationType === opt.value ? 'btn-primary' : 'btn-ghost'}`}
                      onClick={() => {
                        setDestinationType(opt.value);
                        setDestBank(null);
                        setDestCrypto(null);
                      }}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
                {destIsBank ? (
                  <button
                    type="button"
                    className="movement-composer__dest-trigger"
                    onClick={() => setBankPickerOpen(true)}
                  >
                    {destBank ? bankAccountPickerLabel(destBank) : 'Selecionar conta bancária'}
                  </button>
                ) : (
                  <button
                    type="button"
                    className="movement-composer__dest-trigger"
                    onClick={() => setCryptoPickerOpen(true)}
                  >
                    {destCrypto ? cryptoWalletPickerLabel(destCrypto) : 'Selecionar carteira crypto'}
                  </button>
                )}
              </section>

              <section className="movement-composer__section movement-composer__section--conversion">
                <h3 className="movement-composer__section-title">4. Comprovante</h3>
                {destIsCrypto ? (
                  <>
                    <p className="muted small movement-composer__hint">
                      Para destino crypto, informe o hash on-chain ou dados do PIX utilizado no repasse.
                    </p>
                    <div className="field">
                      <label htmlFor="payoutCryptoTx">Hash on-chain</label>
                      <input
                        id="payoutCryptoTx"
                        className="nexus-input mono"
                        value={cryptoTransactionId}
                        onChange={(e) => setCryptoTransactionId(e.target.value)}
                      />
                    </div>
                  </>
                ) : (
                  <p className="muted small movement-composer__hint">
                    Comprovante PIX obrigatório para repasses bancários.
                  </p>
                )}
                <div className="form-grid form-grid-wide">
                  <div className="field span-2">
                    <label htmlFor="payoutPixTx">ID transação PIX</label>
                    <input
                      id="payoutPixTx"
                      className="nexus-input"
                      value={pixTransactionId}
                      onChange={(e) => setPixTransactionId(e.target.value)}
                    />
                  </div>
                  <div className="field span-2">
                    <label htmlFor="payoutPixAuth">Código autenticação PIX</label>
                    <input
                      id="payoutPixAuth"
                      className="nexus-input"
                      value={pixAuthenticationCode}
                      onChange={(e) => setPixAuthenticationCode(e.target.value)}
                    />
                  </div>
                </div>
                {!hasProof ? (
                  <p className="feedback warn movement-composer__hint">Informe ao menos um campo de comprovante.</p>
                ) : null}
              </section>
            </>
          ) : null}

          {error ? <p className="feedback error movement-composer__error">{error}</p> : null}
        </div>
      )}

      <footer className="movement-composer__footer">
        <button type="button" className="btn btn-ghost" onClick={onClose}>Cancelar</button>
        <button
          type="button"
          className="btn btn-primary"
          disabled={busy || !canSubmit}
          onClick={() => void handleSubmit()}
        >
          {busy ? 'Registrando…' : 'Registrar repasse'}
        </button>
      </footer>
    </>
  );

  return (
    <>
      {variant === 'embedded' ? (
        <section
          className="card ops-card movement-composer movement-composer--embedded"
          aria-labelledby="payout-composer-title"
        >
          {composerBody}
        </section>
      ) : (
        <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
          <div
            className="dialog-card movement-composer"
            role="dialog"
            aria-modal="true"
            aria-labelledby="payout-composer-title"
            onClick={(e) => e.stopPropagation()}
          >
            {composerBody}
          </div>
        </div>
      )}

      <BankAccountPickerModal
        open={bankPickerOpen}
        onClose={() => setBankPickerOpen(false)}
        ownerId={strawManId}
        excludeAccountId={sourceAccountId}
        onSelected={(row) => {
          setDestBank(row);
          setBankPickerOpen(false);
        }}
      />

      <CryptoWalletPickerModal
        open={cryptoPickerOpen}
        onClose={() => setCryptoPickerOpen(false)}
        ownerId={strawManId}
        allowAnyStrawMan
        onSelected={(row) => {
          setDestCrypto(row);
          setCryptoPickerOpen(false);
        }}
      />
    </>
  );
}
