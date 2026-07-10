import { useState } from 'react';
import { validateHostPattern } from '../../features/scripts/hostPatternValidation';

type HostPatternEditorProps = {
  patterns: string[];
  onChange: (patterns: string[]) => void;
  disabled?: boolean;
  /** Omits the dashed empty-state box when there are no patterns. */
  compactEmpty?: boolean;
  /** Hides the wildcard syntax hint below the add row. */
  hideHint?: boolean;
  placeholder?: string;
};

export function HostPatternEditor({
  patterns,
  onChange,
  disabled,
  compactEmpty,
  hideHint,
  placeholder = '*.olx.com.br ou olx.com.br',
}: HostPatternEditorProps) {
  const [draft, setDraft] = useState('');
  const [error, setError] = useState<string | null>(null);

  function addPattern() {
    const trimmed = draft.trim();
    if (!trimmed) return;

    const validationError = validateHostPattern(trimmed);
    if (validationError) {
      setError(validationError);
      return;
    }

    if (patterns.some((p) => p.toLowerCase() === trimmed.toLowerCase())) {
      setError('Este host já foi adicionado.');
      return;
    }

    onChange([...patterns, trimmed]);
    setDraft('');
    setError(null);
  }

  function removePattern(pattern: string) {
    onChange(patterns.filter((item) => item !== pattern));
  }

  return (
    <div className="scripts-host-editor">
      {patterns.length === 0 && !compactEmpty ? (
        <p className="scripts-host-editor__empty muted small">
          Sem hosts — resolvido apenas via <code>GET /scripts?name=…</code>
        </p>
      ) : patterns.length > 0 ? (
        <ul className="scripts-host-editor__list">
          {patterns.map((pattern) => (
            <li key={pattern} className="scripts-host-editor__item">
              <code>{pattern}</code>
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                disabled={disabled}
                onClick={() => removePattern(pattern)}
              >
                Remover
              </button>
            </li>
          ))}
        </ul>
      ) : null}

      <div className="scripts-host-editor__add">
        <input
          className="nexus-input scripts-host-editor__input"
          placeholder={placeholder}
          value={draft}
          disabled={disabled}
          aria-label="Novo host pattern"
          onChange={(e) => {
            setDraft(e.target.value);
            setError(null);
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              addPattern();
            }
          }}
        />
        <button
          type="button"
          className="btn btn-ghost btn-sm scripts-host-editor__add-btn"
          disabled={disabled || !draft.trim()}
          onClick={addPattern}
        >
          Adicionar
        </button>
      </div>

      {error ? <p className="scripts-host-editor__error small">{error}</p> : null}

      {!hideHint ? (
        <p className="scripts-host-editor__hint muted small">
          Aceita <code>*</code>, <code>*.domínio.tld</code> ou host exato.
        </p>
      ) : null}
    </div>
  );
}
