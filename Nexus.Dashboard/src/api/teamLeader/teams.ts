import { apiClient } from '../client';
import type { GatewaySelectionStrategy, ProfitShareCutInput } from '../types';

export async function assignOperatorToTeam(teamId: string, operatorId: string) {
  return apiClient.post<void>('/api/team-leader/teams/operators', {
    TeamId: teamId,
    OperatorId: operatorId,
  }, { fallbackError: 'Não foi possível alocar o operador na equipe.' });
}

export async function unassignOperatorFromTeam(teamId: string, operatorId: string) {
  return apiClient.deleteWithBody<void>('/api/team-leader/teams/operators', {
    TeamId: teamId,
    OperatorId: operatorId,
  }, { fallbackError: 'Não foi possível remover o operador da equipe.' });
}

export async function setTeamGatewaySelectionStrategy(teamId: string, strategy: GatewaySelectionStrategy) {
  return apiClient.patch<void>('/api/team-leader/teams/gateway-selection-strategy', {
    TeamId: teamId,
    Strategy: strategy,
  }, { fallbackError: 'Não foi possível alterar a estratégia de gateway.' });
}

export async function assignStrawManToTeam(teamId: string, strawManId: string) {
  return apiClient.post<void>('/api/team-leader/teams/straw-men', {
    TeamId: teamId,
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível vincular o laranja à equipe.' });
}

export async function unassignStrawManFromTeam(teamId: string, strawManId: string) {
  return apiClient.deleteWithBody<void>('/api/team-leader/teams/straw-men', {
    TeamId: teamId,
    StrawManId: strawManId,
  }, { fallbackError: 'Não foi possível remover o laranja da equipe.' });
}

export async function assignGatewayAccountGroupToTeam(teamId: string, gatewayCredentialsGroupId: string) {
  return apiClient.post<void>('/api/team-leader/teams/gateway-account-groups', {
    TeamId: teamId,
    GatewayCredentialsGroupId: gatewayCredentialsGroupId,
  }, { fallbackError: 'Não foi possível vincular o grupo de credenciais.' });
}

export async function unassignGatewayAccountGroupFromTeam(teamId: string, gatewayCredentialsGroupId: string) {
  return apiClient.deleteWithBody<void>('/api/team-leader/teams/gateway-account-groups', {
    TeamId: teamId,
    GatewayCredentialsGroupId: gatewayCredentialsGroupId,
  }, { fallbackError: 'Não foi possível remover o grupo de credenciais.' });
}

export async function assignGatewayAccountToTeam(teamId: string, gatewayCredentialsId: string) {
  return apiClient.post<void>('/api/team-leader/teams/gateway-accounts', {
    TeamId: teamId,
    GatewayCredentialsId: gatewayCredentialsId,
  }, { fallbackError: 'Não foi possível vincular a credencial de gateway.' });
}

export async function unassignGatewayAccountFromTeam(teamId: string, gatewayCredentialsId: string) {
  return apiClient.deleteWithBody<void>('/api/team-leader/teams/gateway-accounts', {
    TeamId: teamId,
    GatewayCredentialsId: gatewayCredentialsId,
  }, { fallbackError: 'Não foi possível remover a credencial de gateway.' });
}

export async function setOperatorProfitShareRule(
  teamId: string,
  operatorId: string,
  cuts: ProfitShareCutInput[],
) {
  return apiClient.put<void>('/api/team-leader/teams/operators/profit-share-rules', {
    TeamId: teamId,
    OperatorId: operatorId,
    Cuts: cuts.map((cut) => ({
      AccountId: cut.accountId,
      Percentage: cut.percentage,
    })),
  }, { fallbackError: 'Não foi possível salvar a regra de repasse.' });
}
