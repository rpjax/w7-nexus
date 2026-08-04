import { useQuery } from '@tanstack/react-query';
import type { OperationDetails, OperationWithLedTeamsDetails } from '@/api/types';
import { fetchOperationById } from './fetchOperationById';
import type { OperationScope } from './operationPaths';

export function useOperationDetail(scope: OperationScope, operationId: string | undefined) {
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['operation-detail', scope, operationId],
    enabled: Boolean(operationId),
    queryFn: async () => {
      const result = await fetchOperationById(scope, operationId!);
      return result ?? null;
    },
  });

  const operation = (data ?? null) as OperationDetails | OperationWithLedTeamsDetails | null;

  return {
    operation,
    loading: isLoading,
    notFound: !operationId || (!isLoading && !data),
    reload: () => void refetch(),
  };
}
