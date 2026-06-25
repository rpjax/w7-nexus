import { useEffect, useState } from 'react';
import { IconButton } from '../IconButton';
import { ADDRESS_NAMESPACE_OPTIONS } from '../../utils/financeLabels';

export type CryptoWalletCreatePayload = {
  namespace: number;
  address: string;
  memo: string | null;
  label: string | null;
};

type CryptoWalletCreateModalProps = {
  open: boolean;
  busy: boolean;
  strawLabel?: string | null;
  onClose: () => void;
  onSubmit: (payload: CryptoWalletCreatePayload) => void;
};

const EMPTY_FORM = {
  namespace: 1,
  address: '',
  memo: '',
  label: '',
};

export function CryptoWalletCreateModal({
  open,
  busy,
  strawLabel,
  onClose,
  onSubmit,
}: CryptoWalletCreateModalProps) {
  const [form, setForm] = useState(EMPTY_FORM);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!open) {
      setForm(EMPTY_FORM);
      setError('');
    }
  }, [open]);

  if (!open) return null;

  function handleSubmit() {
    if (!form.address.trim()) {
      setError('Informe o endereço da carteira.');
      return;
    }
    setError('');
    onSubmit({
      namespace: form.namespace,
      address: form.address.trim(),
      memo: form.memo.trim() || null,
      label: form.label.trim() || null,
    });
  }

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div
        className="dialog-card dialog-card--wide bank-create-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="crypto-create-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="account-picker-header bank-create-modal__head">
          <div className="account-picker-heading">
            <h3 id="crypto-create-modal-title" className="account-picker-title">Nova carteira crypto</h3>
            <p className="account-picker-sub">
              {strawLabel ? `Laranja: ${strawLabel}` : 'Endereço de destino para transferências on-chain.'}
            </p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        <div className="bank-create-modal__body">
          {error ? <p className="bank-create-modal__error" role="alert">{error}</p> : null}

          <div className="bank-create-form">
            <div className="field">
              <label htmlFor="createNamespace">Rede / namespace</label>
              <select
                id="createNamespace"
                className="nexus-input"
                value={form.namespace}
                onChange={(e) => setForm((f) => ({ ...f, namespace: Number(e.target.value) }))}
              >
                {ADDRESS_NAMESPACE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="createAddress">Endereço</label>
              <input
                id="createAddress"
                className="nexus-input mono"
                value={form.address}
                onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
                autoComplete="off"
                spellCheck={false}
              />
            </div>
            <div className="field">
              <label htmlFor="createMemo">Memo / tag <span className="muted small">opcional</span></label>
              <input
                id="createMemo"
                className="nexus-input"
                value={form.memo}
                onChange={(e) => setForm((f) => ({ ...f, memo: e.target.value }))}
                autoComplete="off"
              />
            </div>
            <div className="field">
              <label htmlFor="createWalletLabel">Apelido <span className="muted small">opcional</span></label>
              <input
                id="createWalletLabel"
                className="nexus-input"
                value={form.label}
                onChange={(e) => setForm((f) => ({ ...f, label: e.target.value }))}
                placeholder="Ex.: Carteira principal USDT"
                autoComplete="off"
              />
            </div>
          </div>
        </div>

        <footer className="bank-create-modal__foot">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>Cancelar</button>
          <button type="button" className="btn btn-primary" disabled={busy} onClick={handleSubmit}>
            {busy ? 'Salvando…' : 'Cadastrar carteira'}
          </button>
        </footer>
      </div>
    </div>
  );
}
