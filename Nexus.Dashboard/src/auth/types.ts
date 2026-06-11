export type AuthenticationTokens = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tokenType: string;
};

export type AuthUser = {
  accountId: string;
  username: string;
  roles: string[];
  permissions: string[];
};

export type StoredSession = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
};
