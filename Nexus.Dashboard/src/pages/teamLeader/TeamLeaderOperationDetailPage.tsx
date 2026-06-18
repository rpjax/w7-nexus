import { useParams } from 'react-router-dom';
import { AdminOperationCard } from '../../components/admin/AdminOperationCard';
import { OperationDetailShell } from '../../features/operations/OperationDetailShell';
import { listPath } from '../../features/operations/operationPaths';
import { useOperationDetail } from '../../features/operations/useOperationDetail';
import { useOperationScopeActions } from '../../features/operations/useOperationScopeActions';

export function TeamLeaderOperationDetailPage() {
  const { operationId } = useParams<{ operationId: string }>();
  const { operation, loading, notFound, reload } = useOperationDetail('team-leader', operationId);

  const { cardActions, modals, cardScope } = useOperationScopeActions({
    scope: 'team-leader',
    mode: 'detail',
    operation,
    onMutated: reload,
  });

  return (
    <>
      <OperationDetailShell
        listPath={listPath('team-leader')}
        loading={loading}
        notFound={notFound}
      >
        {operation ? (
          <AdminOperationCard operation={operation} scope={cardScope} actions={cardActions} />
        ) : null}
      </OperationDetailShell>
      {modals}
    </>
  );
}
