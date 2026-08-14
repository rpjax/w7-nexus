import { apiClient } from '@/api/client';

export type LedgerClaim = {
  claimId: string;
  beneficiaryId: string;
  amount: number;
  currency: string;
  originChargeId: string;
  locationAccountId: string;
  status: string;
  kind: string;
  openedAt: string;
  visible: boolean;
};

export async function materializeCharge(input: {
  chargeId: string;
  netAmount: number;
  currency?: string;
  landingWorldAccountId: string;
}) {
  return apiClient.post<{ chargeId: string; status: string; claimIds: string[] }>(
    '/api/ledger/administrator/materializations',
    input,
    { fallbackError: 'Não foi possível materializar a cobrança.' },
  );
}

export async function getClaim(claimId: string) {
  return apiClient.get<LedgerClaim>(`/api/ledger/administrator/claims/${claimId}`, {
    fallbackError: 'Não foi possível carregar o claim.',
  });
}

export async function listClaims(filters?: { chargeId?: string; accountId?: string; beneficiaryId?: string }) {
  const params = new URLSearchParams();
  if (filters?.chargeId) params.set('chargeId', filters.chargeId);
  if (filters?.accountId) params.set('accountId', filters.accountId);
  if (filters?.beneficiaryId) params.set('beneficiaryId', filters.beneficiaryId);
  const query = params.toString();
  return apiClient.get<{ items: LedgerClaim[] }>(
    `/api/ledger/administrator/claims${query ? `?${query}` : ''}`,
    { fallbackError: 'Não foi possível listar claims.' },
  );
}

export type LedgerHop = {
  hopId: string;
  originAccountId: string;
  originCurrency: string;
  bundleClaimIds: string[];
  destinations: { accountId: string; amount: number; currency: string }[];
  cutOrangeMemberId?: string | null;
  cutPercent?: number | null;
  cutInPlace: boolean;
  lossAmount: number;
  occurredAt: string;
};

export async function registerHop(input: {
  originAccountId: string;
  currency: string;
  claimIds?: string[];
  destinations: { accountId: string; amount: number; currency: string }[];
  cut?: { orangeMemberId: string; percent: number; inPlace: boolean; orangeAccountId?: string };
  keepRemainderAtOrigin?: boolean;
  lossCause?: string;
}) {
  return apiClient.post<{ hopId: string; lossAmount: number; claimIds: string[] }>(
    '/api/ledger/administrator/hops',
    input,
    { fallbackError: 'Não foi possível registrar o hop.' },
  );
}

export async function repassClaims(input: {
  originAccountId: string;
  claimIds?: string[];
  payoutAccountId: string;
}) {
  return apiClient.post<{ debitedAmount: number; claimIds: string[] }>(
    '/api/ledger/administrator/repasse',
    input,
    { fallbackError: 'Não foi possível registrar o repasse.' },
  );
}

export async function listHops(accountId?: string) {
  const query = accountId ? `?accountId=${encodeURIComponent(accountId)}` : '';
  return apiClient.get<{ items: LedgerHop[] }>(
    `/api/ledger/administrator/hops${query}`,
    { fallbackError: 'Não foi possível listar hops.' },
  );
}

export async function revealClaim(claimId: string, summary: string) {
  return apiClient.post<{
    claimId: string;
    visible: boolean;
    releasedAmount: number;
    releasedCurrency: string;
    summary: string;
  }>(`/api/ledger/administrator/claims/${claimId}/reveal`, { summary }, { fallbackError: 'Não foi possível revelar o claim.' });
}

export async function markAccountLost(accountId: string, cause: string) {
  return apiClient.post<{ accountId: string; writtenOff: number }>(
    `/api/ledger/administrator/accounts/${accountId}/lost`,
    { cause },
    { fallbackError: 'Não foi possível marcar a conta como perdida.' },
  );
}

export async function reconcileAccount(input: {
  accountId: string;
  currency: string;
  observedBalance: number;
  cause: string;
  claimId?: string;
}) {
  return apiClient.post<{ accountId: string; nexusBalance: number; observedBalance: number }>(
    `/api/ledger/administrator/accounts/${input.accountId}/reconcile`,
    {
      currency: input.currency,
      observedBalance: input.observedBalance,
      cause: input.cause,
      claimId: input.claimId,
    },
    { fallbackError: 'Não foi possível reconciliar a conta.' },
  );
}

export async function reverseCharge(chargeId: string, cause = 'estorno') {
  return apiClient.post<{ chargeId: string; reversedClaims: number }>(
    `/api/ledger/administrator/charges/${chargeId}/reverse`,
    { cause },
    { fallbackError: 'Não foi possível estornar a cobrança.' },
  );
}

export type ExposureLine = {
  accountId: string;
  currency: string;
  amount: number;
  balanceStatus: string;
};

export async function listExposure() {
  return apiClient.get<{ items: ExposureLine[] }>('/api/ledger/administrator/exposure', {
    fallbackError: 'Não foi possível listar a exposição.',
  });
}
