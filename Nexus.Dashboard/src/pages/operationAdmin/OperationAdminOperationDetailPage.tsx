import { useNavigate, useParams } from 'react-router-dom';
import { AdminOperationCard } from '../../components/admin/AdminOperationCard';
import { OperationDetailShell } from '../../features/operations/OperationDetailShell';
import { listPath } from '../../features/operations/operationPaths';
import { useOperationDetail } from '../../features/operations/useOperationDetail';
import { useOperationScopeActions } from '../../features/operations/useOperationScopeActions';
import { teamDetailPath } from '../../features/teams/teamPaths';

export function OperationAdminOperationDetailPage() {
  const { operationId } = useParams<{ operationId: string }>();
  const navigate = useNavigate();
  const { operation, loading, notFound, reload } = useOperationDetail('operation-admin', operationId);

  const { cardActions, modals, cardScope } = useOperationScopeActions({
    scope: 'operation-admin',
    mode: 'detail',
    operation,
    onMutated: reload,
    onTeamCreated: (teamId) => {
      if (operationId) navigate(teamDetailPath('operation-admin', operationId, teamId));
    },
  });

  return (
    <>
      <OperationDetailShell
        listPath={listPath('operation-admin')}
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
