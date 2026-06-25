import { useState } from 'react';
import type { CryptoWalletRow } from '../../api/types';
import { upsertCryptoWalletAddress } from '../../api/cryptoWallets';
import {
  cryptoWalletPickerLabel,
  formatCryptoWalletAddresses,
  formatCryptoWalletBalances,
} from '../../utils/cryptoWalletDisplay';
import { ADDRESS_NAMESPACE_OPTIONS } from '../../utils/financeLabels';
import { formatUtc, shortId } from '../../utils/format';

type CryptoWalletCardProps = {
  row: CryptoWalletRow;
  onUpdated?: (row: CryptoWalletRow) => void;
  onError?: (message: string) => void;
};

export function CryptoWalletCard({ row, onUpdated, onError }: CryptoWalletCardProps) {
  const [addingAddress, setAddingAddress] = useState(false);
  const [namespace, setNamespace] = useState(1);
  const [address, setAddress] = useState('');
  const [memo, setMemo] = useState('');
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);

  const title = row.label?.trim() || cryptoWalletPickerLabel(row, 10);
  const balances = formatCryptoWalletBalances(row);
  const addresses = formatCryptoWalletAddresses(row);

  async function copyFirstAddress() {
    const first = row.addresses?.[0]?.address;
    if (!first) return;
    try {
      await navigator.clipboard.writeText(first);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1800);
    } catch {
      onError?.('Não foi possível copiar o endereço.');
    }
  }

  async function saveAddress() {
    if (!address.trim()) {
      onError?.('Informe o endereço.');
      return;
    }
    setBusy(true);
    try {
      const result = await upsertCryptoWalletAddress(row.id, {
        namespace,
        address: address.trim(),
        memo: memo.trim() || null,
      });
      if (!result.ok) {
        onError?.(result.error);
        return;
      }
      setAddingAddress(false);
      setAddress('');
      setMemo('');
      if (result.data) onUpdated?.(result.data);
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className="crypto-wallet-card">
      <div className="crypto-wallet-card__head">
        <span className="crypto-wallet-card__title">{title}</span>
        {row.label?.trim() ? (
          <span className="crypto-wallet-card__addresses muted small">{addresses}</span>
        ) : null}
      </div>

      {!row.label?.trim() ? (
        <p className="crypto-wallet-card__addresses muted small">{addresses}</p>
      ) : null}

      <p className="crypto-wallet-card__balances">
        {balances === 'Sem saldo' ? (
          <span className="muted small">Sem saldo registrado</span>
        ) : (
          <strong>{balances}</strong>
        )}
      </p>

      {addingAddress ? (
        <div className="crypto-wallet-card__address-form">
          <div className="field">
            <label htmlFor={`ns-${row.id}`}>Namespace</label>
            <select
              id={`ns-${row.id}`}
              className="nexus-input"
              value={namespace}
              onChange={(e) => setNamespace(Number(e.target.value))}
            >
              {ADDRESS_NAMESPACE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label htmlFor={`addr-${row.id}`}>Endereço</label>
            <input
              id={`addr-${row.id}`}
              className="nexus-input mono"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
            />
          </div>
          <div className="crypto-wallet-card__address-actions">
            <button type="button" className="btn btn-primary btn-sm" disabled={busy} onClick={() => void saveAddress()}>
              {busy ? '…' : 'Salvar'}
            </button>
            <button type="button" className="btn btn-ghost btn-sm" disabled={busy} onClick={() => setAddingAddress(false)}>
              Cancelar
            </button>
          </div>
        </div>
      ) : null}

      <footer className="crypto-wallet-card__footer">
        <span className="crypto-wallet-card__meta muted small" title={row.id}>
          {shortId(row.id, 12)} · {formatUtc(row.updatedAt)}
        </span>
        <div className="crypto-wallet-card__actions">
          {row.addresses?.[0] ? (
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => void copyFirstAddress()}>
              {copied ? 'Copiado' : 'Copiar endereço'}
            </button>
          ) : null}
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => setAddingAddress(true)}>
            {row.addresses?.length ? 'Novo endereço' : 'Adicionar endereço'}
          </button>
        </div>
      </footer>
    </li>
  );
}
