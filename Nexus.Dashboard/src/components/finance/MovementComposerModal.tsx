import { useCallback, useEffect, useMemo, useState } from 'react';
import { createBankAccountMovement, createCryptoWalletMovement } from '../../api/transfers';
import type { ActiveBalanceRow, BankAccountRow, CryptoWalletRow } from '../../api/types';
import { BankAccountPickerModal } from './BankAccountPickerModal';
import { CryptoWalletPickerModal } from './CryptoWalletPickerModal';
import { IconButton } from '../IconButton';
import { bankAccountPickerLabel } from '../../utils/bankAccountDisplay';
import { cryptoWalletPickerLabel } from '../../utils/cryptoWalletDisplay';
import {
  CHAIN_OPTIONS,
  CRYPTO_ASSET_OPTIONS,
  DESTINATION_TYPE_OPTIONS,
  ONRAMPING_METHOD_OPTIONS,
  chainEnumName,
  cryptoAssetEnumName,
} from '../../utils/financeLabels';
import {
  formatActiveBalanceAmount,
  formatActiveBalanceAmountInput,
  formatActiveBalanceSource,
  isMovementAmountWithinLimit,
  parseMovementAmount,
} from '../../utils/movementDisplay';
import { shortId } from '../../utils/format';

type MovementComposerModalProps = {
  open: boolean;
  onClose: () => void;
  strawManId: string;
  strawManUsername?: string | null;
  activeBalances: ActiveBalanceRow[];
  initialBalanceId?: string | null;
  onSuccess: (transferId: string) => void;
  variant?: 'modal' | 'embedded';
};

function defaultDestinationType(balance: ActiveBalanceRow): 'Pix' | 'Crypto' {
  return balance.account.kind === 'BankAccount' ? 'Crypto' : 'Pix';
}

function chainValueFromName(chain?: string | null): number {
  if (!chain) return 3;
  const match = CHAIN_OPTIONS.find((opt) => opt.enumName === chain);
  return match?.value ?? 3;
}

export function MovementComposerModal({
  open,
  onClose,
  strawManId,
  strawManUsername,
  activeBalances,
  initialBalanceId,
  onSuccess,
  variant = 'modal',
}: MovementComposerModalProps) {
  const movableBalances = useMemo(
    () => activeBalances.filter((balance) => balance.canMove),
    [activeBalances],
  );

  const [selectedBalanceId, setSelectedBalanceId] = useState<string | null>(null);
  const [amountInput, setAmountInput] = useState('');
  const [destinationType, setDestinationType] = useState<'Pix' | 'Crypto'>('Pix');
  const [destBank, setDestBank] = useState<BankAccountRow | null>(null);
  const [destCrypto, setDestCrypto] = useState<CryptoWalletRow | null>(null);
  const [onrampingMethod, setOnrampingMethod] = useState('Pix');
  const [producedAmountInput, setProducedAmountInput] = useState('');
  const [producedAsset, setProducedAsset] = useState('Usdt');
  const [producedChain, setProducedChain] = useState(3);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const [bankPickerOpen, setBankPickerOpen] = useState(false);
  const [cryptoPickerOpen, setCryptoPickerOpen] = useState(false);

  const selectedBalance = movableBalances.find((balance) => balance.balanceId === selectedBalanceId) ?? null;
  const sourceAccountId = selectedBalance?.account.id ?? null;
  const sourceIsBank = selectedBalance?.account.kind === 'BankAccount';
  const sourceIsCrypto = selectedBalance?.account.kind === 'CryptoWallet';
  const destIsBank = destinationType === 'Pix';
  const destIsCrypto = destinationType === 'Crypto';
  const isBankToCrypto = sourceIsBank && destIsCrypto;
  const isCryptoToBank = sourceIsCrypto && destIsBank;
  const isSameRail = (sourceIsBank && destIsBank) || (sourceIsCrypto && destIsCrypto);

  const maxAmount = selectedBalance?.amount ?? 0;
  const amount = parseMovementAmount(amountInput);
  const producedAmount = parseMovementAmount(producedAmountInput);
  const amountValid = isMovementAmountWithinLimit(amount, maxAmount);

  const selectBalance = useCallback((balance: ActiveBalanceRow) => {
    setSelectedBalanceId(balance.balanceId);
    setAmountInput(formatActiveBalanceAmountInput(balance));
    setDestinationType(defaultDestinationType(balance));
    setDestBank(null);
    setDestCrypto(null);
    setProducedAmountInput('');
    setProducedAsset(balance.asset ?? 'Usdt');
    setProducedChain(chainValueFromName(balance.chain));
    setError('');
  }, []);

  useEffect(() => {
    if (!open) return;
    setError('');
    setBusy(false);
    const defaultBalance = movableBalances.find((b) => b.balanceId === initialBalanceId)
      ?? movableBalances[0]
      ?? null;
    if (defaultBalance) {
      selectBalance(defaultBalance);
    } else {
      setSelectedBalanceId(null);
      setAmountInput('');
    }
  }, [open, initialBalanceId, movableBalances, selectBalance]);

  useEffect(() => {
    if (!open || variant !== 'modal') return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [open, onClose, variant]);

  const canSubmit = useMemo(() => {
    if (!selectedBalance || !strawManId.trim()) return false;
    if (!amountValid) return false;
    if (destIsBank && !destBank) return false;
    if (destIsCrypto && !destCrypto) return false;
    if (destIsBank && destBank?.id === sourceAccountId) return false;
    if (destIsCrypto && destCrypto?.id === sourceAccountId) return false;
    if (isBankToCrypto) {
      return Boolean(onrampingMethod && producedAmount > 0 && producedAsset && producedChain);
    }
    if (isCryptoToBank) {
      return producedAmount > 0;
    }
    return true;
  }, [
    selectedBalance,
    strawManId,
    amountValid,
    destIsBank,
    destBank,
    destIsCrypto,
    destCrypto,
    sourceAccountId,
    isBankToCrypto,
    onrampingMethod,
    producedAmount,
    producedAsset,
    producedChain,
    isCryptoToBank,
  ]);

  async function handleSubmit() {
    if (!selectedBalance || !canSubmit) return;
    setError('');
    setBusy(true);
    try {
      const movementPayload = {
        sourceBalanceId: selectedBalance.balanceId,
        amount,
        destinationBankAccountId: destIsBank ? destBank?.id ?? null : null,
        destinationCryptoWalletId: destIsCrypto ? destCrypto?.id ?? null : null,
        onrampingMethod: isBankToCrypto ? onrampingMethod : null,
        producedAmount: isBankToCrypto || isCryptoToBank ? producedAmount : null,
        producedAsset: isBankToCrypto ? cryptoAssetEnumName(producedAsset) : null,
        producedChain: isBankToCrypto ? chainEnumName(producedChain) : null,
      };

      const result = sourceIsBank
        ? await createBankAccountMovement(movementPayload)
        : await createCryptoWalletMovement(movementPayload);
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
          <h2 id="movement-composer-title" className="movement-composer__title">Nova movimentação</h2>
          <p className="movement-composer__sub muted small">
            {strawManUsername ? `@${strawManUsername}` : shortId(strawManId, 12)}
            {' · '}transfira saldo entre contas do laranja
          </p>
        </div>
        {variant === 'modal' ? <IconButton icon="x" label="Fechar" onClick={onClose} /> : null}
      </header>

      {movableBalances.length === 0 ? (
        <p className="movement-composer__empty muted">
          Não há saldos disponíveis para movimentar nesta cadeia.
        </p>
      ) : (
        <div className="movement-composer__body">
          <section className="movement-composer__section">
            <h3 className="movement-composer__section-title">1. Saldo de origem</h3>
            {movableBalances.length === 1 && selectedBalance ? (
              <div className="movement-composer__balance-card is-selected is-static">
                <strong>{formatActiveBalanceAmount(selectedBalance)}</strong>
                <span className="muted small">{formatActiveBalanceSource(selectedBalance)}</span>
              </div>
            ) : (
              <ul className="movement-composer__balance-list">
                {movableBalances.map((balance) => {
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
                        <span className="mono muted small">{shortId(balance.balanceId, 10)}</span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
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
                    aria-label="Valor a movimentar"
                    aria-invalid={amountInput.trim() !== '' && !amountValid}
                  />
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm"
                    onClick={() => setAmountInput(formatActiveBalanceAmountInput(selectedBalance))}
                  >
                    Usar total
                  </button>
                </div>
                <p className="muted small movement-composer__hint">
                  Máximo disponível: {formatActiveBalanceAmount(selectedBalance)}
                </p>
                {amountInput.trim() !== '' && !amountValid ? (
                  <p className="feedback warn movement-composer__hint">Informe um valor entre 0 e o saldo disponível.</p>
                ) : null}
              </section>

              <section className="movement-composer__section">
                <h3 className="movement-composer__section-title">3. Destino</h3>
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
                        setError('');
                      }}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
                {isSameRail ? (
                  <p className="muted small movement-composer__hint">
                    Movimentação entre contas do mesmo tipo — escolha um destino diferente da origem.
                  </p>
                ) : null}
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

              {isBankToCrypto ? (
                <section className="movement-composer__section movement-composer__section--conversion">
                  <h3 className="movement-composer__section-title">4. Conversão BRL → crypto</h3>
                  <div className="form-grid form-grid-wide">
                    <div className="field">
                      <label htmlFor="onramping">Onramping</label>
                      <select
                        id="onramping"
                        className="nexus-input"
                        value={onrampingMethod}
                        onChange={(e) => setOnrampingMethod(e.target.value)}
                      >
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
                      <select
                        id="producedAsset"
                        className="nexus-input"
                        value={producedAsset}
                        onChange={(e) => setProducedAsset(e.target.value)}
                      >
                        {CRYPTO_ASSET_OPTIONS.map((opt) => (
                          <option key={opt.value} value={opt.value}>{opt.label}</option>
                        ))}
                      </select>
                    </div>
                    <div className="field">
                      <label htmlFor="producedChain">Rede</label>
                      <select
                        id="producedChain"
                        className="nexus-input"
                        value={producedChain}
                        onChange={(e) => setProducedChain(Number(e.target.value))}
                      >
                        {CHAIN_OPTIONS.map((opt) => (
                          <option key={opt.value} value={opt.value}>{opt.label}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                </section>
              ) : null}

              {isCryptoToBank ? (
                <section className="movement-composer__section movement-composer__section--conversion">
                  <h3 className="movement-composer__section-title">4. Conversão crypto → BRL</h3>
                  <div className="field">
                    <label htmlFor="producedBrl">Valor em reais (BRL)</label>
                    <input
                      id="producedBrl"
                      className="nexus-input"
                      inputMode="decimal"
                      value={producedAmountInput}
                      onChange={(e) => setProducedAmountInput(e.target.value)}
                    />
                  </div>
                </section>
              ) : null}
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
          {busy ? 'Registrando…' : 'Registrar movimentação'}
        </button>
      </footer>
    </>
  );

  return (
    <>
      {variant === 'embedded' ? (
        <section
          className="card ops-card movement-composer movement-composer--embedded"
          aria-labelledby="movement-composer-title"
        >
          {composerBody}
        </section>
      ) : (
        <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
          <div
            className="dialog-card movement-composer"
            role="dialog"
            aria-modal="true"
            aria-labelledby="movement-composer-title"
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
        excludeAccountId={sourceIsBank ? sourceAccountId : null}
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
        excludeAccountId={sourceIsCrypto ? sourceAccountId : null}
        onSelected={(row) => {
          setDestCrypto(row);
          setCryptoPickerOpen(false);
        }}
      />
    </>
  );
}
