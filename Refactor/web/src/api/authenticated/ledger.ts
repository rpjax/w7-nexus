import { apiClient } from '@/api/client';

export type StatementLine = {
  originChargeId: string;
  phase: string;
  estimateAmount: number;
  estimateCurrency: string;
  releasedAmount?: number | null;
  releasedCurrency?: string | null;
  summary?: string | null;
  audience?: string | null;
};

export async function getMyStatement() {
  return apiClient.get<{ items: StatementLine[]; view?: string }>('/api/ledger/authenticated/statement', {
    fallbackError: 'Não foi possível carregar o extrato.',
  });
}
