import { apiClient } from '../client';
import type { OperationDetails, SearchRequest, SearchResponse } from '../types';

export async function searchOperationAdministratorOperations(payload: SearchRequest) {
  return apiClient.post<SearchResponse<OperationDetails>>('/api/operation-administrator/operations/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as operações administradas.' });
}
