import { useEffect, useState } from 'react';

type AddCustomChannelModalProps = {
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSubmit: (customName: string) => void;
};

export function AddCustomChannelModal({ open, busy, onClose, onSubmit }: AddCustomChannelModalProps) {
  const [name, setName] = useState('');

  useEffect(() => {
    if (!open) setName('');
  }, [open]);

  if (!open) return null;

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div className="dialog-card scripts-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-stack-header">
          <div>
            <h3>Canal customizado</h3>
            <p className="muted small">Ex.: beta, canary, qa</p>
          </div>
          <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </div>

        <div className="field">
          <label htmlFor="customChannelName">Nome</label>
          <input
            id="customChannelName"
            className="nexus-input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="beta"
            autoFocus
          />
        </div>

        <div className="dialog-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>Cancelar</button>
          <button
            type="button"
            className="btn btn-primary"
            disabled={busy || !name.trim()}
            onClick={() => onSubmit(name.trim())}
          >
            {busy ? 'Adicionando…' : 'Adicionar canal'}
          </button>
        </div>
      </div>
    </div>
  );
}
