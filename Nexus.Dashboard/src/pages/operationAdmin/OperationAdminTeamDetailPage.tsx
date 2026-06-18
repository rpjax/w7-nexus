import { useNavigate, useParams } from 'react-router-dom';
import { AdminTeamPanel } from '../../components/admin/AdminTeamPanel';
import { TeamDetailShell } from '../../features/teams/TeamDetailShell';
import { detailPath } from '../../features/operations/operationPaths';
import { useTeamDetail } from '../../features/teams/useTeamDetail';
import { useTeamScopeActions } from '../../features/teams/useTeamScopeActions';

export function OperationAdminTeamDetailPage() {
  const { operationId, teamId } = useParams<{ operationId: string; teamId: string }>();
  const navigate = useNavigate();
  const { operation, team, loading, notFound, reload } = useTeamDetail(
    'operation-admin',
    operationId,
    teamId,
  );

  const { teamActions, modals, panelScope } = useTeamScopeActions({
    scope: 'operation-admin',
    team,
    onMutated: reload,
    onTeamDeleted: () => {
      if (operationId) navigate(detailPath('operation-admin', operationId));
    },
  });

  return (
    <>
      <TeamDetailShell
        scope="operation-admin"
        operationId={operationId ?? ''}
        operationName={operation?.name}
        loading={loading}
        notFound={notFound}
      >
        {team ? (
          <AdminTeamPanel
            team={team}
            scope={panelScope}
            actions={teamActions}
            variant="detail"
          />
        ) : null}
      </TeamDetailShell>
      {modals}
    </>
  );
}
