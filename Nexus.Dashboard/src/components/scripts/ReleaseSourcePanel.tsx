import { useId, useRef, useState, type ChangeEvent, type DragEvent } from 'react';
import { CodeStudioPanel } from './CodeStudioPanel';
import {
  countSourceLines,
  formatScriptFileSize,
  getSourceCodeByteSize,
  readScriptFile,
  SCRIPT_FILE_ACCEPT,
} from '../../features/scripts/readScriptFile';
import { Button } from '@/components/ui/button';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { cn } from '@/lib/utils';
import { ChevronDownIcon } from 'lucide-react';

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
    <div className="flex flex-col gap-3">
      <p className="text-sm text-muted-foreground">
        Release <span className="font-mono">{versionLabel}</span> · carregue o bundle ou use o editor.
      </p>

      {!hasSource ? (
        <div
          className={cn(
            'relative rounded-xl border-2 border-dashed border-border/60 px-6 py-8 text-center transition-colors',
            dragging && 'border-warning/50 bg-warning/5',
          )}
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
            className="sr-only"
            onChange={handleFileInput}
          />

          <div className="flex flex-col items-center gap-2">
            <p className="text-sm font-medium">
              {loading ? 'Lendo bundle…' : 'Arraste um bundle .js'}
            </p>
            <p className="text-xs text-muted-foreground">
              Saída do build (webpack, rollup, esbuild…) · até {formatScriptFileSize(5 * 1024 * 1024)}
            </p>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={loading}
              onClick={() => fileInputRef.current?.click()}
            >
              Selecionar arquivo
            </Button>
          </div>
        </div>
      ) : (
        <input
          ref={fileInputRef}
          id={inputId}
          type="file"
          accept={SCRIPT_FILE_ACCEPT}
          className="sr-only"
          onChange={handleFileInput}
        />
      )}

      {error ? (
        <p className="text-sm text-destructive" role="alert">{error}</p>
      ) : null}

      {hasSource ? (
        <div className="rounded-lg border border-border/50 bg-muted/20 p-3">
          <div className="grid gap-3 sm:grid-cols-3">
            <div>
              <span className="text-xs text-muted-foreground">Origem</span>
              <p className="text-sm font-medium">{origin === 'file' && fileName ? fileName : 'Editor manual'}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">Tamanho</span>
              <p className="font-mono text-sm font-medium">{formatScriptFileSize(sizeBytes)}</p>
            </div>
            <div>
              <span className="text-xs text-muted-foreground">Linhas</span>
              <p className="font-mono text-sm font-medium">{lineCount.toLocaleString('pt-BR')}</p>
            </div>
          </div>
          <div className="mt-3 flex gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              disabled={loading}
              onClick={() => fileInputRef.current?.click()}
            >
              Trocar arquivo
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={clearSource}>
              Limpar
            </Button>
          </div>
        </div>
      ) : (
        <p className="text-xs text-muted-foreground">
          Sem bundle?{' '}
          <Button
            type="button"
            variant="link"
            size="sm"
            className="h-auto px-0 text-xs"
            onClick={openManualEditor}
          >
            Escrever no editor
          </Button>
        </p>
      )}

      {hasSource || editorOpen ? (
        <Collapsible open={editorOpen} onOpenChange={setEditorOpen}>
          <CollapsibleTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              className="w-full justify-between px-2"
            >
              <span>Visualizar / editar</span>
              <ChevronDownIcon className={cn('size-4 transition-transform', editorOpen && 'rotate-180')} />
            </Button>
          </CollapsibleTrigger>
          <CollapsibleContent className="pt-2">
            <CodeStudioPanel
              value={value}
              onChange={(next) => {
                onChange(next);
                if (origin !== 'file') onOriginChange('editor');
              }}
              height="min(48vh, 480px)"
            />
          </CollapsibleContent>
        </Collapsible>
      ) : null}
    </div>
  );
}
