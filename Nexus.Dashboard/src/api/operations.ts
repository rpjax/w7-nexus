import { apiClient } from './client';
import type { OperationPickerRow, OperationRow, SearchRequest, SearchResponse } from './types';

export async function searchOperations(payload: SearchRequest) {
  return apiClient.post<SearchResponse<OperationRow>>('/api/operations/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
  }, { fallbackError: 'Não foi possível carregar as operações. Atualize a página e tente novamente.' });
}

export async function createOperation(name: string, description: string, operators: string[]) {
  return apiClient.post<void>('/api/operations', {
    Name: name,
    Description: description,
    Operators: operators,
  }, { fallbackError: 'Não foi possível registrar a operação. Verifique os dados e tente novamente.' });
}

export async function deleteOperation(operationId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operations', { OperationId: operationId }, {
    fallbackError: 'Não foi possível excluir a operação. Tente novamente.',
  });
}

export async function addOperator(operationId: string, operatorId: string) {
  return apiClient.post<void>('/api/operations/operators', { OperationId: operationId, OperatorId: operatorId }, {
    fallbackError: 'Não foi possível adicionar o operador à operação.',
  });
}

export async function removeOperator(operationId: string, operatorId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operators', { OperationId: operationId, OperatorId: operatorId }, {
    fallbackError: 'Não foi possível remover o operador da operação.',
  });
}

export async function addStrawMan(operationId: string, strawManId: string) {
  return apiClient.post<void>('/api/operations/strawman', { OperationId: operationId, StrawManId: strawManId }, {
    fallbackError: 'Não foi possível adicionar o laranja à operação.',
  });
}

export async function removeStrawMan(operationId: string, strawManId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/strawman', { OperationId: operationId, StrawManId: strawManId }, {
    fallbackError: 'Não foi possível remover o laranja da operação.',
  });
}

export async function enableManualGatewayCredentials(operationId: string) {
  return apiClient.post<void>('/api/operations/gateway-credentials/manual', { OperationId: operationId }, {
    fallbackError: 'Não foi possível ativar a seleção manual de credenciais.',
  });
}

export async function disableManualGatewayCredentials(operationId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/gateway-credentials/manual', { OperationId: operationId }, {
    fallbackError: 'Não foi possível desativar a seleção manual de credenciais.',
  });
}

export async function addGatewayCredential(operationId: string, credentialId: string) {
  return apiClient.post<void>('/api/operations/gateway-credentials', { OperationId: operationId, CredentialId: credentialId }, {
    fallbackError: 'Não foi possível adicionar a credencial à operação.',
  });
}

export async function removeGatewayCredential(operationId: string, credentialId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/gateway-credentials', { OperationId: operationId, CredentialId: credentialId }, {
    fallbackError: 'Não foi possível remover a credencial da operação.',
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
