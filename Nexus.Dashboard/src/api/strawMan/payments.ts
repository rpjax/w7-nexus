import { apiClient } from '../client';
import type { PaymentRow, SearchRequest, SearchResponse } from '../types';

export async function searchStrawManPayments(payload: SearchRequest) {
  return apiClient.post<SearchResponse<PaymentRow>>('/api/straw-man/payments/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar os pagamentos do laranja.' });
}

export async function getStrawManPayment(paymentId: string) {
  return apiClient.get<PaymentRow>(`/api/straw-man/payments/${encodeURIComponent(paymentId)}`, {
    fallbackError: 'Não foi possível carregar o pagamento.',
  });
}
