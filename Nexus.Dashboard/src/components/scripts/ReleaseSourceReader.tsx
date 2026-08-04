import { useEffect, useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
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

  function handleCopy() {
    if (!sourceCode) return;
    void navigator.clipboard.writeText(sourceCode);
  }

  const toolbar = (fullscreen?: boolean) => (
    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/50 px-3 py-2">
      <div className="flex flex-wrap items-center gap-2">
        <strong className="font-mono text-sm">{version}</strong>
        {sizeLabel ? <span className="text-xs text-muted-foreground">{sizeLabel}</span> : null}
        {lineCount > 0 ? <span className="text-xs text-muted-foreground">{lineCount} linhas</span> : null}
        {fullscreen ? <span className="text-xs text-muted-foreground">Somente leitura</span> : null}
      </div>
      <div className="flex flex-wrap items-center gap-1">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className={cn(wordWrap && 'bg-muted')}
          onClick={() => setWordWrap((value) => !value)}
          title="Alternar quebra de linha"
        >
          Quebra
        </Button>
        <Button type="button" variant="ghost" size="sm" onClick={handleCopy} disabled={!sourceCode}>
          Copiar
        </Button>
        {!fullscreen ? (
          <Button type="button" variant="ghost" size="sm" onClick={() => setExpanded(true)}>
            Expandir
          </Button>
        ) : null}
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={fullscreen ? () => setExpanded(false) : onClose}
        >
          {fullscreen ? 'Fechar' : 'Ocultar'}
        </Button>
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
    <section className="rounded-lg border border-border/60" aria-label="Código-fonte do release">
      {!open ? (
        <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
          <div>
            <h3 className="text-sm font-medium">Código-fonte</h3>
            <p className="text-xs text-muted-foreground">
              {sizeLabel ? `${sizeLabel}` : 'Bundle'}
              {' · '}
              Editor Monaco com busca, minimap e modo expandido.
            </p>
          </div>
          <Button type="button" variant="outline" size="sm" disabled={loading} onClick={onOpen}>
            {loading ? 'Carregando…' : 'Visualizar código'}
          </Button>
        </div>
      ) : (
        <div>
          {toolbar()}
          {loading ? (
            <div className="px-4 py-8 text-sm text-muted-foreground">Carregando bundle…</div>
          ) : (
            editor
          )}
        </div>
      )}

      <Dialog open={expanded && Boolean(sourceCode)} onOpenChange={(next) => !next && setExpanded(false)}>
        <DialogContent className="flex h-[calc(100dvh-2rem)] max-w-[calc(100vw-2rem)] flex-col gap-0 overflow-hidden p-0 sm:max-w-[calc(100vw-2rem)]">
          <DialogHeader className="sr-only">
            <DialogTitle>Código-fonte {version}</DialogTitle>
          </DialogHeader>
          {toolbar(true)}
          {sourceCode ? (
            <CodeStudioPanel
              value={sourceCode}
              readOnly
              wordWrap={wordWrap}
              height="calc(100dvh - 5.5rem)"
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </section>
  );
}
