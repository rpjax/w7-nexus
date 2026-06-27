import { apiClient } from '../client';
import type {
  OlxAdminAdSpoofRow,
  OlxAdminSearchRequest,
  OlxSearchResponse,
  UnimpersonateAdPayload,
} from './types';

export async function searchOlxAdminAdSpoofs(payload: OlxAdminSearchRequest) {
  return apiClient.post<OlxSearchResponse<OlxAdminAdSpoofRow>>('/api/olx/admin/ad-spoofs/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
    OperatorIds: payload.operatorIds ?? [],
    OperationIds: payload.operationIds ?? [],
  }, { fallbackError: 'Não foi possível carregar os anúncios spoofados.' });
}

export async function adminUnimpersonateOlxAd(payload: UnimpersonateAdPayload) {
  return apiClient.post<unknown>('/api/olx/admin/ads/unimpersonate', {
    OperationId: payload.operationId,
    OperatorId: payload.operatorId,
    AdId: payload.adId,
  }, { fallbackError: 'Não foi possível desimpersonar o anúncio.' });
}
