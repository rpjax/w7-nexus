import type { AuthUser } from './types';

export const ROLES = {
  Administrator: 'Administrator',
  Operator: 'Operator',
  StrawMan: 'StrawMan',
  OlxOperator: 'OlxOperator',
} as const;

export type AppRole = (typeof ROLES)[keyof typeof ROLES];

export function hasRole(user: AuthUser | null | undefined, role: AppRole): boolean {
  return user?.roles.includes(role) ?? false;
}

export function isAdministrator(user: AuthUser | null | undefined): boolean {
  return hasRole(user, ROLES.Administrator);
}

export function isOperator(user: AuthUser | null | undefined): boolean {
  return hasRole(user, ROLES.Operator);
}

export function isStrawMan(user: AuthUser | null | undefined): boolean {
  return hasRole(user, ROLES.StrawMan);
}

export function isOlxOperator(user: AuthUser | null | undefined): boolean {
  return hasRole(user, ROLES.OlxOperator);
}

export function hasAnyRole(user: AuthUser | null | undefined, roles: AppRole[]): boolean {
  return roles.some((role) => hasRole(user, role));
}

/** Operador ou administrador — painel operacional completo. */
export function canUseOperatorPanel(user: AuthUser | null | undefined): boolean {
  return isOperator(user) || isAdministrator(user);
}

/** Conta com role laranja — painel de pagamentos do laranja. */
export function canUseStrawManPanel(user: AuthUser | null | undefined): boolean {
  return isStrawMan(user);
}

/** Operador OLX ou administrador — painel de spoof de anúncios. */
export function canUseOlxPanel(user: AuthUser | null | undefined): boolean {
  return isOlxOperator(user) || isAdministrator(user);
}
