export type OperationScope = 'global-admin' | 'operation-admin' | 'team-leader' | 'operator';

const LIST_PATHS: Record<OperationScope, string> = {
  'global-admin': '/dashboard/admin/operations',
  'operation-admin': '/dashboard/operation-admin/operations',
  'team-leader': '/dashboard/team-leader/operations',
  operator: '/dashboard/operations',
};

export function listPath(scope: OperationScope): string {
  return LIST_PATHS[scope];
}

export function detailPath(scope: OperationScope, operationId: string): string {
  return `${LIST_PATHS[scope]}/${encodeURIComponent(operationId)}`;
}

export function cardScope(scope: OperationScope): 'global-admin' | 'operation-admin' | 'team-leader' {
  if (scope === 'operation-admin') return 'operation-admin';
  if (scope === 'team-leader') return 'team-leader';
  return 'global-admin';
}

export function isOperationDetailPath(pathname: string): boolean {
  const normalized = pathname.replace(/\/$/, '').toLowerCase();
  return (
    /^\/dashboard\/admin\/operations\/[^/]+$/.test(normalized)
    || /^\/dashboard\/operation-admin\/operations\/[^/]+$/.test(normalized)
    || /^\/dashboard\/team-leader\/operations\/[^/]+$/.test(normalized)
    || /^\/dashboard\/operations\/[^/]+$/.test(normalized)
  );
}
