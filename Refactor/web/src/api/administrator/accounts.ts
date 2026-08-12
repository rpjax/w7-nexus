import { apiClient } from '@/api/client';
import type { AccountDetails } from '@/auth/types';

type CreateAccountResponse = {
  account: AccountDetails;
};

type SearchAccountsResponse = {
  offset: number;
  limit: number;
  total: number;
  items: AccountDetails[];
};

type GetAccountResponse = {
  account: AccountDetails;
};

type AccountMutationResponse = {
  account: AccountDetails;
};

export type CreateAccountType = 'usuario' | 'admin';

export async function searchAdministratorAccounts(params: {
  limit: number;
  offset: number;
  keyword?: string;
  status?: string;
  role?: string;
}) {
  return apiClient.post<SearchAccountsResponse>('/api/accounts/administrator/search', {
    limit: params.limit,
    offset: params.offset,
    keyword: params.keyword,
    status: params.status,
    role: params.role,
  }, { fallbackError: 'Não foi possível buscar contas.' });
}

export async function getAdministratorAccount(accountId: string) {
  return apiClient.get<GetAccountResponse>(`/api/accounts/administrator/${accountId}`, {
    fallbackError: 'Não foi possível carregar a conta.',
  });
}

export async function createAdministratorAccount(params: {
  username: string;
  password: string;
  accountType: CreateAccountType;
  masterKey?: string;
}) {
  return apiClient.post<CreateAccountResponse>('/api/accounts/administrator', {
    username: params.username,
    password: params.password,
    accountType: params.accountType,
  }, {
    fallbackError: 'Não foi possível criar a conta.',
    headers: params.accountType === 'admin' && params.masterKey
      ? { 'X-Administrator-Create-Token': params.masterKey }
      : undefined,
  });
}

export async function grantAdministratorAccountRole(accountId: string, role: string) {
  return apiClient.post('/api/accounts/administrator/roles', {
    accountId,
    role,
  }, { fallbackError: 'Não foi possível conceder a função.' });
}

export async function revokeAdministratorAccountRole(accountId: string, role: string) {
  return apiClient.delete('/api/accounts/administrator/roles', {
    accountId,
    role,
  }, { fallbackError: 'Não foi possível remover a função.' });
}

export async function grantAdministratorAccountPermission(accountId: string, permission: string) {
  return apiClient.post('/api/accounts/administrator/permissions', {
    accountId,
    permission,
  }, { fallbackError: 'Não foi possível conceder a permissão.' });
}

export async function revokeAdministratorAccountPermission(accountId: string, permission: string) {
  return apiClient.delete('/api/accounts/administrator/permissions', {
    accountId,
    permission,
  }, { fallbackError: 'Não foi possível remover a permissão.' });
}

export async function disableAdministratorAccount(accountId: string) {
  return apiClient.post<AccountMutationResponse>('/api/accounts/administrator/disable', {
    accountId,
  }, { fallbackError: 'Não foi possível desabilitar a conta.' });
}

export async function enableAdministratorAccount(accountId: string) {
  return apiClient.post<AccountMutationResponse>('/api/accounts/administrator/enable', {
    accountId,
  }, { fallbackError: 'Não foi possível reabilitar a conta.' });
}

export async function resetAdministratorAccountPassword(accountId: string, newPassword: string) {
  return apiClient.post<AccountMutationResponse>('/api/accounts/administrator/password', {
    accountId,
    newPassword,
  }, { fallbackError: 'Não foi possível redefinir a senha.' });
}
