import { apiClient } from '../client';
import type { OperationPickerRow, SearchRequest } from '../types';

type OperationSearchRow = {
  id: string;
  name: string;
};

export async function searchAdministratorOperationsToAssign(payload: SearchRequest) {
  return apiClient.post<{ total: number; items: OperationSearchRow[] }>(
    '/api/administrator/operations/to-assign/search',
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
    },
    { fallbackError: 'Não foi possível buscar operações.' },
  );
}

export async function searchAdministratorOperationsForPicker(payload: SearchRequest) {
  const result = await searchAdministratorOperationsToAssign(payload);
  if (!result.ok) return result;

  const items: OperationPickerRow[] = (result.data?.items ?? []).map((row) => ({
    id: row.id,
    name: row.name?.trim() ? row.name : row.id,
  }));

  return {
    ok: true as const,
    data: {
      total: result.data?.total ?? 0,
      items,
    },
  };
}
