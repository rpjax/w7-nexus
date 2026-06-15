import type { GatewaySelectionStrategy, OperatorDetails } from '../../api/types';

export type AdminTeamPanelActions = {
  busy: boolean;
  onDeleteTeam: (teamId: string) => void;
  onAssignLeader: (teamId: string) => void;
  onUnassignLeader: (teamId: string) => void;
  onAssignOperator: (teamId: string) => void;
  onUnassignOperator: (teamId: string, operatorId: string) => void;
  onEditProfitShare: (teamId: string, operator: OperatorDetails) => void;
  onGatewayStrategyChange: (teamId: string, strategy: GatewaySelectionStrategy) => void;
  onAssignStrawMan: (teamId: string) => void;
  onUnassignStrawMan: (teamId: string, accountId: string) => void;
  onAssignGatewayCredential: (teamId: string) => void;
  onUnassignGatewayCredential: (teamId: string, credentialId: string) => void;
  onAssignGatewayGroup: (teamId: string, groupId: string) => void;
  onUnassignGatewayGroup: (teamId: string, groupId: string) => void;
};
