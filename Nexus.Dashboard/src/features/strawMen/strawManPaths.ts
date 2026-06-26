export type StrawManScope = 'self' | 'global-admin';

const SETTINGS_PATHS: Record<StrawManScope, string> = {
  self: '/dashboard/straw-man/settings',
  'global-admin': '/dashboard/admin/straw-men',
};

const PAYMENTS_PATHS: Record<StrawManScope, string> = {
  self: '/dashboard/straw-man/payments',
  'global-admin': '/dashboard/admin/payments',
};

export function settingsPath(scope: StrawManScope): string {
  return SETTINGS_PATHS[scope];
}

export function paymentsPath(scope: StrawManScope): string {
  return PAYMENTS_PATHS[scope];
}

export function paymentDetailPath(scope: StrawManScope, paymentId: string): string {
  return `${PAYMENTS_PATHS[scope]}/${encodeURIComponent(paymentId)}`;
}
