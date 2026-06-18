import { useParams } from 'react-router-dom';
import type { OperationDetails } from '../../api/types';
import { OperationDetailShell } from '../../features/operations/OperationDetailShell';
import { OperatorOperationDetail } from '../../features/operations/OperatorOperationDetail';
import { listPath } from '../../features/operations/operationPaths';
import { useOperationDetail } from '../../features/operations/useOperationDetail';

export function OperatorOperationDetailPage() {
  const { operationId } = useParams<{ operationId: string }>();
  const { operation, loading, notFound } = useOperationDetail('operator', operationId);

  return (
    <OperationDetailShell
      listPath={listPath('operator')}
      loading={loading}
      notFound={notFound}
    >
      {operation ? (
        <OperatorOperationDetail operation={operation as OperationDetails} />
      ) : null}
    </OperationDetailShell>
  );
}
