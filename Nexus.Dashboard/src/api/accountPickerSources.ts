import { toAccountPickerSearchFn } from './accountPicker';
import { searchAccountsForPicker } from './accounts';
import { searchAdministratorAccountsForPicker } from './administrator/accounts';

/** Busca contas via `POST /api/account/search`. */
export const searchAccountsPicker = toAccountPickerSearchFn(searchAccountsForPicker);

/** Busca contas via `POST /api/administrator/accounts/search`. */
export const searchAdministratorAccountsPicker = toAccountPickerSearchFn(searchAdministratorAccountsForPicker);
