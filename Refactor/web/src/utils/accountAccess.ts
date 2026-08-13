export const GRANTABLE_ACCOUNT_ROLES = ['Administrator'] as const;

export type GrantableAccountRole = (typeof GRANTABLE_ACCOUNT_ROLES)[number];

export type AccountRoleDefinition = {
  id: GrantableAccountRole;
  label: string;
  description: string;
};

/** Preset raiz. Demais papéis de produto são mandato (etapa 02), não roles soltos. */
export const ACCOUNT_ROLE_CATALOG: readonly AccountRoleDefinition[] = [
  {
    id: 'Administrator',
    label: 'Admin',
    description: 'Preset raiz: acesso irrestrito no hub. Criar ou revogar Admin é capacidade de Admin.',
  },
];

export function roleLabel(role: string): string {
  return ACCOUNT_ROLE_CATALOG.find((item) => item.id === role)?.label ?? role;
}

export function statusLabel(status: string | undefined | null): string {
  if (!status) return '—';
  if (status.localeCompare('Active', undefined, { sensitivity: 'accent' }) === 0) return 'Ativa';
  if (status.localeCompare('Disabled', undefined, { sensitivity: 'accent' }) === 0) return 'Desabilitada';
  return status;
}

export function hasRoleIgnoreCase(roles: string[], role: string): boolean {
  return roles.some((item) => item.localeCompare(role, undefined, { sensitivity: 'accent' }) === 0);
}

export function isAdministrator(roles: string[] | undefined | null): boolean {
  return hasRoleIgnoreCase(roles ?? [], 'Administrator');
}
