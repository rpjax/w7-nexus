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
