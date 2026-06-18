import { useCallback, useEffect, useState } from 'react';
import type { OperationDetails, OperationWithLedTeamsDetails } from '../../api/types';
import { usePageTitle } from '../../layouts/PageTitleContext';
import { fetchOperationById } from './fetchOperationById';
import type { OperationScope } from './operationPaths';

export function useOperationDetail(scope: OperationScope, operationId: string | undefined) {
  const { setTitle } = usePageTitle();
  const [operation, setOperation] = useState<OperationDetails | OperationWithLedTeamsDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const reload = useCallback(async () => {
    if (!operationId) {
      setNotFound(true);
      setOperation(null);
      setTitle(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    const result = await fetchOperationById(scope, operationId);
    if (!result) {
      setNotFound(true);
      setOperation(null);
      setTitle(null);
    } else {
      setNotFound(false);
      setOperation(result);
      setTitle(result.name);
    }
    setLoading(false);
  }, [operationId, scope, setTitle]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => () => setTitle(null), [setTitle]);

  return { operation, loading, notFound, reload };
}
