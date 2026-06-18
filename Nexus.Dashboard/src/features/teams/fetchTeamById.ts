import type { OperationDetails, TeamDetails } from '../../api/types';
import { fetchOperationById } from '../operations/fetchOperationById';
import type { TeamScope } from './teamPaths';

export type TeamDetailResult = {
  operation: OperationDetails;
  team: TeamDetails;
};

export async function fetchTeamById(
  scope: TeamScope,
  operationId: string,
  teamId: string,
): Promise<TeamDetailResult | null> {
  const operation = await fetchOperationById(scope, operationId);
  if (!operation || !('teams' in operation)) return null;
  const team = (operation.teams ?? []).find((item) => item.id === teamId) ?? null;
  if (!team) return null;
  return { operation: operation as OperationDetails, team };
}
