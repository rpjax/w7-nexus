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

export type AccessTone = 'admin' | 'operator' | 'straw' | 'olx' | 'permission';

export type AccountRoleDefinition = {
  id: ManageableAccountRole;
  label: string;
  description: string;
  tone: AccessTone;
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
    tone: 'admin',
  },
  {
    id: 'Operator',
    label: 'Operador',
    description: 'Painel operacional, equipes e pagamentos do operador.',
    tone: 'operator',
  },
  {
    id: 'StrawMan',
    label: 'Laranja',
    description: 'Painel do titular, pagamentos e configurações próprias.',
    tone: 'straw',
  },
  {
    id: 'OlxOperator',
    label: 'Operador OLX',
    description: 'Fluxos OLX e gestão de patches de anúncios.',
    tone: 'olx',
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

const ROLE_LABELS: Record<string, string> = Object.fromEntries(
  ACCOUNT_ROLE_CATALOG.map((role) => [role.id, role.label]),
);

const PERMISSION_LABELS: Record<string, string> = Object.fromEntries(
  ACCOUNT_PERMISSION_CATALOG.map((permission) => [permission.id, permission.label]),
);

const ROLE_TONES: Record<string, AccessTone> = Object.fromEntries(
  ACCOUNT_ROLE_CATALOG.map((role) => [role.id, role.tone]),
);

export function roleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role;
}

export function permissionLabel(permission: string): string {
  return PERMISSION_LABELS[permission] ?? permission;
}

export function roleTone(role: string): AccessTone {
  return ROLE_TONES[role] ?? 'operator';
}

export function roleDescription(role: string): string | null {
  return ACCOUNT_ROLE_CATALOG.find((item) => item.id === role)?.description ?? null;
}

export function permissionDescription(permission: string): string | null {
  return ACCOUNT_PERMISSION_CATALOG.find((item) => item.id === permission)?.description ?? null;
}

export function hasRoleIgnoreCase(roles: string[], role: string): boolean {
  return roles.some((item) => item.localeCompare(role, undefined, { sensitivity: 'accent' }) === 0);
}

export function hasPermissionIgnoreCase(permissions: string[], permission: string): boolean {
  return permissions.some((item) => item.localeCompare(permission, undefined, { sensitivity: 'accent' }) === 0);
}

export function summarizeRoles(roles: string[]): string {
  if (roles.length === 0) return 'Sem funções';
  return roles.map((role) => roleLabel(role)).join(' · ');
}
