import { useEffect, useState } from 'react';
import { HostPatternEditor } from './HostPatternEditor';
import { ResolutionModeBadge } from './ResolutionModeBadge';

type CreateScriptModalProps = {
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSubmit: (payload: {
    name: string;
    hostPatterns: string[];
    priority: number;
    description: string | null;
  }) => void;
};

export function CreateScriptModal({ open, busy, onClose, onSubmit }: CreateScriptModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState(0);
  const [hostPatterns, setHostPatterns] = useState<string[]>([]);

  useEffect(() => {
    if (!open) {
      setName('');
      setDescription('');
      setPriority(0);
      setHostPatterns([]);
    }
  }, [open]);

  if (!open) return null;

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div
        className="dialog-card scripts-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-script-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="scripts-modal__header">
          <div>
            <p className="scripts-modal__kicker">Admin · Scripts</p>
            <h3 id="create-script-title">Novo script</h3>
            <p className="scripts-modal__lead muted small">
              Registre um patch de runtime no inventário central.
            </p>
          </div>
          <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </header>

        <div className="scripts-modal__body">
          <div className="form-grid scripts-modal__form">
            <div className="field scripts-modal__field">
              <label htmlFor="scriptName">Nome</label>
              <input
                id="scriptName"
                className="nexus-input scripts-modal__input"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="runtime, olx, …"
                autoFocus
              />
            </div>

            <div className="field scripts-modal__field">
              <label htmlFor="scriptPriority">Prioridade</label>
              <div className="scripts-priority-stepper scripts-modal__priority">
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  disabled={busy}
                  onClick={() => setPriority((value) => Math.max(0, value - 1))}
                  aria-label="Diminuir prioridade"
                >
                  −
                </button>
                <input
                  id="scriptPriority"
                  type="number"
                  className="nexus-input scripts-priority-stepper__input scripts-modal__input"
                  value={priority}
                  min={0}
                  onChange={(e) => setPriority(Number(e.target.value))}
                  title="Menor valor injeta primeiro em lookups por host"
                />
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  disabled={busy}
                  onClick={() => setPriority((value) => value + 1)}
                  aria-label="Aumentar prioridade"
                >
                  +
                </button>
              </div>
              <p className="scripts-modal__hint muted small">Menor valor = injeta primeiro.</p>
            </div>

            <div className="field scripts-modal__field scripts-modal__field--full">
              <label htmlFor="scriptDesc">Descrição</label>
              <textarea
                id="scriptDesc"
                className="nexus-input scripts-modal__input"
                rows={2}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Opcional — contexto operacional do patch"
              />
            </div>
          </div>

          <section className="scripts-modal__hosts" aria-labelledby="script-hosts-label">
            <div className="scripts-modal__hosts-head">
              <div>
                <h4 id="script-hosts-label" className="scripts-modal__section-title">
                  Host patterns
                  <span className="scripts-modal__optional">opcional</span>
                </h4>
                <p className="scripts-modal__hint muted small">
                  Define em quais hosts o script entra no resolve por URL.
                </p>
              </div>
              <ResolutionModeBadge hostPatterns={hostPatterns} />
            </div>
            <HostPatternEditor patterns={hostPatterns} onChange={setHostPatterns} disabled={busy} />
          </section>
        </div>

        <footer className="scripts-modal__footer dialog-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </button>
          <button
            type="button"
            className="btn btn-primary"
            disabled={busy || !name.trim()}
            onClick={() => onSubmit({
              name: name.trim(),
              hostPatterns,
              priority,
              description: description.trim() || null,
            })}
          >
            {busy ? 'Criando…' : 'Criar script'}
          </button>
        </footer>
      </div>
    </div>
  );
}
