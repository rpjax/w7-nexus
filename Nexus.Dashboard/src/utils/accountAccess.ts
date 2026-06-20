export const MANAGEABLE_ACCOUNT_ROLES = [
  'Administrator',
  'Operator',
  'StrawMan',
] as const;

export const MANAGEABLE_ACCOUNT_PERMISSIONS = [
  'CreateOperatorAccount',
  'CreateAdministratorAccount',
] as const;

export type ManageableAccountRole = (typeof MANAGEABLE_ACCOUNT_ROLES)[number];
export type ManageableAccountPermission = (typeof MANAGEABLE_ACCOUNT_PERMISSIONS)[number];

const ROLE_LABELS: Record<string, string> = {
  Administrator: 'Administrador',
  Operator: 'Operador',
  StrawMan: 'Laranja',
};

const PERMISSION_LABELS: Record<string, string> = {
  CreateOperatorAccount: 'Criar conta de operador',
  CreateAdministratorAccount: 'Criar conta de administrador',
};

export function roleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role;
}

export function permissionLabel(permission: string): string {
  return PERMISSION_LABELS[permission] ?? permission;
}

export function hasRoleIgnoreCase(roles: string[], role: string): boolean {
  return roles.some((item) => item.localeCompare(role, undefined, { sensitivity: 'accent' }) === 0);
}

export function hasPermissionIgnoreCase(permissions: string[], permission: string): boolean {
  return permissions.some((item) => item.localeCompare(permission, undefined, { sensitivity: 'accent' }) === 0);
}
