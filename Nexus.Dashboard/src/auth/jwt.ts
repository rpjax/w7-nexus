import type { AuthUser } from './types';

type JwtPayload = {
  sub?: string;
  unique_name?: string;
  role?: string | string[];
  permission?: string | string[];
  exp?: number;
};

function readClaimValues(value: string | string[] | undefined): string[] {
  if (!value) return [];
  return Array.isArray(value) ? value : [value];
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const json = atob(padded);
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

export function userFromAccessToken(accessToken: string): AuthUser | null {
  const payload = decodeJwtPayload(accessToken);
  if (!payload?.sub || !payload.unique_name) return null;

  return {
    accountId: payload.sub,
    username: payload.unique_name,
    roles: readClaimValues(payload.role),
    permissions: readClaimValues(payload.permission),
  };
}

export function isTokenExpired(token: string, bufferSeconds = 30): boolean {
  const payload = decodeJwtPayload(token);
  if (!payload?.exp) return true;
  return payload.exp * 1000 <= Date.now() + bufferSeconds * 1000;
}

export function isIsoDateExpired(isoDate: string, bufferSeconds = 30): boolean {
  const expiresAt = Date.parse(isoDate);
  if (Number.isNaN(expiresAt)) return true;
  return expiresAt <= Date.now() + bufferSeconds * 1000;
}
