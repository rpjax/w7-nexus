import { apiClient } from '../client';
import type { AccountRow, SearchRequest, SearchResponse } from '../types';

export async function createAdministratorAccount(username: string, password: string) {
  return apiClient.post<void>('/api/accounts/administrator', {
    Username: username,
    Password: password,
  }, { fallbackError: 'Não foi possível criar a conta. Verifique os dados informados.' });
}

export async function searchAdministratorAccounts(payload: SearchRequest) {
  return apiClient.post<SearchResponse<AccountRow>>('/api/accounts/administrator/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as contas.' });
}

export async function grantAdministratorAccountRole(accountId: string, role: string) {
  return apiClient.post<void>('/api/accounts/administrator/roles', {
    AccountId: accountId,
    Role: role,
  }, { fallbackError: 'Não foi possível conceder a função.' });
}

export async function revokeAdministratorAccountRole(accountId: string, role: string) {
  return apiClient.deleteWithBody<void>('/api/accounts/administrator/roles', {
    AccountId: accountId,
    Role: role,
  }, { fallbackError: 'Não foi possível remover a função.' });
}

export async function grantAdministratorAccountPermission(accountId: string, permission: string) {
  return apiClient.post<void>('/api/accounts/administrator/permissions', {
    AccountId: accountId,
    Permission: permission,
  }, { fallbackError: 'Não foi possível conceder a permissão.' });
}

export async function revokeAdministratorAccountPermission(accountId: string, permission: string) {
  return apiClient.deleteWithBody<void>('/api/accounts/administrator/permissions', {
    AccountId: accountId,
    Permission: permission,
  }, { fallbackError: 'Não foi possível remover a permissão.' });
}

export async function searchAdministratorAccountsForPicker(payload: SearchRequest) {
  const result = await searchAdministratorAccounts(payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: (result.data?.items ?? []).map((row) => ({
        id: row.id,
        username: row.username,
        roles: row.roles,
      })),
    },
  };
}

export async function searchAdministratorStrawMenForPicker(payload: SearchRequest) {
  const result = await searchAdministratorAccountsForPicker(payload);
  if (!result.ok) return result;

  const items = result.data.items.filter((row) => row.roles.includes('StrawMan'));
  return {
    ok: true as const,
    data: {
      total: items.length,
      items,
    },
  };
}

export async function searchAdministratorOlxOperatorsForPicker(payload: SearchRequest) {
  const result = await searchAdministratorAccountsForPicker(payload);
  if (!result.ok) return result;

  const items = result.data.items.filter((row) => row.roles.includes('OlxOperator'));
  return {
    ok: true as const,
    data: {
      total: items.length,
      items,
    },
  };
}
