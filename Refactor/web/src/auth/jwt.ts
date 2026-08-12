import type { AuthUser } from './types';

const ROLE_CLAIM_KEYS = [
  'role',
  'roles',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
] as const;

const PERMISSION_CLAIM_KEYS = [
  'permission',
  'permissions',
] as const;

function readClaimList(payload: Record<string, unknown>, keys: readonly string[]): string[] {
  for (const key of keys) {
    const value = payload[key];
    if (typeof value === 'string' && value.trim()) {
      return [value.trim()];
    }
    if (Array.isArray(value)) {
      const items = value
        .filter((entry): entry is string => typeof entry === 'string' && entry.trim().length > 0)
        .map((entry) => entry.trim());
      if (items.length > 0) return items;
    }
  }
  return [];
}

export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const json = atob(padded);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function userFromAccessToken(accessToken: string): AuthUser | null {
  const payload = decodeJwtPayload(accessToken);
  if (!payload) return null;

  const accountId = typeof payload.sub === 'string' ? payload.sub : null;
  const username = typeof payload.unique_name === 'string'
    ? payload.unique_name
    : typeof payload.preferred_username === 'string'
      ? payload.preferred_username
      : null;

  if (!accountId || !username) return null;

  return {
    accountId,
    username,
    roles: readClaimList(payload, ROLE_CLAIM_KEYS),
    permissions: readClaimList(payload, PERMISSION_CLAIM_KEYS),
  };
}

export function isTokenExpired(token: string, bufferSeconds = 30): boolean {
  const payload = decodeJwtPayload(token);
  const exp = payload?.exp;
  if (typeof exp !== 'number') return true;
  return exp * 1000 <= Date.now() + bufferSeconds * 1000;
}

export function isIsoDateExpired(isoDate: string, bufferSeconds = 30): boolean {
  const expiresAt = Date.parse(isoDate);
  if (Number.isNaN(expiresAt)) return true;
  return expiresAt <= Date.now() + bufferSeconds * 1000;
}
