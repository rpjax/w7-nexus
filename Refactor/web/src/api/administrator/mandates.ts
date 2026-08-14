import { apiClient } from '@/api/client';

export const MANDATE_PRESETS = [
  { id: 'Recruiter', label: 'Recrutador' },
  { id: 'OperationsManager', label: 'Gestor de Operações' },
  { id: 'Accountant', label: 'Contador' },
  { id: 'Gateways', label: 'Gateways' },
  { id: 'Operator', label: 'Operador' },
  { id: 'Orange', label: 'Laranja' },
] as const;

export type MandatePresetId = (typeof MANDATE_PRESETS)[number]['id'];

export type MemberMandate = {
  accountId: string;
  appliedPresets: string[];
  grants: Array<{
    id: string;
    capability: string;
    scopeKind: string;
    operationIds: string[];
    grantedBy: string;
    grantedAt: string;
    sourcePreset: string | null;
  }>;
  attritionStatus: string;
  attritionCause: string | null;
};

export type AgencyDeal = {
  dealId: string;
  recruiterAccountId: string;
  operatorAccountId: string;
  operatorPercent: number;
  recruiterPercent: number;
  status: string;
};

export type Shareholder = {
  accountId: string;
  percentage: number;
};

export function presetLabel(presetId: string): string {
  return MANDATE_PRESETS.find((item) => item.id === presetId)?.label ?? presetId;
}

export async function getMemberMandate(accountId: string) {
  return apiClient.get<MemberMandate>(`/api/mandates/administrator/members/${accountId}`, {
    fallbackError: 'Não foi possível carregar o mandato.',
  });
}

export async function grantMandatePreset(accountId: string, presetId: string) {
  return apiClient.post('/api/mandates/administrator/presets', { accountId, presetId }, {
    fallbackError: 'Não foi possível conceder o preset.',
  });
}

export async function revokeMandatePreset(accountId: string, presetId: string) {
  return apiClient.delete('/api/mandates/administrator/presets', { accountId, presetId }, {
    fallbackError: 'Não foi possível revogar o preset.',
  });
}

export async function grantMandateCapability(input: {
  accountId: string;
  capability: string;
  scopeKind: string;
  operationIds?: string[];
}) {
  return apiClient.post('/api/mandates/administrator/capabilities', input, {
    fallbackError: 'Não foi possível conceder a capacidade.',
  });
}

export async function revokeMandateCapability(input: {
  accountId: string;
  capability: string;
  scopeKind: string;
  operationIds?: string[];
}) {
  return apiClient.delete('/api/mandates/administrator/capabilities', input, {
    fallbackError: 'Não foi possível revogar a capacidade.',
  });
}

export async function recordMemberAttrition(accountId: string, status: string, cause: string) {
  return apiClient.post(`/api/mandates/administrator/members/${accountId}/attrition`, { status, cause }, {
    fallbackError: 'Não foi possível registrar attrition.',
  });
}

export async function listAgencyDeals() {
  return apiClient.get<{ items: AgencyDeal[] }>('/api/mandates/administrator/deals', {
    fallbackError: 'Não foi possível listar deals.',
  });
}

export async function upsertAgencyDeal(input: {
  recruiterAccountId: string;
  operatorAccountId: string;
  operatorPercent: number;
  recruiterPercent: number;
}) {
  return apiClient.put<{ dealId: string }>('/api/mandates/administrator/deals', input, {
    fallbackError: 'Não foi possível salvar o deal.',
  });
}

export async function closeAgencyDeal(operatorAccountId: string) {
  return apiClient.post('/api/mandates/administrator/deals/close', { operatorAccountId }, {
    fallbackError: 'Não foi possível encerrar o deal.',
  });
}

export async function listShareholders() {
  return apiClient.get<{ items: Shareholder[]; totalPercent: number }>(
    '/api/mandates/administrator/shareholders',
    { fallbackError: 'Não foi possível listar Acionistas.' },
  );
}

export async function upsertShareholder(accountId: string, percentage: number) {
  return apiClient.put<{ accountId: string; percentage: number }>(
    '/api/mandates/administrator/shareholders',
    { accountId, percentage },
    { fallbackError: 'Não foi possível salvar a participação.' },
  );
}

export async function removeShareholder(accountId: string) {
  return apiClient.delete(`/api/mandates/administrator/shareholders/${accountId}`, undefined, {
    fallbackError: 'Não foi possível remover a participação.',
  });
}
