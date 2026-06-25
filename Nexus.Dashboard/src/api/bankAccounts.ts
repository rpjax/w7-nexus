import { apiClient } from './client';
import type {
  BankAccountRow,
  BankAccountType,
  SearchResponse,
  SearchScopedAccountsRequest,
} from './types';

export async function searchBankAccounts(payload: SearchScopedAccountsRequest) {
  return apiClient.post<SearchResponse<BankAccountRow>>('/api/administrator/bank-accounts/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    OwnerId: payload.ownerId ?? null,
  }, { fallbackError: 'Não foi possível carregar as contas bancárias.' });
}

export async function getBankAccount(bankAccountId: string) {
  return apiClient.get<BankAccountRow>(`/api/administrator/bank-accounts/${encodeURIComponent(bankAccountId)}`, {
    fallbackError: 'Não foi possível carregar a conta bancária.',
  });
}

export async function updateBankAccountLabel(bankAccountId: string, label: string | null) {
  return apiClient.patch<BankAccountRow>(
    `/api/administrator/bank-accounts/${encodeURIComponent(bankAccountId)}/label`,
    { Label: label },
    { fallbackError: 'Não foi possível atualizar o apelido da conta.' },
  );
}

export async function createBankAccount(payload: {
  ownerId: string;
  bank: number;
  agency: string;
  accountNumber: string;
  accountDigit?: string | null;
  accountType: BankAccountType;
  label?: string | null;
}) {
  return apiClient.post<BankAccountRow>('/api/administrator/bank-accounts', {
    OwnerId: payload.ownerId,
    Bank: payload.bank,
    Agency: payload.agency,
    AccountNumber: payload.accountNumber,
    AccountDigit: payload.accountDigit ?? null,
    AccountType: payload.accountType === 'Checking' ? 0 : 1,
    Label: payload.label ?? null,
  }, { fallbackError: 'Não foi possível cadastrar a conta bancária.' });
}
