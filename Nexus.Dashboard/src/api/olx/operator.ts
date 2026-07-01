import { apiClient } from '../client';
import type {
  ImpersonateAdPayload,
  OlxOperatorAdPatchRow,
  OlxOperatorSearchRequest,
  OlxSearchResponse,
  UnimpersonateAdPayload,
  UpdateAdPatchPayload,
} from './types';

export async function searchOlxOperatorAdPatches(payload: OlxOperatorSearchRequest) {
  return apiClient.post<OlxSearchResponse<OlxOperatorAdPatchRow>>('/api/olx/ad-patches/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
    OperationIds: payload.operationIds ?? [],
  }, { fallbackError: 'Não foi possível carregar seus anúncios patchados.' });
}

export async function impersonateOlxAd(payload: ImpersonateAdPayload) {
  return apiClient.post<unknown>('/api/olx/ads/impersonate', {
    OperationId: payload.operationId,
    OperatorId: payload.operatorId,
    AdId: payload.adId,
    AdUrl: payload.adUrl,
  }, { fallbackError: 'Não foi possível impersonar o anúncio.' });
}

export async function unimpersonateOlxAd(payload: UnimpersonateAdPayload) {
  return apiClient.post<unknown>('/api/olx/ads/unimpersonate', {
    OperationId: payload.operationId,
    OperatorId: payload.operatorId,
    AdId: payload.adId,
  }, { fallbackError: 'Não foi possível desimpersonar o anúncio.' });
}

export async function updateOlxAdPatch(payload: UpdateAdPatchPayload) {
  return apiClient.put<unknown>('/api/olx/ads/patch', {
    OperationId: payload.operationId,
    AdId: payload.adId,
    OriginalPrice: payload.originalPrice ?? null,
    PromotionalPrice: payload.promotionalPrice ?? null,
  }, { fallbackError: 'Não foi possível atualizar o patch do anúncio.' });
}
