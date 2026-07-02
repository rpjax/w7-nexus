import { apiClient } from '../../client';
import type { AccountPickerRow, SearchRequest } from '../../types';

type AccountSearchRow = {
  id: string;
  username: string;
  roles: string[];
};

export async function searchTeamLeaderOperatorsToAssign(teamId: string, payload: SearchRequest) {
  return apiClient.post<{ total: number; items: AccountSearchRow[] }>(
    '/api/operations/team-leader/teams/operators/search',
    {
      TeamId: teamId,
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
    },
    { fallbackError: 'Não foi possível buscar operadores.' },
  );
}

export async function searchTeamLeaderProfitShareAccounts(teamId: string, payload: SearchRequest) {
  return apiClient.post<{ total: number; items: AccountSearchRow[] }>(
    '/api/operations/team-leader/teams/profit-share-accounts/search',
    {
      TeamId: teamId,
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

export async function searchTeamLeaderOperatorsForPicker(teamId: string, payload: SearchRequest) {
  const result = await searchTeamLeaderOperatorsToAssign(teamId, payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: toPickerRows(result.data?.items ?? []),
    },
  };
}

export async function searchTeamLeaderProfitShareAccountsForPicker(teamId: string, payload: SearchRequest) {
  const result = await searchTeamLeaderProfitShareAccounts(teamId, payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: toPickerRows(result.data?.items ?? []),
    },
  };
}
