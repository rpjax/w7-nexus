import { useEffect, useState } from 'react';
import type { BankAccountType, PixKeyType } from '../../api/types';
import { IconButton } from '../IconButton';
import { BrazilianBankSelect } from './BrazilianBankSelect';
import { BANK_ACCOUNT_TYPE_OPTIONS, PIX_KEY_TYPE_OPTIONS } from '../../utils/financeLabels';
import {
  formatPixKeyInput,
  normalizePixKey,
  pixKeyHint,
  pixKeyInputMode,
  pixKeyMaxLength,
  pixKeyPlaceholder,
  validatePixKey,
} from '../../utils/pixKey';

export type BankAccountCreatePayload = {
  bank: number;
  agency: string;
  accountNumber: string;
  accountDigit: string | null;
  accountType: BankAccountType;
  pixKeyType: PixKeyType;
  pixKey: string;
  label: string | null;
};

type BankAccountCreateModalProps = {
  open: boolean;
  busy: boolean;
  strawLabel?: string | null;
  onClose: () => void;
  onSubmit: (payload: BankAccountCreatePayload) => void;
};

const EMPTY_FORM = {
  bank: null as number | null,
  agency: '',
  accountNumber: '',
  accountDigit: '',
  accountType: 'Checking' as BankAccountType,
  pixKeyType: 'Email' as PixKeyType,
  pixKey: '',
  label: '',
};

export function BankAccountCreateModal({
  open,
  busy,
  strawLabel,
  onClose,
  onSubmit,
}: BankAccountCreateModalProps) {
  const [form, setForm] = useState(EMPTY_FORM);
  const [error, setError] = useState('');
  const [showOptional, setShowOptional] = useState(false);

  useEffect(() => {
    if (!open) {
      setForm(EMPTY_FORM);
      setError('');
      setShowOptional(false);
    }
  }, [open]);

  if (!open) return null;

  function handleSubmit() {
    if (form.bank === null) {
      setError('Selecione o banco.');
      return;
    }
    if (!form.agency.trim()) {
      setError('Informe a agência.');
      return;
    }
    if (!form.accountNumber.trim()) {
      setError('Informe o número da conta.');
      return;
    }
    const pixError = validatePixKey(form.pixKeyType, form.pixKey);
    if (pixError) {
      setError(pixError);
      return;
    }
    const normalizedPixKey = normalizePixKey(form.pixKeyType, form.pixKey);
    if (!normalizedPixKey) {
      setError('Informe a chave PIX.');
      return;
    }
    setError('');
    onSubmit({
      bank: form.bank,
      agency: form.agency.trim(),
      accountNumber: form.accountNumber.trim(),
      accountDigit: form.accountDigit.trim() || null,
      accountType: form.accountType,
      pixKeyType: form.pixKeyType,
      pixKey: normalizedPixKey,
      label: form.label.trim() || null,
    });
  }

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div
        className="dialog-card dialog-card--wide bank-create-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="bank-create-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="account-picker-header bank-create-modal__head">
          <div className="account-picker-heading">
            <h3 id="bank-create-modal-title" className="account-picker-title">Nova conta bancária</h3>
            <p className="account-picker-sub">
              {strawLabel ? `Laranja: ${strawLabel}` : 'Conta de destino PIX para saques.'}
            </p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        <div className="bank-create-modal__body">
          {error ? (
            <p className="bank-create-modal__error" role="alert">{error}</p>
          ) : null}

          <div className="bank-create-form">
            <div className="field">
              <label htmlFor="createBank">Banco</label>
              <BrazilianBankSelect value={form.bank} onChange={(bank) => setForm((f) => ({ ...f, bank }))} />
            </div>

            <div className="bank-create-form__account-row">
              <div className="field">
                <label htmlFor="createAgency">Agência</label>
                <input
                  id="createAgency"
                  className="nexus-input"
                  value={form.agency}
                  onChange={(e) => setForm((f) => ({ ...f, agency: e.target.value }))}
                  inputMode="numeric"
                  autoComplete="off"
                />
              </div>
              <div className="field">
                <label htmlFor="createAccountNumber">Conta</label>
                <input
                  id="createAccountNumber"
                  className="nexus-input"
                  value={form.accountNumber}
                  onChange={(e) => setForm((f) => ({ ...f, accountNumber: e.target.value }))}
                  inputMode="numeric"
                  autoComplete="off"
                />
              </div>
              <div className="field bank-create-form__digit">
                <label htmlFor="createAccountDigit">Díg.</label>
                <input
                  id="createAccountDigit"
                  className="nexus-input"
                  value={form.accountDigit}
                  onChange={(e) => setForm((f) => ({ ...f, accountDigit: e.target.value }))}
                  inputMode="numeric"
                  autoComplete="off"
                  maxLength={2}
                />
              </div>
            </div>

            <div className="field">
              <label htmlFor="createAccountType">Tipo de conta</label>
              <select
                id="createAccountType"
                className="nexus-input"
                value={form.accountType}
                onChange={(e) => setForm((f) => ({ ...f, accountType: e.target.value as BankAccountType }))}
              >
                {BANK_ACCOUNT_TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>

            <div className="bank-create-form__pix-section">
              <span className="bank-create-form__kicker">Chave PIX (obrigatória)</span>

              <div className="field">
                <label htmlFor="createPixKeyType">Tipo da chave PIX</label>
                <select
                  id="createPixKeyType"
                  className="nexus-input"
                  value={form.pixKeyType}
                  required
                  onChange={(e) => setForm((f) => ({
                    ...f,
                    pixKeyType: e.target.value as PixKeyType,
                    pixKey: '',
                  }))}
                >
                  {PIX_KEY_TYPE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>

              <div className="field">
                <label htmlFor="createPixKey">Chave PIX</label>
                <input
                  id="createPixKey"
                  className="nexus-input"
                  value={form.pixKey}
                  onChange={(e) => setForm((f) => ({
                    ...f,
                    pixKey: formatPixKeyInput(f.pixKeyType, e.target.value),
                  }))}
                  placeholder={pixKeyPlaceholder(form.pixKeyType)}
                  inputMode={pixKeyInputMode(form.pixKeyType)}
                  type={form.pixKeyType === 'Email' ? 'email' : 'text'}
                  maxLength={pixKeyMaxLength(form.pixKeyType)}
                  required
                  autoComplete="off"
                />
                <p className="muted small">{pixKeyHint(form.pixKeyType)}</p>
              </div>
            </div>

            <button
              type="button"
              className="bank-create-form__optional-toggle"
              aria-expanded={showOptional}
              onClick={() => setShowOptional((v) => !v)}
            >
              {showOptional ? 'Ocultar apelido' : 'Apelido (opcional)'}
              <span className="bank-create-form__optional-chevron" aria-hidden="true">{showOptional ? '▾' : '▸'}</span>
            </button>

            {showOptional ? (
              <div className="bank-create-form__optional">
                <div className="field">
                  <label htmlFor="createLabel">Apelido</label>
                  <input
                    id="createLabel"
                    className="nexus-input"
                    value={form.label}
                    onChange={(e) => setForm((f) => ({ ...f, label: e.target.value }))}
                    placeholder="Ex.: Conta principal"
                    autoComplete="off"
                  />
                </div>
              </div>
            ) : null}
          </div>
        </div>

        <footer className="bank-create-modal__foot">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </button>
          <button type="button" className="btn btn-primary" disabled={busy} onClick={handleSubmit}>
            {busy ? 'Salvando…' : 'Cadastrar conta'}
          </button>
        </footer>
      </div>
    </div>
  );
}
