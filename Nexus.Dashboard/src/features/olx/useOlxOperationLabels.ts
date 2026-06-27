import { useEffect, useMemo, useState } from 'react';
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
  const [labels, setLabels] = useState<Record<string, string>>({});

  const operationIds = useMemo(
    () => [...new Set(rows.map((row) => row.operationId).filter(Boolean))].sort().join(','),
    [rows],
  );

  useEffect(() => {
    const ids = operationIds ? operationIds.split(',') : [];
    if (!adminView || ids.length === 0) {
      setLabels({});
      return;
    }

    let cancelled = false;

    void (async () => {
      const result = await searchAdministratorOperations({ limit: 200, offset: 0, keyword: null });
      if (!result.ok || cancelled) return;

      const next: Record<string, string> = {};
      for (const operation of result.data?.items ?? []) {
        if (ids.includes(operation.id)) next[operation.id] = operation.name;
      }
      setLabels(next);
    })();

    return () => {
      cancelled = true;
    };
  }, [adminView, operationIds]);

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
  const [labels, setLabels] = useState<Record<string, string>>({});

  const operatorIds = useMemo(
    () => [...new Set(rows.map((row) => row.operatorId).filter(Boolean) as string[])].sort().join(','),
    [rows],
  );

  useEffect(() => {
    const ids = operatorIds ? operatorIds.split(',') : [];
    if (!adminView || ids.length === 0) {
      setLabels({});
      return;
    }

    let cancelled = false;

    void (async () => {
      const result = await searchAdministratorAccounts({ limit: 200, offset: 0, keyword: null });
      if (!result.ok || cancelled) return;

      const next: Record<string, string> = {};
      for (const account of result.data?.items ?? []) {
        if (ids.includes(account.id) && account.roles.includes('OlxOperator')) {
          next[account.id] = account.username;
        }
      }
      setLabels(next);
    })();

    return () => {
      cancelled = true;
    };
  }, [adminView, operatorIds]);

  return useMemo(
    () => ({ ...labels, ...extraLabels }),
    [extraLabels, labels],
  );
}
