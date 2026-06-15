import { apiClient } from '../client';
import type { AccountRow, SearchRequest, SearchResponse } from '../types';

export async function searchAdministratorAccounts(payload: SearchRequest) {
  return apiClient.post<SearchResponse<AccountRow>>('/api/administrator/accounts/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as contas.' });
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
