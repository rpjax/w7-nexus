import { apiClient } from './client';
import type { PixChargeResult } from './types';

export async function generatePix(payload: {
  operationId: string;
  amount: number;
  operatorId?: string | null;
}) {
  return apiClient.post<PixChargeResult>('/api/charges/administrator/pix', {
    OperationId: payload.operationId,
    Amount: payload.amount,
    OperatorId: payload.operatorId ?? null,
  }, { fallbackError: 'Não foi possível gerar a cobrança PIX. Verifique os dados e tente novamente.' });
}
