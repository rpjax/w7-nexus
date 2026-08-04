import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { searchAdministratorAccounts } from '../../api/administrator/accounts';
import { searchAdministratorOperations } from '../../api/administrator/operations';
import { isAdministrator } from '../../auth/roles';
import { useAuth } from '../../auth/AuthContext';

export function useOlxOperationLabels(
  rows: Array<{ operationId: string }>,
  extraLabels: Record<string, string> = {},
) {
  const { user } = useAuth();
  const adminView = isAdministrator(user);

  const operationIds = useMemo(
    () => [...new Set(rows.map((row) => row.operationId).filter(Boolean))].sort().join(','),
    [rows],
  );

  const { data: labels = {} } = useQuery({
    queryKey: ['olx-operation-labels', operationIds],
    enabled: adminView && Boolean(operationIds),
    queryFn: async () => {
      const ids = operationIds.split(',');
      const result = await searchAdministratorOperations({ limit: 200, offset: 0, keyword: null });
      if (!result.ok) return {};

      const next: Record<string, string> = {};
      for (const operation of result.data?.items ?? []) {
        if (ids.includes(operation.id)) next[operation.id] = operation.name;
      }
      return next;
    },
  });

  return useMemo(
    () => ({ ...labels, ...extraLabels }),
    [extraLabels, labels],
  );
}

export function resolveOlxOperationLabel(
  operationId: string,
  labels: Record<string, string>,
): string {
  return labels[operationId]?.trim() || 'Operação vinculada';
}

export function useOlxOperatorLabels(
  rows: Array<{ operatorId?: string | null }>,
  extraLabels: Record<string, string> = {},
) {
  const { user } = useAuth();
  const adminView = isAdministrator(user);

  const operatorIds = useMemo(
    () => [...new Set(rows.map((row) => row.operatorId).filter(Boolean) as string[])].sort().join(','),
    [rows],
  );

  const { data: labels = {} } = useQuery({
    queryKey: ['olx-operator-labels', operatorIds],
    enabled: adminView && Boolean(operatorIds),
    queryFn: async () => {
      const ids = operatorIds.split(',');
      const result = await searchAdministratorAccounts({ limit: 200, offset: 0, keyword: null });
      if (!result.ok) return {};

      const next: Record<string, string> = {};
      for (const account of result.data?.items ?? []) {
        if (ids.includes(account.id) && account.roles.includes('OlxOperator')) {
          next[account.id] = account.username;
        }
      }
      return next;
    },
  });

  return useMemo(
    () => ({ ...labels, ...extraLabels }),
    [extraLabels, labels],
  );
}
