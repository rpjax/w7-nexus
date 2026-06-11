import { apiClient } from './client';
import type { AccountPickerRow, AccountRow, SearchRequest, SearchResponse } from './types';

export async function searchAccounts(payload: SearchRequest) {
  return apiClient.post<SearchResponse<AccountRow>>('/api/accounts/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as contas. Atualize a página e tente novamente.' });
}

export async function createAccount(username: string, password: string) {
  return apiClient.post<void>('/api/accounts', { Username: username, Password: password }, {
    fallbackError: 'Não foi possível criar a conta. Verifique os dados informados e tente novamente.',
  });
}

export async function searchAccountsForPicker(payload: SearchRequest) {
  return apiClient.post<SearchResponse<AccountPickerRow>>('/api/accounts/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  });
}
