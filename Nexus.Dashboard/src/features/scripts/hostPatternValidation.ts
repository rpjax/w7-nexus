export function validateHostPattern(value: string): string | null {
  const trimmed = value.trim().toLowerCase();

  if (!trimmed) return 'O host não pode ser vazio.';

  if (trimmed.includes('://') || trimmed.includes('/')) {
    return 'Não inclua protocolo ou caminho.';
  }

  if (trimmed.includes(':')) {
    return 'Não inclua porta no host.';
  }

  if (trimmed === '*') return null;

  if (trimmed.startsWith('*.')) {
    const domain = trimmed.slice(2);
    if (!domain.includes('.')) {
      return 'Use um domínio completo após *., ex: *.olx.com.br';
    }
    return null;
  }

  if (trimmed.includes('*')) {
    return 'Wildcard só é permitido como * ou *.domínio.tld';
  }

  return null;
}

export function validateHostPatterns(patterns: string[]): string | null {
  for (const pattern of patterns) {
    const error = validateHostPattern(pattern);
    if (error) return error;
  }
  return null;
}
