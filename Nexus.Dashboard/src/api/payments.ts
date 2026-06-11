import { apiClient } from './client';
import type { GatewayPixResult, PaymentRow, SearchRequest, SearchResponse } from './types';

export async function searchPayments(payload: SearchRequest) {
  return apiClient.post<SearchResponse<PaymentRow>>('/api/payments/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar os pagamentos. Atualize a página e tente novamente.' });
}

export async function generatePix(payload: {
  operationId: string;
  amount: number;
  operatorAccountId?: string | null;
  strawManAccountId?: string | null;
}) {
  return apiClient.post<GatewayPixResult>('/api/gateways/pix', {
    OperationId: payload.operationId,
    Amount: payload.amount,
    OperatorAccountId: payload.operatorAccountId ?? null,
    StrawManAccountId: payload.strawManAccountId ?? null,
  }, { fallbackError: 'Não foi possível gerar a cobrança PIX. Verifique os dados e tente novamente.' });
}
