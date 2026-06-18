import type { TeamDetails } from '../../api/types';
import { AdminGatewaySection } from './AdminGatewaySection';
import type { AdminTeamPanelActions } from './adminTeamTypes';

type AdminTeamGatewaySectionProps = {
  team: TeamDetails;
  actions: AdminTeamPanelActions;
  showHeader?: boolean;
};

export function AdminTeamGatewaySection({ team, actions, showHeader = true }: AdminTeamGatewaySectionProps) {
  return (
    <AdminGatewaySection
      scope={team}
      actions={actions}
      variant="team"
      showHeader={showHeader}
    />
  );
}

export { AdminGatewaySection } from './AdminGatewaySection';
export type { AdminGatewayActions, GatewayScopeDetails } from './adminGatewayTypes';
