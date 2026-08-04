import { useParams } from 'react-router-dom';
import type { OperationDetails } from '@/api/types';
import { OperationDetailShell } from '@/features/operations/OperationDetailShell';
import { OperatorOperationDetail } from '@/features/operations/OperatorOperationDetail';
import { useOperationDetail } from '@/features/operations/useOperationDetail';

export function OperatorOperationDetailPage() {
  const { operationId } = useParams<{ operationId: string }>();
  const { operation, loading, notFound } = useOperationDetail('operator', operationId);

  return (
      <OperationDetailShell
        scope="operator"
        operationName={operation?.name}
        loading={loading}
        notFound={notFound}
      >
      {operation ? (
        <OperatorOperationDetail operation={operation as OperationDetails} />
      ) : null}
    </OperationDetailShell>
  );
}
