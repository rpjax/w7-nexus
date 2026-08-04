import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { copyText } from '../../features/api-docs/utils';

type CodeBlockProps = {
  code: string;
  label?: string;
  language?: string;
};

export function CodeBlock({ code, label, language = 'json' }: CodeBlockProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    const ok = await copyText(code);
    if (ok) {
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-[rgba(5,8,18,0.85)]">
      <div className="flex items-center justify-between border-b border-border bg-card/60 px-2.5 py-1.5">
        <span className="text-[0.68rem] font-semibold uppercase tracking-wide text-muted-foreground">
          {label ?? language.toUpperCase()}
        </span>
        <Button
          type="button"
          variant="ghost"
          size="xs"
          className="h-auto px-1.5 py-0.5 text-[0.72rem] text-primary hover:text-primary"
          onClick={() => void handleCopy()}
        >
          {copied ? 'Copiado' : 'Copiar'}
        </Button>
      </div>
      <pre className={cn('m-0 overflow-x-auto p-3.5 font-mono text-[0.76rem] leading-normal')}>
        <code>{code}</code>
      </pre>
    </div>
  );
}
