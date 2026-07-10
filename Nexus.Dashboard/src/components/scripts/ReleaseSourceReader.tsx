import { useEffect, useState } from 'react';
import { countSourceLines, formatScriptFileSize } from '../../features/scripts/readScriptFile';
import { CodeStudioPanel } from './CodeStudioPanel';

type ReleaseSourceReaderProps = {
  version: string;
  sourceCode: string | null;
  sizeBytes: number;
  open: boolean;
  loading: boolean;
  onOpen: () => void;
  onClose: () => void;
};

export function ReleaseSourceReader({
  version,
  sourceCode,
  sizeBytes,
  open,
  loading,
  onOpen,
  onClose,
}: ReleaseSourceReaderProps) {
  const [expanded, setExpanded] = useState(false);
  const [wordWrap, setWordWrap] = useState(false);

  const lineCount = sourceCode ? countSourceLines(sourceCode) : 0;
  const sizeLabel = sizeBytes > 0 ? formatScriptFileSize(sizeBytes) : null;

  useEffect(() => {
    if (!open) setExpanded(false);
  }, [open]);

  useEffect(() => {
    if (!expanded) return undefined;

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setExpanded(false);
    }

    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [expanded]);

  function handleCopy() {
    if (!sourceCode) return;
    void navigator.clipboard.writeText(sourceCode);
  }

  const toolbar = (
    <div className="scripts-source-reader__toolbar">
      <div className="scripts-source-reader__meta">
        <strong className="mono">{version}</strong>
        {sizeLabel ? <span className="muted small">{sizeLabel}</span> : null}
        {lineCount > 0 ? <span className="muted small">{lineCount} linhas</span> : null}
      </div>
      <div className="scripts-source-reader__actions">
        <button
          type="button"
          className={`btn btn-ghost btn-sm ${wordWrap ? 'is-active' : ''}`}
          onClick={() => setWordWrap((value) => !value)}
          title="Alternar quebra de linha"
        >
          Quebra
        </button>
        <button type="button" className="btn btn-ghost btn-sm" onClick={handleCopy} disabled={!sourceCode}>
          Copiar
        </button>
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          onClick={() => setExpanded(true)}
        >
          Expandir
        </button>
        <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>
          Ocultar
        </button>
      </div>
    </div>
  );

  const editor = sourceCode ? (
    <CodeStudioPanel
      value={sourceCode}
      readOnly
      wordWrap={wordWrap}
      height={expanded ? 'calc(100dvh - 5.5rem)' : 'min(58vh, 560px)'}
    />
  ) : null;

  return (
    <section className={`scripts-source-reader ${open ? 'is-open' : ''}`} aria-label="Código-fonte do release">
      {!open ? (
        <div className="scripts-source-reader__closed">
          <div>
            <h3>Código-fonte</h3>
            <p className="muted small">
              {sizeLabel ? `${sizeLabel}` : 'Bundle'}
              {' · '}
              Editor Monaco com busca, minimap e modo expandido.
            </p>
          </div>
          <button
            type="button"
            className="btn btn-scripts-outline btn-sm"
            disabled={loading}
            onClick={onOpen}
          >
            {loading ? 'Carregando…' : 'Visualizar código'}
          </button>
        </div>
      ) : (
        <div className="scripts-source-reader__open">
          {toolbar}
          {loading ? (
            <div className="scripts-source-reader__loading muted">Carregando bundle…</div>
          ) : (
            editor
          )}
        </div>
      )}

      {expanded && sourceCode ? (
        <div
          className="scripts-source-reader-backdrop"
          role="presentation"
          onClick={() => setExpanded(false)}
        >
          <div
            className="scripts-source-reader scripts-source-reader--fullscreen"
            role="dialog"
            aria-modal="true"
            aria-label={`Código-fonte ${version}`}
            onClick={(event) => event.stopPropagation()}
          >
            <div className="scripts-source-reader__toolbar scripts-source-reader__toolbar--fullscreen">
              <div className="scripts-source-reader__meta">
                <strong className="mono">{version}</strong>
                <span className="muted small">Somente leitura</span>
              </div>
              <div className="scripts-source-reader__actions">
                <button
                  type="button"
                  className={`btn btn-ghost btn-sm ${wordWrap ? 'is-active' : ''}`}
                  onClick={() => setWordWrap((value) => !value)}
                >
                  Quebra
                </button>
                <button type="button" className="btn btn-ghost btn-sm" onClick={handleCopy}>
                  Copiar
                </button>
                <button type="button" className="btn btn-ghost btn-sm" onClick={() => setExpanded(false)}>
                  Fechar
                </button>
              </div>
            </div>
            <CodeStudioPanel
              value={sourceCode}
              readOnly
              wordWrap={wordWrap}
              height="calc(100dvh - 5.5rem)"
            />
          </div>
        </div>
      ) : null}
    </section>
  );
}
