import { apiClient } from '../../client';
import type { OperationWithLedTeamsDetails, SearchRequest, SearchResponse } from '../../types';

export async function searchTeamLeaderLedTeams(payload: SearchRequest) {
  return apiClient.post<SearchResponse<OperationWithLedTeamsDetails>>('/api/operations/team-leader/operations/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as equipes lideradas.' });
}
