import { apiClient } from '../client';
import type { PaymentRow, SearchRequest, SearchResponse } from '../types';

export type AdminPaymentSearchRequest = SearchRequest & {
  status?: string | null;
  settlementStatus?: string | null;
  operationId?: string | null;
  strawManId?: string | null;
};

export async function searchAdministratorPayments(payload: AdminPaymentSearchRequest) {
  return apiClient.post<SearchResponse<PaymentRow>>('/api/administrator/payments/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
    Status: payload.status ?? null,
    SettlementStatus: payload.settlementStatus ?? null,
    OperationId: payload.operationId ?? null,
    StrawManId: payload.strawManId ?? null,
  }, { fallbackError: 'Não foi possível carregar os pagamentos do sistema.' });
}

export async function getAdministratorPayment(paymentId: string) {
  return apiClient.get<PaymentRow>(`/api/administrator/payments/${encodeURIComponent(paymentId)}`, {
    fallbackError: 'Não foi possível carregar o pagamento.',
  });
}

export async function payAdministratorPayment(paymentId: string) {
  return apiClient.post<PaymentRow>(`/api/administrator/payments/${encodeURIComponent(paymentId)}/pay`, {}, {
    fallbackError: 'Não foi possível marcar o pagamento como pago.',
  });
}

export async function refundAdministratorPayment(paymentId: string) {
  return apiClient.post<PaymentRow>(`/api/administrator/payments/${encodeURIComponent(paymentId)}/refund`, {}, {
    fallbackError: 'Não foi possível reembolsar o pagamento.',
  });
}

export async function killAdministratorPayment(paymentId: string, reason: string) {
  return apiClient.post<PaymentRow>(`/api/administrator/payments/${encodeURIComponent(paymentId)}/kill`, {
    Reason: reason,
  }, { fallbackError: 'Não foi possível cancelar o pagamento.' });
}

export async function deleteAdministratorPayment(paymentId: string) {
  return apiClient.delete(`/api/administrator/payments/${encodeURIComponent(paymentId)}`, {
    fallbackError: 'Não foi possível excluir o pagamento.',
  });
}

export async function bindAdministratorPaymentOperator(paymentId: string, operatorId: string) {
  return apiClient.post<PaymentRow>(`/api/administrator/payments/${encodeURIComponent(paymentId)}/bind-operator`, {
    OperatorId: operatorId,
  }, { fallbackError: 'Não foi possível vincular o operador.' });
}

export async function bindAdministratorPaymentStrawMan(paymentId: string, strawManId: string) {
  return apiClient.post<PaymentRow>(`/api/administrator/payments/${encodeURIComponent(paymentId)}/bind-straw-man`, {
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível vincular o laranja.' });
}
