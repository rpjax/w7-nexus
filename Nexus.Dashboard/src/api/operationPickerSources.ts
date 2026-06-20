import type { OperationPickerSearchFn } from './operationPicker';
import { toOperationPickerSearchFn } from './operationPicker';
import { searchAdministratorOperationsForPicker } from './administrator/operationPickers';

/** Busca operações via `POST /api/administrator/operations/to-assign/search`. */
export const searchAdministratorOperationsPicker = toOperationPickerSearchFn(
  searchAdministratorOperationsForPicker,
);

export type { OperationPickerSearchFn };
