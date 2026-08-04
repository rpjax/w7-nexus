import { useState } from 'react';
import { validateHostPattern } from '../../features/scripts/hostPatternValidation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

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
    <div className="flex flex-col gap-2">
      {patterns.length === 0 && !compactEmpty ? (
        <p className="rounded-lg border border-dashed border-border/60 px-3 py-2 text-sm text-muted-foreground">
          Sem hosts — resolvido apenas via <code className="font-mono text-xs">GET /scripts?name=…</code>
        </p>
      ) : patterns.length > 0 ? (
        <ul className="flex flex-col gap-1.5">
          {patterns.map((pattern) => (
            <li
              key={pattern}
              className="flex items-center justify-between gap-2 rounded-lg border border-border/50 bg-muted/20 px-3 py-2"
            >
              <code className="font-mono text-sm">{pattern}</code>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled}
                onClick={() => removePattern(pattern)}
              >
                Remover
              </Button>
            </li>
          ))}
        </ul>
      ) : null}

      <div className="flex gap-2">
        <Input
          className="flex-1 font-mono text-sm"
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
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="shrink-0"
          disabled={disabled || !draft.trim()}
          onClick={addPattern}
        >
          Adicionar
        </Button>
      </div>

      {error ? <p className={cn('text-sm text-destructive')}>{error}</p> : null}

      {!hideHint ? (
        <p className="text-xs text-muted-foreground">
          Aceita <code className="font-mono">*</code>, <code className="font-mono">*.domínio.tld</code> ou host exato.
        </p>
      ) : null}
    </div>
  );
}
