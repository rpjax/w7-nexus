import { apiClient } from './client';
import type { GatewayPixResult } from './types';

export async function generatePix(payload: {
  operationId: string;
  amount: number;
  operatorId?: string | null;
}) {
  return apiClient.post<GatewayPixResult>('/api/gateways/pix', {
    OperationId: payload.operationId,
    Amount: payload.amount,
    OperatorId: payload.operatorId ?? null,
  }, { fallbackError: 'Não foi possível gerar a cobrança PIX. Verifique os dados e tente novamente.' });
}
