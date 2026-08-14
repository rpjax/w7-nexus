import { apiClient } from '@/api/client';

export type MyMandate = {
  accountId: string;
  appliedPresets: string[];
  capabilities: string[];
  canGrant: boolean;
  canManageOperations: boolean;
  canManageGateways: boolean;
  canSeeFinance: boolean;
  canActAsOperator: boolean;
  canRecruit: boolean;
};

export type CarteiraDeal = {
  dealId: string;
  operatorAccountId: string;
  operatorPercent: number;
  recruiterPercent: number;
};

export async function getMyMandate() {
  return apiClient.get<MyMandate>('/api/mandates/me', {
    fallbackError: 'Não foi possível carregar o mandato.',
  });
}

export async function getMyCarteira() {
  return apiClient.get<{ items: CarteiraDeal[] }>('/api/mandates/me/carteira', {
    fallbackError: 'Não foi possível carregar a carteira.',
  });
}
