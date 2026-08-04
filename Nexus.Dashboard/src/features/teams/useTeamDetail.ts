import { useQuery } from '@tanstack/react-query';
import type { OperationDetails, TeamDetails } from '@/api/types';
import { fetchTeamById } from './fetchTeamById';
import type { TeamScope } from './teamPaths';

export function useTeamDetail(
  scope: TeamScope,
  operationId: string | undefined,
  teamId: string | undefined,
) {
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['team-detail', scope, operationId, teamId],
    enabled: Boolean(operationId && teamId),
    queryFn: async () => {
      const result = await fetchTeamById(scope, operationId!, teamId!);
      return result ?? null;
    },
  });

  return {
    operation: (data?.operation ?? null) as OperationDetails | null,
    team: (data?.team ?? null) as TeamDetails | null,
    loading: isLoading,
    notFound: !operationId || !teamId || (!isLoading && !data),
    reload: () => void refetch(),
  };
}
