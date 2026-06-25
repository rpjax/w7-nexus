import { useEffect, useState } from 'react';
import type { BankAccountRow } from '../../api/types';
import { updateBankAccountLabel } from '../../api/bankAccounts';
import {
  bankAccountCopyText,
  bankAccountSummary,
  formatBankAccountBalance,
  formatBankAccountMeta,
  resolveBankMetadata,
} from '../../utils/bankAccountDisplay';

type BankAccountCardProps = {
  row: BankAccountRow;
  variant?: 'default' | 'compact';
  selectable?: boolean;
  onSelect?: (row: BankAccountRow) => void;
  onLabelUpdated?: (row: BankAccountRow) => void;
  onError?: (message: string) => void;
};

export function BankAccountCard({
  row,
  variant = 'default',
  selectable = false,
  onSelect,
  onLabelUpdated,
  onError,
}: BankAccountCardProps) {
  const [editingLabel, setEditingLabel] = useState(false);
  const [labelDraft, setLabelDraft] = useState(row.label ?? '');
  const [labelBusy, setLabelBusy] = useState(false);
  const [copiedField, setCopiedField] = useState(false);

  useEffect(() => {
    if (!editingLabel) setLabelDraft(row.label ?? '');
  }, [row.label, editingLabel]);

  useEffect(() => {
    if (!copiedField) return;
    const timer = window.setTimeout(() => setCopiedField(false), 1800);
    return () => window.clearTimeout(timer);
  }, [copiedField]);

  const meta = resolveBankMetadata(row.bank);
  const title = row.label?.trim() || (meta ? `${meta.code} — ${meta.name}` : row.bank);
  const balance = formatBankAccountBalance(row);
  const cardClass = [
    'bank-account-card',
    variant === 'compact' ? 'bank-account-card--compact' : '',
    selectable ? 'bank-account-card--selectable' : '',
  ]
    .filter(Boolean)
    .join(' ');

  async function copyText(value: string) {
    try {
      await navigator.clipboard.writeText(value);
      setCopiedField(true);
    } catch {
      onError?.('Não foi possível copiar para a área de transferência.');
    }
  }

  async function saveLabel() {
    setLabelBusy(true);
    try {
      const nextLabel = labelDraft.trim() || null;
      const result = await updateBankAccountLabel(row.id, nextLabel);
      if (!result.ok) {
        onError?.(result.error);
        return;
      }
      setEditingLabel(false);
      if (result.data) onLabelUpdated?.(result.data);
    } finally {
      setLabelBusy(false);
    }
  }

  return (
    <li className={cardClass}>
      <div className="bank-account-card__head">
        {editingLabel ? (
          <div className="bank-account-card__label-edit">
            <input
              className="nexus-input"
              value={labelDraft}
              onChange={(e) => setLabelDraft(e.target.value)}
              placeholder="Apelido da conta"
              autoFocus
              disabled={labelBusy}
              onKeyDown={(e) => {
                if (e.key === 'Enter') void saveLabel();
                if (e.key === 'Escape') {
                  setLabelDraft(row.label ?? '');
                  setEditingLabel(false);
                }
              }}
            />
            <button type="button" className="btn btn-primary btn-sm" disabled={labelBusy} onClick={() => void saveLabel()}>
              {labelBusy ? '…' : 'Salvar'}
            </button>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              disabled={labelBusy}
              onClick={() => {
                setLabelDraft(row.label ?? '');
                setEditingLabel(false);
              }}
            >
              Cancelar
            </button>
          </div>
        ) : (
          <>
            <span className="bank-account-card__title">{title}</span>
            {!row.label?.trim() ? (
              <button type="button" className="bank-account-card__add-label muted small" onClick={() => setEditingLabel(true)}>
                + Apelido
              </button>
            ) : null}
          </>
        )}
      </div>

      {row.label?.trim() && !editingLabel ? (
        <p className="bank-account-card__bank muted small">{bankAccountSummary(row)}</p>
      ) : null}

      <p className="bank-account-card__account">
        <span className="mono">{bankAccountCopyText(row)}</span>
      </p>

      {balance ? (
        <p className="bank-account-card__balance muted small">
          Saldo: <strong>{balance}</strong>
        </p>
      ) : null}

      <footer className="bank-account-card__footer">
        <span className="bank-account-card__meta muted small" title={row.id}>
          {formatBankAccountMeta(row)}
        </span>
        <div className="bank-account-card__actions">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => void copyText(bankAccountCopyText(row))}
          >
            {copiedField ? 'Copiado' : 'Copiar conta'}
          </button>
          {row.label?.trim() && !editingLabel ? (
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => setEditingLabel(true)}>
              Editar apelido
            </button>
          ) : null}
          {selectable && onSelect ? (
            <button type="button" className="btn btn-primary btn-sm" onClick={() => onSelect(row)}>
              Usar
            </button>
          ) : null}
        </div>
      </footer>
    </li>
  );
}
