import { apiClient } from '../client';
import type { AccountPickerRow, SearchRequest } from '../types';

type AccountSearchRow = {
  id: string;
  username: string;
  roles: string[];
};

export async function searchAdministratorOperatorsToAssign(payload: SearchRequest) {
  return apiClient.post<{ total: number; items: AccountSearchRow[] }>(
    '/api/operations/administrator/teams/operators/search',
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
    },
    { fallbackError: 'Não foi possível buscar operadores.' },
  );
}

export async function searchAdministratorProfitShareAccounts(payload: SearchRequest) {
  return apiClient.post<{ total: number; items: AccountSearchRow[] }>(
    '/api/operations/administrator/teams/profit-share-accounts/search',
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
    },
    { fallbackError: 'Não foi possível buscar contas para repasse.' },
  );
}

function toPickerRows(items: AccountSearchRow[]): AccountPickerRow[] {
  return items.map((row) => ({
    id: row.id,
    username: row.username,
    roles: row.roles,
  }));
}

export async function searchAdministratorOperatorsForPicker(payload: SearchRequest) {
  const result = await searchAdministratorOperatorsToAssign(payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: toPickerRows(result.data?.items ?? []),
    },
  };
}

export async function searchAdministratorProfitShareAccountsForPicker(payload: SearchRequest) {
  const result = await searchAdministratorProfitShareAccounts(payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: toPickerRows(result.data?.items ?? []),
    },
  };
}
