import { searchAdministratorPayments } from '../../api/administrator/payments';
import type { PaymentRow, SearchRequest, SearchResponse } from '../../api/types';

export type EligibleWithdrawalPaymentsParams = SearchRequest & {
  strawManId: string;
};

export function isEligibleWithdrawalPayment(
  payment: PaymentRow,
  strawManId: string,
): boolean {
  return (
    payment.strawManId === strawManId
    && payment.status === 'Paid'
    && payment.settlementStatus === 'Unsettled'
  );
}

export async function searchEligibleWithdrawalPayments(params: EligibleWithdrawalPaymentsParams) {
  const strawManId = params.strawManId.trim();

  const result = await searchAdministratorPayments({
    limit: params.limit,
    offset: params.offset,
    keyword: params.keyword ?? null,
    status: 'Paid',
    settlementStatus: 'Unsettled',
    strawManId,
  });

  if (!result.ok || !result.data) {
    return result;
  }

  const items = result.data.items.filter((payment) =>
    isEligibleWithdrawalPayment(payment, strawManId));

  const response: SearchResponse<PaymentRow> = {
    ...result.data,
    items,
    total: items.length,
  };

  return { ok: true as const, data: response };
}
