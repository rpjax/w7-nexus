import { apiClient } from '../client';
import type { PaymentRow, SearchRequest, SearchResponse } from '../types';

export async function searchOperatorPayments(payload: SearchRequest) {
  return apiClient.post<SearchResponse<PaymentRow>>('/api/operator/payments/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar seus pagamentos.' });
}

export async function getOperatorPayment(paymentId: string) {
  return apiClient.get<PaymentRow>(`/api/operator/payments/${encodeURIComponent(paymentId)}`, {
    fallbackError: 'Não foi possível carregar o pagamento.',
  });
}
