import { useNavigate, useParams } from 'react-router-dom';
import { AdminTeamPanel } from '../../components/admin/AdminTeamPanel';
import { TeamDetailShell } from '../../features/teams/TeamDetailShell';
import { detailPath } from '../../features/operations/operationPaths';
import { useTeamDetail } from '../../features/teams/useTeamDetail';
import { useTeamScopeActions } from '../../features/teams/useTeamScopeActions';

export function AdminTeamDetailPage() {
  const { operationId, teamId } = useParams<{ operationId: string; teamId: string }>();
  const navigate = useNavigate();
  const { operation, team, loading, notFound, reload } = useTeamDetail(
    'global-admin',
    operationId,
    teamId,
  );

  const { teamActions, modals, panelScope } = useTeamScopeActions({
    scope: 'global-admin',
    team,
    onMutated: reload,
    onTeamDeleted: () => {
      if (operationId) navigate(detailPath('global-admin', operationId));
    },
  });

  return (
    <>
      <TeamDetailShell
        scope="global-admin"
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
