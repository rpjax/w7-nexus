import { apiClient } from '../client';
import { GATEWAY_SELECTION_STRATEGY_VALUE, type GatewaySelectionStrategy } from '../types';

export async function setOperationGatewaySelectionStrategy(operationId: string, strategy: GatewaySelectionStrategy) {
  return apiClient.put<void>('/api/operations/administrator/operations/gateway-selection-strategy', {
    OperationId: operationId,
    Strategy: GATEWAY_SELECTION_STRATEGY_VALUE[strategy],
  }, { fallbackError: 'Não foi possível alterar a estratégia de gateway da operação.' });
}

export async function assignStrawManToOperation(operationId: string, strawManId: string) {
  return apiClient.post<void>('/api/operations/administrator/operations/straw-men', {
    OperationId: operationId,
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível vincular o laranja à operação.' });
}

export async function unassignStrawManFromOperation(operationId: string, strawManId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/administrator/operations/straw-men', {
    OperationId: operationId,
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível remover o laranja da operação.' });
}

export async function assignGatewayAccountGroupToOperation(operationId: string, gatewayCredentialsGroupId: string) {
  return apiClient.post<void>('/api/operations/administrator/operations/gateway-account-groups', {
    OperationId: operationId,
    GatewayCredentialsGroupId: gatewayCredentialsGroupId,
  }, { fallbackError: 'Não foi possível vincular o grupo de credenciais.' });
}

export async function unassignGatewayAccountGroupFromOperation(operationId: string, gatewayCredentialsGroupId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/administrator/operations/gateway-account-groups', {
    OperationId: operationId,
    GatewayCredentialsGroupId: gatewayCredentialsGroupId,
  }, { fallbackError: 'Não foi possível remover o grupo de credenciais.' });
}

export async function assignGatewayAccountToOperation(operationId: string, gatewayCredentialsId: string) {
  return apiClient.post<void>('/api/operations/administrator/operations/gateway-accounts', {
    OperationId: operationId,
    GatewayCredentialsId: gatewayCredentialsId,
  }, { fallbackError: 'Não foi possível vincular a credencial de gateway.' });
}

export async function unassignGatewayAccountFromOperation(operationId: string, gatewayCredentialsId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/administrator/operations/gateway-accounts', {
    OperationId: operationId,
    GatewayCredentialsId: gatewayCredentialsId,
  }, { fallbackError: 'Não foi possível remover a credencial de gateway.' });
}
