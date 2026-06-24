import { getAdministratorPayment } from '../../api/administrator/payments';
import { getOperatorPayment } from '../../api/operator/payments';
import { getStrawManPayment } from '../../api/strawMan/payments';
import type { PaymentRow } from '../../api/types';
import type { PaymentScope } from './paymentPaths';

export async function fetchPaymentById(scope: PaymentScope, paymentId: string) {
  switch (scope) {
    case 'global-admin':
      return getAdministratorPayment(paymentId);
    case 'operator':
      return getOperatorPayment(paymentId);
    case 'straw-man':
      return getStrawManPayment(paymentId);
    default: {
      const _exhaustive: never = scope;
      return _exhaustive;
    }
  }
}

export type PaymentFetchResult = Awaited<ReturnType<typeof fetchPaymentById>>;

export function normalizePaymentRow(row: PaymentRow): PaymentRow {
  return {
    ...row,
    splits: row.splits ?? [],
  };
}
