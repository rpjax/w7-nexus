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

export type OperationDetails = {
  id: string;
  name: string;
  description?: string | null;
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
