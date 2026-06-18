import { useCallback, useEffect, useState } from 'react';
import type { OperationDetails, TeamDetails } from '../../api/types';
import { usePageTitle } from '../../layouts/PageTitleContext';
import { fetchTeamById } from './fetchTeamById';
import type { TeamScope } from './teamPaths';

export function useTeamDetail(
  scope: TeamScope,
  operationId: string | undefined,
  teamId: string | undefined,
) {
  const { setTitle } = usePageTitle();
  const [operation, setOperation] = useState<OperationDetails | null>(null);
  const [team, setTeam] = useState<TeamDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const reload = useCallback(async () => {
    if (!operationId || !teamId) {
      setNotFound(true);
      setOperation(null);
      setTeam(null);
      setTitle(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    const result = await fetchTeamById(scope, operationId, teamId);
    if (!result) {
      setNotFound(true);
      setOperation(null);
      setTeam(null);
      setTitle(null);
    } else {
      setNotFound(false);
      setOperation(result.operation);
      setTeam(result.team);
      setTitle(result.team.name);
    }
    setLoading(false);
  }, [operationId, scope, setTitle, teamId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => () => setTitle(null), [setTitle]);

  return { operation, team, loading, notFound, reload };
}
