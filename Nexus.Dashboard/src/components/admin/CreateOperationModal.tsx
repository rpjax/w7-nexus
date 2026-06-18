import { useEffect, useState } from 'react';

type CreateOperationModalProps = {
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSubmit: (name: string, description: string | null) => void;
};

export function CreateOperationModal({
  open,
  busy,
  onClose,
  onSubmit,
}: CreateOperationModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  useEffect(() => {
    if (!open) {
      setName('');
      setDescription('');
    }
  }, [open]);

  if (!open) return null;

  function handleSubmit() {
    const trimmed = name.trim();
    if (!trimmed) return;
    onSubmit(trimmed, description.trim() || null);
  }

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div className="dialog-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-stack-header">
          <div>
            <h3>Nova operação</h3>
            <p className="muted small">Registre uma operação no repositório central.</p>
          </div>
          <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </div>

        <div className="form-grid">
          <div className="field">
            <label htmlFor="createOpName">Nome</label>
            <input
              id="createOpName"
              className="nexus-input"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ex.: Operação Atlas"
              autoFocus
            />
          </div>
          <div className="field span-2">
            <label htmlFor="createOpDesc">Descrição</label>
            <textarea
              id="createOpDesc"
              className="nexus-input"
              rows={2}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Contexto e escopo da operação"
            />
          </div>
        </div>

        <div className="dialog-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </button>
          <button
            type="button"
            className="btn btn-primary"
            disabled={busy || !name.trim()}
            onClick={handleSubmit}
          >
            {busy ? 'Registrando…' : 'Registrar operação'}
          </button>
        </div>
      </div>
    </div>
  );
}
