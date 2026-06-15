import { apiClient } from '../client';

export async function createOperationTeam(operationId: string, name: string) {
  return apiClient.post<void>('/api/operation-administrator/teams', {
    OperationId: operationId,
    Name: name,
  }, { fallbackError: 'Não foi possível criar a equipe.' });
}

export async function deleteOperationTeam(teamId: string) {
  return apiClient.deleteWithBody<void>('/api/operation-administrator/teams', {
    TeamId: teamId,
  }, { fallbackError: 'Não foi possível excluir a equipe.' });
}

export async function assignOperationTeamLeader(teamId: string, teamLeaderId: string) {
  return apiClient.post<void>('/api/operation-administrator/teams/leaders', {
    TeamId: teamId,
    TeamLeaderId: teamLeaderId,
  }, { fallbackError: 'Não foi possível vincular o líder à equipe.' });
}

export async function unassignOperationTeamLeader(teamId: string) {
  return apiClient.deleteWithBody<void>('/api/operation-administrator/teams/leaders', {
    TeamId: teamId,
  }, { fallbackError: 'Não foi possível remover o líder da equipe.' });
}
