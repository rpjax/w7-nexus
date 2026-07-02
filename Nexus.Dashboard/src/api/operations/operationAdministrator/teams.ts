import { apiClient } from '../../client';
import { GATEWAY_SELECTION_STRATEGY_VALUE, type GatewaySelectionStrategy } from '../../types';

export async function createOperationTeam(operationId: string, name: string) {
  return apiClient.post<void>('/api/operations/operation-administrator/teams', {
    OperationId: operationId,
    Name: name,
  }, { fallbackError: 'Não foi possível criar a equipe.' });
}

export async function deleteOperationTeam(teamId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operation-administrator/teams', {
    TeamId: teamId,
  }, { fallbackError: 'Não foi possível excluir a equipe.' });
}

export async function assignOperationTeamLeader(teamId: string, teamLeaderId: string) {
  return apiClient.post<void>('/api/operations/operation-administrator/teams/leaders', {
    TeamId: teamId,
    TeamLeaderId: teamLeaderId,
  }, { fallbackError: 'Não foi possível vincular o líder à equipe.' });
}

export async function unassignOperationTeamLeader(teamId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operation-administrator/teams/leaders', {
    TeamId: teamId,
  }, { fallbackError: 'Não foi possível remover o líder da equipe.' });
}

export async function setTeamGatewaySelectionStrategy(teamId: string, strategy: GatewaySelectionStrategy) {
  return apiClient.put<void>('/api/operations/operation-administrator/teams/gateway-selection-strategy', {
    TeamId: teamId,
    Strategy: GATEWAY_SELECTION_STRATEGY_VALUE[strategy],
  }, { fallbackError: 'Não foi possível alterar a estratégia de gateway.' });
}

export async function assignStrawManToTeam(teamId: string, strawManId: string) {
  return apiClient.post<void>('/api/operations/operation-administrator/teams/straw-men', {
    TeamId: teamId,
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível vincular o laranja à equipe.' });
}

export async function unassignStrawManFromTeam(teamId: string, strawManId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operation-administrator/teams/straw-men', {
    TeamId: teamId,
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível remover o laranja da equipe.' });
}

export async function assignGatewayAccountGroupToTeam(teamId: string, gatewayCredentialsGroupId: string) {
  return apiClient.post<void>('/api/operations/operation-administrator/teams/gateway-account-groups', {
    TeamId: teamId,
    GatewayCredentialsGroupId: gatewayCredentialsGroupId,
  }, { fallbackError: 'Não foi possível vincular o grupo de credenciais.' });
}

export async function unassignGatewayAccountGroupFromTeam(teamId: string, gatewayCredentialsGroupId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operation-administrator/teams/gateway-account-groups', {
    TeamId: teamId,
    GatewayCredentialsGroupId: gatewayCredentialsGroupId,
  }, { fallbackError: 'Não foi possível remover o grupo de credenciais.' });
}

export async function assignGatewayAccountToTeam(teamId: string, gatewayCredentialsId: string) {
  return apiClient.post<void>('/api/operations/operation-administrator/teams/gateway-accounts', {
    TeamId: teamId,
    GatewayCredentialsId: gatewayCredentialsId,
  }, { fallbackError: 'Não foi possível vincular a credencial de gateway.' });
}

export async function unassignGatewayAccountFromTeam(teamId: string, gatewayCredentialsId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/operation-administrator/teams/gateway-accounts', {
    TeamId: teamId,
    GatewayCredentialsId: gatewayCredentialsId,
  }, { fallbackError: 'Não foi possível remover a credencial de gateway.' });
}
