import type { AuthUser } from './types';

export const ROLES = {
  Administrator: 'Administrator',
  Operator: 'Operator',
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

export function hasAnyRole(user: AuthUser | null | undefined, roles: AppRole[]): boolean {
  return roles.some((role) => hasRole(user, role));
}

/** Operador ou administrador — painel operacional completo. */
export function canUseOperatorPanel(user: AuthUser | null | undefined): boolean {
  return isOperator(user) || isAdministrator(user);
}
