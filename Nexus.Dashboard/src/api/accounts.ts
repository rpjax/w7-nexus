import { apiClient } from './client';
import type { AccountPickerRow, AccountRow, SearchRequest, SearchResponse } from './types';

export async function searchAccounts(payload: SearchRequest) {
  return apiClient.post<SearchResponse<AccountRow>>('/api/account/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as contas. Atualize a página e tente novamente.' });
}

export async function createAccount(username: string, password: string) {
  return apiClient.post<void>('/api/account', { Username: username, Password: password }, {
    fallbackError: 'Não foi possível criar a conta. Verifique os dados informados e tente novamente.',
  });
}

export async function searchAccountsForPicker(payload: SearchRequest) {
  const result = await searchAccounts(payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: (result.data?.items ?? []).map((row): AccountPickerRow => ({
        id: row.id,
        username: row.username,
        roles: row.roles,
      })),
    },
  };
}
