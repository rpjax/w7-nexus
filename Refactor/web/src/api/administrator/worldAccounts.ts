import { apiClient } from '@/api/client';

export type WorldAccount = {
  accountId: string;
  kind: string;
  label: string;
  orangeMemberId: string | null;
  level1CutPercent: number | null;
  emissionStatus: string;
  balanceStatus: string;
  balances: Record<string, number>;
  quotas: Record<string, number>;
  createdAt: string;
  lastUpdatedAt: string;
};

export type WorldAccountTransaction = {
  kind: string;
  currency: string;
  amount: number;
  memo: string | null;
  chargeId: string | null;
  occurredAt: string;
};

export async function listWorldAccounts() {
  return apiClient.get<{ items: WorldAccount[] }>('/api/world-accounts/administrator', {
    fallbackError: 'Não foi possível listar contas do livro-mundo.',
  });
}

export async function getWorldAccount(accountId: string) {
  return apiClient.get<WorldAccount>(`/api/world-accounts/administrator/${accountId}`, {
    fallbackError: 'Não foi possível carregar a conta.',
  });
}

export async function openWorldAccount(input: {
  kind: string;
  label: string;
  orangeMemberId?: string;
  level1CutPercent?: number;
  quotaCurrency?: string;
  quotaRemaining?: number;
}) {
  return apiClient.post<{ accountId: string }>('/api/world-accounts/administrator', input, {
    fallbackError: 'Não foi possível abrir a conta.',
  });
}

export async function labelWorldAccount(accountId: string, label: string) {
  return apiClient.put(`/api/world-accounts/administrator/${accountId}/label`, { label }, {
    fallbackError: 'Não foi possível alterar o rótulo.',
  });
}

export async function configureWorldAccount(
  accountId: string,
  input: {
    level1CutPercent?: number;
    orangeMemberId?: string;
    quotaCurrency?: string;
    quotaRemaining?: number;
    emissionStatus?: string;
    balanceStatus?: string;
  },
) {
  return apiClient.put(`/api/world-accounts/administrator/${accountId}`, input, {
    fallbackError: 'Não foi possível configurar a conta.',
  });
}

export async function recordWorldAccountObservation(
  accountId: string,
  input: { direction: string; currency: string; amount: number; memo?: string },
) {
  return apiClient.post(`/api/world-accounts/administrator/${accountId}/observations`, input, {
    fallbackError: 'Não foi possível registrar a observação.',
  });
}

export async function listWorldAccountTransactions(accountId: string) {
  return apiClient.get<{ items: WorldAccountTransaction[] }>(
    `/api/world-accounts/administrator/${accountId}/transactions`,
    { fallbackError: 'Não foi possível listar transações.' },
  );
}
