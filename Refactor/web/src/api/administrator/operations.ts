import { apiClient } from '@/api/client';

export type Operation = {
  operationId: string;
  operationKey: string;
  name: string;
  status: string;
  managementCutPercent: number | null;
  assignedOperatorIds: string[];
  allowsNewCharging: boolean;
  createdAt: string;
  lastUpdatedAt: string;
};

export type StoreObject = {
  objectId: string;
  operationKey: string;
  objectType: string;
  payloadJson: string;
  lastUpdatedAt: string;
};

export async function listOperations() {
  return apiClient.get<{ items: Operation[] }>('/api/operations/administrator', {
    fallbackError: 'Não foi possível listar operações.',
  });
}

export async function getOperation(operationId: string) {
  return apiClient.get<Operation>(`/api/operations/administrator/${operationId}`, {
    fallbackError: 'Não foi possível carregar a operação.',
  });
}

export async function createOperation(name: string, managementCutPercent?: number | null) {
  return apiClient.post<{ operationId: string; operationKey: string; status: string }>(
    '/api/operations/administrator',
    { name, managementCutPercent: managementCutPercent ?? null },
    { fallbackError: 'Não foi possível criar a operação.' },
  );
}

export async function transitionOperation(operationId: string, targetStatus: string) {
  return apiClient.post<{ operationId: string; status: string }>(
    `/api/operations/administrator/${operationId}/transition`,
    { targetStatus },
    { fallbackError: 'Não foi possível alterar o status.' },
  );
}

export async function configureOperationCut(operationId: string, managementCutPercent: number | null) {
  return apiClient.put(`/api/operations/administrator/${operationId}/cut`, { managementCutPercent }, {
    fallbackError: 'Não foi possível salvar o cut.',
  });
}

export async function assignOperator(operationId: string, memberId: string) {
  return apiClient.post(`/api/operations/administrator/${operationId}/assignments`, { memberId }, {
    fallbackError: 'Não foi possível assignar o operador.',
  });
}

export async function unassignOperator(operationId: string, memberId: string) {
  return apiClient.delete(`/api/operations/administrator/${operationId}/assignments/${memberId}`, undefined, {
    fallbackError: 'Não foi possível remover o assign.',
  });
}

export async function registerScript(operationId: string, name: string, body: string) {
  return apiClient.post<{ scriptId: string }>(`/api/operations/administrator/${operationId}/scripts`, {
    name,
    body,
  }, { fallbackError: 'Não foi possível registrar o script.' });
}

export async function listStoreObjects(operationId: string) {
  return apiClient.get<{ items: StoreObject[] }>(`/api/operations/administrator/${operationId}/store`, {
    fallbackError: 'Não foi possível listar o Store.',
  });
}

export async function upsertStoreObject(
  operationId: string,
  input: { objectId?: string; objectType: string; payloadJson: string },
) {
  return apiClient.put<{ objectId: string }>(`/api/operations/administrator/${operationId}/store`, input, {
    fallbackError: 'Não foi possível salvar o objeto.',
  });
}

export async function deleteStoreObject(operationId: string, objectId: string) {
  return apiClient.delete(`/api/operations/administrator/${operationId}/store/${objectId}`, undefined, {
    fallbackError: 'Não foi possível remover o objeto.',
  });
}

export async function resolveScript(operationKey: string) {
  return apiClient.get<{ scriptId: string; name: string; body: string; operationKey: string }>(
    `/api/operations/edge/scripts/${encodeURIComponent(operationKey)}`,
    { fallbackError: 'Não foi possível resolver o script.' },
  );
}
