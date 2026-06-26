import { describe, expect, it } from 'vitest';
import type { PaymentRow } from '../../api/types';
import { isEligibleWithdrawalPayment } from './searchEligibleWithdrawalPayments';

function payment(overrides: Partial<PaymentRow> = {}): PaymentRow {
  return {
    id: 'pay-1',
    operationId: 'op-1',
    operatorId: null,
    strawManId: 'straw-1',
    gateway: 'Frendz',
    gatewayTransactionId: 'gw-1',
    amount: 100,
    status: 'Paid',
    settlementStatus: 'Unsettled',
    distributionStatus: 'Pending',
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

describe('isEligibleWithdrawalPayment', () => {
  it('accepts paid unsettled payments for the selected straw man', () => {
    expect(isEligibleWithdrawalPayment(payment(), 'straw-1')).toBe(true);
  });

  it('rejects payments from another straw man', () => {
    expect(isEligibleWithdrawalPayment(payment({ strawManId: 'straw-2' }), 'straw-1')).toBe(false);
  });

  it('rejects withdrawn payments', () => {
    expect(isEligibleWithdrawalPayment(payment({ settlementStatus: 'Withdrawn' }), 'straw-1')).toBe(false);
  });
});
