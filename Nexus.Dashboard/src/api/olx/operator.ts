import { apiClient } from '../client';
import type {
  ImpersonateAdPayload,
  OlxOperatorAdSpoofRow,
  OlxOperatorSearchRequest,
  OlxSearchResponse,
  UnimpersonateAdPayload,
  UpdateAdSpoofPayload,
} from './types';

export async function searchOlxOperatorAdSpoofs(payload: OlxOperatorSearchRequest) {
  return apiClient.post<OlxSearchResponse<OlxOperatorAdSpoofRow>>('/api/olx/ad-spoofs/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
    OperationIds: payload.operationIds ?? [],
  }, { fallbackError: 'Não foi possível carregar seus anúncios spoofados.' });
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

export async function updateOlxAdSpoof(payload: UpdateAdSpoofPayload) {
  return apiClient.put<unknown>('/api/olx/ads/spoof', {
    OperationId: payload.operationId,
    AdId: payload.adId,
    OriginalPrice: payload.originalPrice ?? null,
    PromotionalPrice: payload.promotionalPrice ?? null,
  }, { fallbackError: 'Não foi possível atualizar o spoof do anúncio.' });
}
