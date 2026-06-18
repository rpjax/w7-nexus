export type SearchResponse<T> = {
  total: number;
  items: T[];
};

export type SearchRequest = {
  limit: number;
  offset: number;
  keyword?: string | null;
  enabledOnly?: boolean;
};

export type AccountRow = {
  id: string;
  username: string;
  roles: string[];
  permissions: string[];
  createdAt: string;
  lastUpdatedAt: string;
};

export type OperationRow = {
  id: string;
  name: string;
  description?: string | null;
  operatorIds: string[];
  strawManIds: string[];
  manuallySetGatewayCredentials: boolean;
  gatewayCredentialsIds: string[];
  updatedAt: string;
};

export type OperationAdministratorDetails = {
  accountId: string;
  username: string;
};

export type TeamLeaderDetails = {
  accountId: string;
  username: string;
};

export type ProfitSplitDetails = {
  accountId: string;
  username: string;
  percentage: number;
};

export type ProfitShareRuleDetails = {
  cuts: ProfitSplitDetails[];
};

export type OperatorDetails = {
  accountId: string;
  username: string;
  profitShareRule: ProfitShareRuleDetails;
};

export type TeamAccountDetails = {
  accountId: string;
  username: string;
};

export type GatewaySelectionStrategy = 'PerStrawman' | 'PerGroup' | 'Manual';

export const GATEWAY_SELECTION_STRATEGY_VALUE: Record<GatewaySelectionStrategy, number> = {
  PerStrawman: 0,
  PerGroup: 1,
  Manual: 2,
};

export type ProfitShareCutInput = {
  accountId: string;
  percentage: number;
};

export type TeamGatewayCredentialDetails = {
  id: string;
  name: string;
  gateway: string;
};

export type TeamGatewayGroupDetails = {
  id: string;
  name: string;
  credentialCount: number;
};

export type TeamDetails = {
  id: string;
  operationId: string;
  name: string;
  teamLeader?: TeamLeaderDetails | null;
  operators: OperatorDetails[];
  gatewaySelectionStrategy?: GatewaySelectionStrategy;
  strawMen?: TeamAccountDetails[];
  gatewayCredentials?: TeamGatewayCredentialDetails[];
  gatewayCredentialsGroups?: TeamGatewayGroupDetails[];
};

export type OperationDetails = {
  id: string;
  name: string;
  description?: string | null;
  administrators: OperationAdministratorDetails[];
  teams: TeamDetails[];
  createdAt: string;
  updatedAt: string;
};

/** Operação com apenas as equipes lideradas pelo usuário (Team Leader). */
export type OperationWithLedTeamsDetails = {
  id: string;
  name: string;
  description?: string | null;
  teams: TeamDetails[];
  createdAt: string;
  updatedAt: string;
};

export type PaymentRow = {
  id: string;
  operationId: string;
  gateway: string;
  gatewayTransactionId: string;
  amount: number;
  status: string;
  operatorAccountId?: string | null;
  strawManAccountId?: string | null;
  createdAt: string;
};

export type GatewayPixResult = {
  id: string;
  code: string;
};

export type TokenCredential = {
  id: string;
  name: string;
  token: string;
  strawManId?: string | null;
  enabled: boolean;
};

export type KeyPairCredential = {
  id: string;
  name: string;
  publicKey: string;
  secretKey: string;
  strawManId?: string | null;
  enabled: boolean;
};

export type GatewayPrefix = 'frendz' | 'sigilopay' | 'wintech';

export type AccountPickerRow = {
  id: string;
  username: string;
  roles?: string[];
};

export type OperationPickerRow = {
  id: string;
  name: string;
};

export type GatewayCredentialPickerRow = {
  id: string;
  name: string;
  gatewayLabel: string;
};
