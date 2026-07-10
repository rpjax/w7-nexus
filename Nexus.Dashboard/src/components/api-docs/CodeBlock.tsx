import { useState } from 'react';
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
    <div className="api-code-block">
      <div className="api-code-block__header">
        <span className="api-code-block__label">{label ?? language.toUpperCase()}</span>
        <button type="button" className="api-code-block__copy" onClick={() => void handleCopy()}>
          {copied ? 'Copiado' : 'Copiar'}
        </button>
      </div>
      <pre className="api-code-block__body">
        <code>{code}</code>
      </pre>
    </div>
  );
}
