import { apiClient } from '../client';
import type { OperationDetails, SearchRequest, SearchResponse } from '../types';

export async function searchOperatorOperations(payload: SearchRequest) {
  return apiClient.post<SearchResponse<OperationDetails>>('/api/operations/operator/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar suas operações.' });
}
