import { apiClient } from '@/api/client';

export type EmissionRail = {
  railId: string;
  orangeMemberId: string;
  level1CutPercent: number;
  currency: string;
  quotaRemaining: number;
  status: string;
};

export type Charge = {
  chargeId: string;
  operationId: string;
  operatorMemberId: string;
  grossAmount: number;
  currency: string;
  emissionRailId: string;
  orangeMemberId: string;
  splitIntent: { lines: Array<{ order: number; kind: string; percentOfRemainder: number }> };
  status: string;
  externalReference: string | null;
  netAmount: number | null;
  landingWorldAccountId: string | null;
  openedAt: string;
};

export async function listEmissionRails() {
  return apiClient.get<{ items: EmissionRail[] }>('/api/charging/administrator/rails', {
    fallbackError: 'Não foi possível listar trilhos.',
  });
}

export async function listOperationRails(operationId: string) {
  return apiClient.get<{ railIds: string[] }>(`/api/charging/administrator/operations/${operationId}/rails`, {
    fallbackError: 'Não foi possível listar contas de emissão da operação.',
  });
}

export async function bindEmissionRail(operationId: string, railId: string) {
  return apiClient.post(`/api/charging/administrator/operations/${operationId}/rails/${railId}`, {}, {
    fallbackError: 'Não foi possível ligar o trilho.',
  });
}

export async function unbindEmissionRail(operationId: string, railId: string) {
  return apiClient.delete(`/api/charging/administrator/operations/${operationId}/rails/${railId}`, undefined, {
    fallbackError: 'Não foi possível desligar o trilho.',
  });
}

export async function listCharges() {
  return apiClient.get<{ items: Charge[] }>('/api/charging/administrator', {
    fallbackError: 'Não foi possível listar cobranças.',
  });
}

export async function getCharge(chargeId: string) {
  return apiClient.get<Charge>(`/api/charging/administrator/${chargeId}`, {
    fallbackError: 'Não foi possível carregar a cobrança.',
  });
}

export async function createCharge(input: {
  operationId: string;
  grossAmount: number;
  operatorMemberId?: string;
  emissionRailId?: string;
}) {
  return apiClient.post<{ chargeId: string; status: string; externalReference: string | null }>(
    '/api/charging/authenticated',
    input,
    { fallbackError: 'Não foi possível gerar a cobrança.' },
  );
}

export async function transitionCharge(chargeId: string, target: string) {
  return apiClient.post<{ chargeId: string; status: string }>(
    `/api/charging/administrator/${chargeId}/transition`,
    { target },
    { fallbackError: 'Não foi possível alterar o status.' },
  );
}

export async function markChargePaid(chargeId: string) {
  return apiClient.post<{ chargeId: string; status: string }>(
    `/api/charging/administrator/${chargeId}/mark-paid`,
    {},
    { fallbackError: 'Não foi possível marcar como Paga.' },
  );
}
