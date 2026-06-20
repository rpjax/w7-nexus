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
  gatewaySelectionStrategy?: GatewaySelectionStrategy;
  strawMen?: TeamAccountDetails[];
  gatewayCredentials?: TeamGatewayCredentialDetails[];
  gatewayCredentialsGroups?: TeamGatewayGroupDetails[];
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

export type PaymentSplitRow = {
  accountId: string;
  percentage: number;
  amount: number;
};

export type PaymentRow = {
  id: string;
  operationId: string;
  teamId?: string | null;
  gateway: string;
  gatewayTransactionId: string;
  amount: number;
  splits?: PaymentSplitRow[];
  status: string;
  settlementStatus: string;
  operatorAccountId?: string | null;
  strawManAccountId?: string | null;
  createdAt: string;
  paidAt?: string | null;
  refundedAt?: string | null;
  diedAt?: string | null;
  deathReason?: string | null;
  withdrawnAt?: string | null;
};

export type WithdrawalType = 'Pix' | 'Crypto';

export type BankAccountType = 'Checking' | 'Savings';

export type BankAccountRow = {
  id: string;
  strawManAccountId: string;
  bank: string;
  bankName: string;
  bankCode: string;
  bankIspb: string;
  agency: string;
  accountNumber: string;
  accountDigit?: string | null;
  accountType: BankAccountType;
  pixKey?: string | null;
  label?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CryptoWalletRow = {
  id: string;
  strawManAccountId: string;
  chain: string;
  chainCaip2: string;
  asset: string;
  address: string;
  memo?: string | null;
  label?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type WithdrawalPixProof = {
  transactionId?: string | null;
  authenticationCode?: string | null;
};

export type WithdrawalCryptoProof = {
  transactionId?: string | null;
};

export type WithdrawalRow = {
  id: string;
  operationId: string;
  type: WithdrawalType;
  strawManAccountId: string;
  bankAccountId?: string | null;
  cryptoWalletId?: string | null;
  paymentIds: string[];
  costDescription?: string | null;
  costAmount: number;
  pixProof?: WithdrawalPixProof | null;
  cryptoProof?: WithdrawalCryptoProof | null;
  paymentsTotalAmount: number;
  netAmount: number;
  createdAt: string;
};

export type SearchWithdrawalsRequest = {
  limit: number;
  offset: number;
  operationId?: string | null;
  strawManAccountId?: string | null;
  type?: WithdrawalType | null;
};

export type SearchScopedAccountsRequest = {
  limit: number;
  offset: number;
  strawManAccountId?: string | null;
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
