import { useId, useRef, useState, type ChangeEvent, type DragEvent } from 'react';
import { CodeStudioPanel } from './CodeStudioPanel';
import {
  countSourceLines,
  formatScriptFileSize,
  getSourceCodeByteSize,
  readScriptFile,
  SCRIPT_FILE_ACCEPT,
} from '../../features/scripts/readScriptFile';

type ReleaseSourcePanelProps = {
  versionLabel: string;
  value: string;
  fileName: string | null;
  origin: 'file' | 'editor' | null;
  onChange: (value: string) => void;
  onFileNameChange: (fileName: string | null) => void;
  onOriginChange: (origin: 'file' | 'editor' | null) => void;
};

const IIFE_TEMPLATE = "(function(){\n  \n})();\n";

export function ReleaseSourcePanel({
  versionLabel,
  value,
  fileName,
  origin,
  onChange,
  onFileNameChange,
  onOriginChange,
}: ReleaseSourcePanelProps) {
  const inputId = useId();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);

  const hasSource = value.trim().length > 0;
  const lineCount = countSourceLines(value);
  const sizeBytes = getSourceCodeByteSize(value);

  async function ingestFile(file: File) {
    setLoading(true);
    setError(null);

    const result = await readScriptFile(file);
    setLoading(false);

    if (!result.ok) {
      setError(result.message);
      return;
    }

    onChange(result.content);
    onFileNameChange(result.fileName);
    onOriginChange('file');
    setEditorOpen(true);
  }

  function handleFileInput(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (file) void ingestFile(file);
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setDragging(false);
    const file = event.dataTransfer.files.item(0);
    if (file) void ingestFile(file);
  }

  function clearSource() {
    onChange('');
    onFileNameChange(null);
    onOriginChange(null);
    setError(null);
    setEditorOpen(false);
  }

  function openManualEditor() {
    if (!hasSource) {
      onChange(IIFE_TEMPLATE);
      onOriginChange('editor');
    }
    setEditorOpen(true);
  }

  return (
    <div className="scripts-release-source">
      <p className="scripts-release-source__lead muted small">
        Release <span className="mono">{versionLabel}</span> · carregue o bundle ou use o editor.
      </p>

      {!hasSource ? (
        <div
          className={`scripts-release-source__dropzone ${dragging ? 'is-dragging' : ''}`}
          onDragEnter={(event) => {
            event.preventDefault();
            setDragging(true);
          }}
          onDragOver={(event) => event.preventDefault()}
          onDragLeave={(event) => {
            event.preventDefault();
            if (event.currentTarget === event.target) setDragging(false);
          }}
          onDrop={handleDrop}
        >
          <input
            ref={fileInputRef}
            id={inputId}
            type="file"
            accept={SCRIPT_FILE_ACCEPT}
            className="scripts-release-source__input"
            onChange={handleFileInput}
          />

          <div className="scripts-release-source__dropzone-body">
            <p className="scripts-release-source__dropzone-title">
              {loading ? 'Lendo bundle…' : 'Arraste um bundle .js'}
            </p>
            <p className="scripts-release-source__dropzone-hint muted small">
              Saída do build (webpack, rollup, esbuild…) · até {formatScriptFileSize(5 * 1024 * 1024)}
            </p>
            <button
              type="button"
              className="btn btn-scripts-outline btn-sm"
              disabled={loading}
              onClick={() => fileInputRef.current?.click()}
            >
              Selecionar arquivo
            </button>
          </div>
        </div>
      ) : (
        <input
          ref={fileInputRef}
          id={inputId}
          type="file"
          accept={SCRIPT_FILE_ACCEPT}
          className="scripts-release-source__input"
          onChange={handleFileInput}
        />
      )}

      {error ? (
        <p className="scripts-release-source__error" role="alert">{error}</p>
      ) : null}

      {hasSource ? (
        <div className="scripts-release-source__loaded">
          <div className="scripts-release-source__meta">
            <div>
              <span className="scripts-release-source__meta-label muted small">Origem</span>
              <strong>{origin === 'file' && fileName ? fileName : 'Editor manual'}</strong>
            </div>
            <div>
              <span className="scripts-release-source__meta-label muted small">Tamanho</span>
              <strong className="mono">{formatScriptFileSize(sizeBytes)}</strong>
            </div>
            <div>
              <span className="scripts-release-source__meta-label muted small">Linhas</span>
              <strong className="mono">{lineCount.toLocaleString('pt-BR')}</strong>
            </div>
          </div>
          <div className="scripts-release-source__loaded-actions">
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              disabled={loading}
              onClick={() => fileInputRef.current?.click()}
            >
              Trocar arquivo
            </button>
            <button type="button" className="btn btn-ghost btn-sm" onClick={clearSource}>
              Limpar
            </button>
          </div>
        </div>
      ) : (
        <p className="scripts-release-source__manual-hint muted small">
          Sem bundle?{' '}
          <button type="button" className="scripts-release-source__manual-link" onClick={openManualEditor}>
            Escrever no editor
          </button>
        </p>
      )}

      {hasSource || editorOpen ? (
        <div className="scripts-release-source__editor">
          <button
            type="button"
            className="scripts-release-source__editor-toggle"
            aria-expanded={editorOpen}
            onClick={() => setEditorOpen((open) => !open)}
          >
            <span>Visualizar / editar</span>
            <span className="scripts-release-source__editor-chevron" aria-hidden="true">{editorOpen ? '▾' : '▸'}</span>
          </button>

          {editorOpen ? (
            <CodeStudioPanel value={value} onChange={(next) => {
              onChange(next);
              if (origin !== 'file') onOriginChange('editor');
            }} height="min(48vh, 480px)" />
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
