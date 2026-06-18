import { detailPath, type OperationScope } from '../operations/operationPaths';

export type TeamScope = 'global-admin' | 'operation-admin';

export function teamDetailPath(scope: TeamScope, operationId: string, teamId: string): string {
  return `${detailPath(scope, operationId)}/teams/${encodeURIComponent(teamId)}`;
}

export function isTeamDetailPath(pathname: string): boolean {
  const normalized = pathname.replace(/\/$/, '').toLowerCase();
  return (
    /^\/dashboard\/admin\/operations\/[^/]+\/teams\/[^/]+$/.test(normalized)
    || /^\/dashboard\/operation-admin\/operations\/[^/]+\/teams\/[^/]+$/.test(normalized)
  );
}

export function teamPanelScope(scope: TeamScope): 'full' | 'operation-admin' {
  return scope === 'global-admin' ? 'full' : 'operation-admin';
}

export function toTeamScope(scope: OperationScope): TeamScope | null {
  if (scope === 'global-admin' || scope === 'operation-admin') return scope;
  return null;
}
