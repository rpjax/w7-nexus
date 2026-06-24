export type PaymentScope = 'global-admin' | 'operator' | 'straw-man';

const LIST_PATHS: Record<PaymentScope, string> = {
  'global-admin': '/dashboard/admin/payments',
  operator: '/dashboard/payments',
  'straw-man': '/dashboard/straw-man/payments',
};

export function listPath(scope: PaymentScope): string {
  return LIST_PATHS[scope];
}

export function detailPath(scope: PaymentScope, paymentId: string): string {
  return `${LIST_PATHS[scope]}/${encodeURIComponent(paymentId)}`;
}

export function isPaymentDetailPath(pathname: string): boolean {
  const normalized = pathname.replace(/\/$/, '').toLowerCase();
  return (
    /^\/dashboard\/admin\/payments\/[^/]+$/.test(normalized)
    || (/^\/dashboard\/payments\/[^/]+$/.test(normalized) && !normalized.endsWith('/pix'))
    || /^\/dashboard\/straw-man\/payments\/[^/]+$/.test(normalized)
  );
}
