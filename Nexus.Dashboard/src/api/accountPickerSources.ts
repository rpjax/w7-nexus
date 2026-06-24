import type { AccountPickerSearchFn } from './accountPicker';
import { toAccountPickerSearchFn } from './accountPicker';
import {
  searchAdministratorOperatorsForPicker,
  searchAdministratorProfitShareAccountsForPicker,
} from './administrator/accountPickers';
import {
  searchAdministratorAccountsForPicker,
  searchAdministratorStrawMenForPicker,
} from './administrator/accounts';
import {
  searchOpAdminStrawMenForPicker,
  searchOpAdminTeamLeaderCandidatesForPicker,
} from './operationAdministrator/accountPickers';
import {
  searchTeamLeaderOperatorsForPicker,
  searchTeamLeaderProfitShareAccountsForPicker,
} from './teamLeader/accountPickers';

/** Busca contas via `POST /api/administrator/accounts/search`. */
export const searchAdministratorAccountsPicker = toAccountPickerSearchFn(searchAdministratorAccountsForPicker);

/** Busca laranjas via `POST /api/administrator/accounts/search` (filtro StrawMan). */
export const searchAdministratorStrawMenPicker = toAccountPickerSearchFn(searchAdministratorStrawMenForPicker);

/** Busca operadores via `POST /api/administrator/teams/operators/search`. */
export const searchAdministratorOperatorsPicker = toAccountPickerSearchFn(searchAdministratorOperatorsForPicker);

/** Busca contas de repasse via `POST /api/administrator/teams/profit-share-accounts/search`. */
export const searchAdministratorProfitShareAccountsPicker = toAccountPickerSearchFn(
  searchAdministratorProfitShareAccountsForPicker,
);

/** Busca líderes via `POST /api/operation-administrator/accounts/team-leader-candidates/search`. */
export const searchOpAdminTeamLeaderCandidatesPicker = toAccountPickerSearchFn(
  searchOpAdminTeamLeaderCandidatesForPicker,
);

/** Busca laranjas via `POST /api/operation-administrator/accounts/straw-men/search`. */
export const searchOpAdminStrawMenPicker = toAccountPickerSearchFn(searchOpAdminStrawMenForPicker);

export function createTeamLeaderOperatorsPicker(teamId: string): AccountPickerSearchFn {
  return toAccountPickerSearchFn((payload) => searchTeamLeaderOperatorsForPicker(teamId, payload));
}

export function createTeamLeaderProfitShareAccountsPicker(teamId: string): AccountPickerSearchFn {
  return toAccountPickerSearchFn((payload) => searchTeamLeaderProfitShareAccountsForPicker(teamId, payload));
}
