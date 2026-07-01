import { apiClient } from '../client';
import type {
  OlxAdminAdPatchRow,
  OlxAdminSearchRequest,
  OlxSearchResponse,
  UnimpersonateAdPayload,
} from './types';

export async function searchOlxAdminAdPatches(payload: OlxAdminSearchRequest) {
  return apiClient.post<OlxSearchResponse<OlxAdminAdPatchRow>>('/api/olx/admin/ad-patches/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
    OperatorIds: payload.operatorIds ?? [],
    OperationIds: payload.operationIds ?? [],
  }, { fallbackError: 'Não foi possível carregar os anúncios patchados.' });
}

export async function adminUnimpersonateOlxAd(payload: UnimpersonateAdPayload) {
  return apiClient.post<unknown>('/api/olx/admin/ads/unimpersonate', {
    OperationId: payload.operationId,
    OperatorId: payload.operatorId,
    AdId: payload.adId,
  }, { fallbackError: 'Não foi possível desimpersonar o anúncio.' });
}
