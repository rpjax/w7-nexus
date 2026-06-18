import { useEffect, useState } from 'react';

type CreateTeamModalProps = {
  open: boolean;
  busy: boolean;
  operationName?: string;
  onClose: () => void;
  onSubmit: (name: string) => void;
};

export function CreateTeamModal({
  open,
  busy,
  operationName,
  onClose,
  onSubmit,
}: CreateTeamModalProps) {
  const [name, setName] = useState('');

  useEffect(() => {
    if (!open) setName('');
  }, [open]);

  if (!open) return null;

  function handleSubmit() {
    const trimmed = name.trim();
    if (!trimmed) return;
    onSubmit(trimmed);
  }

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div className="dialog-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-stack-header">
          <div>
            <h3>Nova equipe</h3>
            {operationName ? (
              <p className="muted small">Operação: {operationName}</p>
            ) : (
              <p className="muted small">Crie uma equipe para esta operação.</p>
            )}
          </div>
          <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </div>

        <div className="field">
          <label htmlFor="createTeamName">Nome da equipe</label>
          <input
            id="createTeamName"
            className="nexus-input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Ex.: Equipe Alpha"
            autoFocus
            onKeyDown={(e) => { if (e.key === 'Enter') handleSubmit(); }}
          />
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
            {busy ? 'Criando…' : 'Criar equipe'}
          </button>
        </div>
      </div>
    </div>
  );
}
