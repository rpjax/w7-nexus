import type { ApiDocsView, AuthLevel } from './types';

export function parseApiDocsView(search: string): ApiDocsView {
  const params = new URLSearchParams(search);
  const view = params.get('view');
  const id = params.get('id');

  if (view === 'flow' && id) return { kind: 'flow', id };
  if (view === 'endpoint' && id) return { kind: 'endpoint', id };
  if (view === 'group' && id) return { kind: 'group', id };
  return { kind: 'overview' };
}

export function buildApiDocsUrl(view: ApiDocsView): string {
  const base = '/dashboard/admin/api-docs';
  if (view.kind === 'overview') return base;
  const params = new URLSearchParams({ view: view.kind, id: view.id });
  return `${base}?${params.toString()}`;
}

export function authLabel(auth: 'none' | 'jwt' | 'master-token'): string {
  if (auth === 'jwt') return 'JWT Bearer';
  if (auth === 'master-token') return 'Token mestre';
  return 'Público';
}

export function methodTone(method: string): string {
  switch (method) {
    case 'GET': return 'get';
    case 'POST': return 'post';
    case 'PUT': return 'put';
    case 'PATCH': return 'patch';
    case 'DELETE': return 'delete';
    default: return 'get';
  }
}

export async function copyText(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}

export function buildCurlExample(
  method: string,
  path: string,
  body?: string,
  auth: AuthLevel = 'none',
  token?: string | null,
): string {
  const base = import.meta.env.VITE_API_BASE_URL ?? window.location.origin;
  const url = `${base}${path}`;
  const lines = [`curl -X ${method} "${url}"`];

  if (auth === 'jwt' && token) {
    lines.push(`  -H "Authorization: Bearer ${token}"`);
  } else if (auth === 'master-token') {
    lines.push('  -H "Authorization: {Authentication:AdministratorToken}"');
  } else if (auth === 'jwt') {
    lines.push('  -H "Authorization: Bearer {accessToken}"');
  }

  if (body && method !== 'GET') {
    lines.push('  -H "Content-Type: application/json"');
    lines.push(`  -d '${body.replace(/\n/g, '').replace(/\s+/g, ' ')}'`);
  }

  return lines.join(' \\\n');
}
