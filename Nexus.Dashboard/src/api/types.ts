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
  username?: string | null;
  role?: string | null;
  percentage: number;
  amount: number;
};

export type PaymentRow = {
  id: string;
  operationId: string;
  operationName?: string | null;
  gateway: string;
  gatewayTransactionId: string;
  amount: number;
  splits?: PaymentSplitRow[];
  status: string;
  settlementStatus: string;
  distributionStatus: string;
  operatorId?: string | null;
  operatorUsername?: string | null;
  strawManId: string;
  strawManUsername?: string | null;
  createdAt: string;
  paidAt?: string | null;
  refundedAt?: string | null;
  killedAt?: string | null;
  killReason?: string | null;
  withdrawnAt?: string | null;
  distributedAt?: string | null;
};

export type TransferType = 'Withdrawal' | 'Movement' | 'Payout';

export type OnrampingMethod = 'Pix' | 'GiftCard' | 'CreditDebitCard';

export type TransferEndpointBankAccount = {
  bankAccountId: string;
  ownerId: string;
};

export type TransferEndpointCryptoWallet = {
  cryptoWalletId: string;
  ownerId: string;
};

export type TransferProof = {
  pixTransactionId?: string | null;
  pixAuthenticationCode?: string | null;
  cryptoTransactionId?: string | null;
};

export type TransferRow = {
  id: string;
  type: TransferType;
  onrampingMethod?: OnrampingMethod | null;
  proof?: TransferProof | null;
  originType?: string | null;
  originBankAccount?: TransferEndpointBankAccount | null;
  originCryptoWallet?: TransferEndpointCryptoWallet | null;
  destinationType?: string | null;
  destinationBankAccount?: TransferEndpointBankAccount | null;
  destinationCryptoWallet?: TransferEndpointCryptoWallet | null;
  sourceAmount: number;
  producedAmount?: number | null;
  producedAsset?: string | null;
  producedChain?: string | null;
  paymentIds: string[];
  sourceBalanceId?: string | null;
  strawManId: string;
  createdAt: string;
};

export type AccountSummaryRow = {
  id: string;
  username: string;
};

export type TransferEndpoint = {
  kind: string;
  id?: string | null;
  displayName: string;
  label?: string | null;
  username?: string | null;
  bankSummary?: string | null;
  cryptoSummary?: string | null;
};

export type BalanceEffectRow = {
  direction: 'Credit' | 'Debit';
  balanceId: string;
  amount: number;
  chain?: string | null;
  asset?: string | null;
  currency: string;
  account: TransferEndpoint;
};

export type PaymentSummaryRow = {
  id: string;
  amount: number;
  status: string;
  settlementStatus: string;
  gateway: string;
  gatewayTransactionId: string;
  operatorUsername?: string | null;
  createdAt: string;
};

export type TransferEnrichedRow = {
  id: string;
  type: TransferType;
  onrampingMethod?: string | null;
  proof?: TransferProof | null;
  sourceAmount: number;
  producedAmount?: number | null;
  producedAsset?: string | null;
  producedChain?: string | null;
  paymentIds: string[];
  sourceBalanceId?: string | null;
  createdAt: string;
  source?: TransferEndpoint | null;
  destination?: TransferEndpoint | null;
  strawMan: AccountSummaryRow;
};

export type TransferTimelineStep = {
  transferId: string;
  type: TransferType;
  createdAt: string;
  isFocus: boolean;
  isCurrent: boolean;
  title: string;
  summary: string;
  transfer: TransferEnrichedRow;
  balanceEffects: BalanceEffectRow[];
  payments: PaymentSummaryRow[];
};

export type ActiveBalanceRow = {
  balanceId: string;
  transferId: string;
  amount: number;
  chain?: string | null;
  asset?: string | null;
  currency: string;
  account: TransferEndpoint;
  canMove: boolean;
  canPayout: boolean;
};

export type TransferTimelineDetails = {
  rootTransferId: string;
  focusTransferId: string;
  strawMan?: AccountSummaryRow | null;
  steps: TransferTimelineStep[];
  activeBalances: ActiveBalanceRow[];
};

export type SearchTransfersRequest = {
  limit: number;
  offset: number;
  strawManId?: string | null;
  type?: TransferType | null;
};

export type BankAccountType = 'Checking' | 'Savings';

export type BalanceOrigin = {
  operationId: string;
  operatorId?: string | null;
};

export type BankBalanceRow = {
  id: string;
  amountBrl: number;
  transferId: string;
  createdAt: string;
  splits: unknown[];
  origin: BalanceOrigin;
};

export type CryptoBalanceRow = {
  id: string;
  chain: string;
  asset: string;
  amount: number;
  transferId: string;
  createdAt: string;
  splits: unknown[];
  origin: BalanceOrigin;
};

export type CryptoBalanceByChainAsset = {
  chain: string;
  asset: string;
  totalAmount: number;
};

export type CryptoWalletAddressRow = {
  namespace: string;
  address: string;
  memo?: string | null;
};

export type BankAccountRow = {
  id: string;
  ownerId: string;
  bank: string;
  agency: string;
  accountNumber: string;
  accountDigit?: string | null;
  accountType: BankAccountType;
  label?: string | null;
  totalBalanceBrl?: number;
  balances?: BankBalanceRow[];
  createdAt: string;
  updatedAt: string;
};

export type CryptoWalletRow = {
  id: string;
  ownerId: string;
  addresses: CryptoWalletAddressRow[];
  label?: string | null;
  balancesByChainAsset?: CryptoBalanceByChainAsset[];
  balances?: CryptoBalanceRow[];
  createdAt: string;
  updatedAt: string;
};

export type SearchScopedAccountsRequest = {
  limit: number;
  offset: number;
  ownerId?: string | null;
};

export type StrawManSettings = {
  strawManId: string;
  movementFeePercentage: number;
  updatedAt?: string | null;
  updatedByAdminId?: string | null;
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
