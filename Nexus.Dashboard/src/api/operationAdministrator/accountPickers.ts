import { apiClient } from '../client';
import type { AccountPickerRow, SearchRequest } from '../types';

type AccountSearchRow = {
  id: string;
  username: string;
  roles: string[];
};

export async function searchOpAdminTeamLeaderCandidates(payload: SearchRequest) {
  return apiClient.post<{ total: number; items: AccountSearchRow[] }>(
    '/api/operation-administrator/accounts/team-leader-candidates/search',
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
    },
    { fallbackError: 'Não foi possível buscar contas para líder.' },
  );
}

export async function searchOpAdminStrawMenToAssign(payload: SearchRequest) {
  return apiClient.post<{ total: number; items: AccountSearchRow[] }>(
    '/api/operation-administrator/accounts/straw-men/search',
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
    },
    { fallbackError: 'Não foi possível buscar contas laranja.' },
  );
}

function toPickerRows(items: AccountSearchRow[]): AccountPickerRow[] {
  return items.map((row) => ({
    id: row.id,
    username: row.username,
    roles: row.roles,
  }));
}

export async function searchOpAdminTeamLeaderCandidatesForPicker(payload: SearchRequest) {
  const result = await searchOpAdminTeamLeaderCandidates(payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: toPickerRows(result.data?.items ?? []),
    },
  };
}

export async function searchOpAdminStrawMenForPicker(payload: SearchRequest) {
  const result = await searchOpAdminStrawMenToAssign(payload);
  if (!result.ok) return result;

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items: toPickerRows(result.data?.items ?? []),
    },
  };
}
