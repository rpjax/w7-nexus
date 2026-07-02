import { apiClient } from '../../client';
import type { ProfitShareCutInput } from '../../types';

export async function assignOperatorToTeam(teamId: string, operatorId: string) {
  return apiClient.post<void>('/api/operations/team-leader/teams/operators', {
    TeamId: teamId,
    OperatorId: operatorId,
  }, { fallbackError: 'Não foi possível alocar o operador na equipe.' });
}

export async function unassignOperatorFromTeam(teamId: string, operatorId: string) {
  return apiClient.deleteWithBody<void>('/api/operations/team-leader/teams/operators', {
    TeamId: teamId,
    OperatorId: operatorId,
  }, { fallbackError: 'Não foi possível remover o operador da equipe.' });
}

export async function setOperatorProfitShareRule(
  teamId: string,
  operatorId: string,
  cuts: ProfitShareCutInput[],
) {
  return apiClient.put<void>('/api/operations/team-leader/teams/operators/profit-share-rules', {
    TeamId: teamId,
    OperatorId: operatorId,
    Cuts: cuts.map((cut) => ({
      AccountId: cut.accountId,
      Percentage: cut.percentage,
    })),
  }, { fallbackError: 'Não foi possível salvar a regra de repasse.' });
}
