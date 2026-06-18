import { searchAdministratorOperations } from '../../api/administrator/operations';
import { searchOperationAdministratorOperations } from '../../api/operationAdministrator/operations';
import { searchOperatorOperations } from '../../api/operator/operations';
import { searchTeamLeaderLedTeams } from '../../api/teamLeader/operations';
import type { OperationDetails, OperationWithLedTeamsDetails, TeamDetails } from '../../api/types';
import type { OperationScope } from './operationPaths';

type ApiResult<T> = { ok: true; data: T | null } | { ok: false; error: string };

type OperatorSearchRow = OperationDetails & { team?: TeamDetails };

function teamsFromRow(row: OperatorSearchRow): TeamDetails[] {
  if (Array.isArray(row.teams) && row.teams.length > 0) return row.teams;
  if (row.team) return [row.team];
  return [];
}

export function mergeOperatorSearchRows(rows: OperatorSearchRow[]): OperationDetails | null {
  if (rows.length === 0) return null;
  const first = rows[0]!;
  const teams = rows.flatMap(teamsFromRow);
  const uniqueTeams = [...new Map(teams.map((team) => [team.id, team])).values()];
  return {
    id: first.id,
    name: first.name,
    description: first.description,
    administrators: first.administrators ?? [],
    teams: uniqueTeams,
    createdAt: first.createdAt,
    updatedAt: first.updatedAt,
  };
}

export function dedupeOperatorListItems(items: OperationDetails[]): OperationDetails[] {
  const byId = new Map<string, OperationDetails>();
  for (const item of items) {
    const row = item as OperatorSearchRow;
    const teams = teamsFromRow(row);
    const existing = byId.get(item.id);
    if (!existing) {
      byId.set(item.id, { ...item, teams });
      continue;
    }
    const mergedTeams = [...existing.teams, ...teams];
    existing.teams = [...new Map(mergedTeams.map((team) => [team.id, team])).values()];
  }
  return [...byId.values()];
}

async function searchExactById<T extends { id: string }>(
  searchFn: (payload: { limit: number; offset: number; keyword: string }) => Promise<ApiResult<{ items?: T[] }>>,
  operationId: string,
): Promise<T | null> {
  const result = await searchFn({ limit: 50, offset: 0, keyword: operationId });
  if (!result.ok) return null;
  const matches = (result.data?.items ?? []).filter((item) => item.id === operationId);
  return matches[0] ?? null;
}

export async function fetchOperationById(
  scope: OperationScope,
  operationId: string,
): Promise<OperationDetails | OperationWithLedTeamsDetails | null> {
  if (scope === 'global-admin') {
    return searchExactById(searchAdministratorOperations, operationId);
  }
  if (scope === 'operation-admin') {
    return searchExactById(searchOperationAdministratorOperations, operationId);
  }
  if (scope === 'team-leader') {
    return searchExactById(searchTeamLeaderLedTeams, operationId);
  }

  const result = await searchOperatorOperations({
    limit: 50,
    offset: 0,
    keyword: operationId,
  });
  if (!result.ok) return null;
  const rows = (result.data?.items ?? []).filter((item) => item.id === operationId) as OperatorSearchRow[];
  return mergeOperatorSearchRows(rows);
}
