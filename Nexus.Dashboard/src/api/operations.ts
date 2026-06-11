import { apiClient } from './client';
import type { OperationPickerRow, OperationRow, SearchRequest, SearchResponse } from './types';

export async function searchOperations(payload: SearchRequest) {
  return apiClient.post<SearchResponse<OperationRow>>('/api/operations/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Falha ao carregar operações.' });
}

export async function createOperation(name: string, description: string, operators: string[]) {
  return apiClient.post<void>('/api/operations', {
    Name: name,
    Description: description,
    Operators: operators,
  }, { fallbackError: 'Falha ao criar operação.' });
}

export async function deleteOperation(operationId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operations', { OperationId: operationId }, {
    fallbackError: 'Falha ao excluir operação.',
  });
}

export async function addOperator(operationId: string, operatorId: string) {
  return apiClient.post<void>('/api/operations/operators', { OperationId: operationId, OperatorId: operatorId }, {
    fallbackError: 'Falha ao adicionar operador.',
  });
}

export async function removeOperator(operationId: string, operatorId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operators', { OperationId: operationId, OperatorId: operatorId }, {
    fallbackError: 'Falha ao remover operador.',
  });
}

export async function addStrawMan(operationId: string, strawManId: string) {
  return apiClient.post<void>('/api/operations/strawman', { OperationId: operationId, StrawManId: strawManId }, {
    fallbackError: 'Falha ao adicionar laranja.',
  });
}

export async function removeStrawMan(operationId: string, strawManId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/strawman', { OperationId: operationId, StrawManId: strawManId }, {
    fallbackError: 'Falha ao remover laranja.',
  });
}

export async function enableManualGatewayCredentials(operationId: string) {
  return apiClient.post<void>('/api/operations/gateway-credentials/manual', { OperationId: operationId }, {
    fallbackError: 'Falha ao ativar seleção manual.',
  });
}

export async function disableManualGatewayCredentials(operationId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/gateway-credentials/manual', { OperationId: operationId }, {
    fallbackError: 'Falha ao desativar seleção manual.',
  });
}

export async function addGatewayCredential(operationId: string, credentialId: string) {
  return apiClient.post<void>('/api/operations/gateway-credentials', { OperationId: operationId, CredentialId: credentialId }, {
    fallbackError: 'Falha ao adicionar credencial.',
  });
}

export async function removeGatewayCredential(operationId: string, credentialId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/gateway-credentials', { OperationId: operationId, CredentialId: credentialId }, {
    fallbackError: 'Falha ao remover credencial.',
  });
}

export async function searchOperationsForPicker(payload: SearchRequest) {
  return apiClient.post<SearchResponse<{ id: string; name: string }>>('/api/operations/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  });
}

export type { OperationPickerRow };
