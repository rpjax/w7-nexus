import { apiClient } from '../client';
import type { OperationDetails, SearchRequest, SearchResponse } from '../types';

type AdminSearchRequest = SearchRequest & {
  administratorIds?: string[];
};

export async function searchAdministratorOperations(payload: AdminSearchRequest) {
  return apiClient.post<SearchResponse<OperationDetails>>('/api/administrator/operations/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    Keyword: payload.keyword ?? null,
    AdministratorIds: payload.administratorIds ?? [],
  }, { fallbackError: 'Não foi possível carregar as operações do sistema.' });
}

export async function createAdministratorOperation(name: string, description: string | null) {
  return apiClient.post<OperationDetails>('/api/administrator/operations', {
    Name: name,
    Description: description,
  }, { fallbackError: 'Não foi possível registrar a operação.' });
}

export async function deleteAdministratorOperation(operationId: string) {
  return apiClient.deleteWithBody<void>('/api/administrator/operations', {
    OperationId: operationId,
  }, { fallbackError: 'Não foi possível excluir a operação.' });
}

export async function assignOperationAdministrator(operationId: string, administratorId: string) {
  return apiClient.post<void>('/api/administrator/operations/administrators', {
    OperationId: operationId,
    AdministratorId: administratorId,
  }, { fallbackError: 'Não foi possível vincular o administrador à operação.' });
}

export async function unassignOperationAdministrator(operationId: string, administratorId: string) {
  return apiClient.deleteWithBody<void>('/api/administrator/operations/administrators', {
    OperationId: operationId,
    AdministratorId: administratorId,
  }, { fallbackError: 'Não foi possível remover o administrador da operação.' });
}
