export const MANAGEABLE_ACCOUNT_ROLES = [
  'Administrator',
  'Operator',
  'StrawMan',
  'OlxOperator',
] as const;

export const MANAGEABLE_ACCOUNT_PERMISSIONS = [
  'CreateOperatorAccount',
  'CreateAdministratorAccount',
] as const;

export type ManageableAccountRole = (typeof MANAGEABLE_ACCOUNT_ROLES)[number];
export type ManageableAccountPermission = (typeof MANAGEABLE_ACCOUNT_PERMISSIONS)[number];

export type AccountRoleDefinition = {
  id: ManageableAccountRole;
  label: string;
  description: string;
};

export type AccountPermissionDefinition = {
  id: ManageableAccountPermission;
  label: string;
  description: string;
};

export const ACCOUNT_ROLE_CATALOG: readonly AccountRoleDefinition[] = [
  {
    id: 'Administrator',
    label: 'Administrador',
    description: 'Acesso global ao painel e gestão da plataforma.',
  },
  {
    id: 'Operator',
    label: 'Operador',
    description: 'Painel operacional, equipes e pagamentos do operador.',
  },
  {
    id: 'StrawMan',
    label: 'Laranja',
    description: 'Painel do titular, pagamentos e configurações próprias.',
  },
  {
    id: 'OlxOperator',
    label: 'Operador OLX',
    description: 'Fluxos OLX e gestão de patches de anúncios.',
  },
] as const;

export const ACCOUNT_PERMISSION_CATALOG: readonly AccountPermissionDefinition[] = [
  {
    id: 'CreateOperatorAccount',
    label: 'Criar operador',
    description: 'Permite registrar novas contas de operador.',
  },
  {
    id: 'CreateAdministratorAccount',
    label: 'Criar administrador',
    description: 'Permite registrar novas contas de administrador.',
  },
] as const;

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

export function hasPermissionIgnoreCase(permissions: string[], permission: string): boolean {
  return permissions.some((item) => item.localeCompare(permission, undefined, { sensitivity: 'accent' }) === 0);
}

export function isAdministrator(roles: string[] | undefined | null): boolean {
  return hasRoleIgnoreCase(roles ?? [], 'Administrator');
}
