export function shortId(id: string, max = 14): string {
  if (!id) return '—';
  return id.length <= max ? id : `${id.slice(0, max - 1)}…`;
}

export function shortTx(tx: string): string {
  if (!tx) return '—';
  return tx.length <= 16 ? tx : `${tx.slice(0, 8)}…${tx.slice(-6)}`;
}

export function joinList(list?: string[] | null): string {
  if (!list?.length) return '—';
  return list.join(', ');
}

export function maskToken(token: string): string {
  if (!token.trim()) return '';
  if (token.length <= 10) return '*'.repeat(token.length);
  return `${token.slice(0, 4)}…${token.slice(-4)}`;
}

export function maskKey(key: string): string {
  return maskToken(key);
}

export function formatDateTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString('pt-BR');
}

export function formatUtc(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toISOString().replace('T', ' ').replace('Z', '');
}
