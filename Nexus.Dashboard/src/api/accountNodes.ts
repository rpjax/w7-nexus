import { apiClient } from './client';
import type {
  BankAccountRow,
  BankAccountType,
  CryptoWalletRow,
  SearchResponse,
  SearchScopedAccountsRequest,
} from './types';
import { namespaceEnumName } from '../utils/financeLabels';

export async function searchBankAccounts(payload: SearchScopedAccountsRequest) {
  return apiClient.post<SearchResponse<BankAccountRow>>('/api/administrator/bank-accounts/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    StrawManId: payload.strawManId ?? null,
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
  strawManId: string;
  bank: number;
  agency: string;
  accountNumber: string;
  accountDigit?: string | null;
  accountType: BankAccountType;
  label?: string | null;
}) {
  return apiClient.post<BankAccountRow>('/api/administrator/bank-accounts', {
    StrawManId: payload.strawManId,
    Bank: payload.bank,
    Agency: payload.agency,
    AccountNumber: payload.accountNumber,
    AccountDigit: payload.accountDigit ?? null,
    AccountType: payload.accountType === 'Checking' ? 0 : 1,
    Label: payload.label ?? null,
  }, { fallbackError: 'Não foi possível cadastrar a conta bancária.' });
}

export async function searchCryptoWallets(payload: SearchScopedAccountsRequest) {
  return apiClient.post<SearchResponse<CryptoWalletRow>>('/api/administrator/crypto-wallets/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    StrawManId: payload.strawManId ?? null,
  }, { fallbackError: 'Não foi possível carregar as carteiras crypto.' });
}

export async function createCryptoWallet(payload: {
  strawManId: string;
  addresses: Array<{ namespace: number; address: string; memo?: string | null }>;
  label?: string | null;
}) {
  return apiClient.post<CryptoWalletRow>('/api/administrator/crypto-wallets', {
    StrawManId: payload.strawManId,
    Addresses: payload.addresses.map((entry) => ({
      Namespace: namespaceEnumName(entry.namespace),
      Address: entry.address,
      Memo: entry.memo ?? null,
    })),
    Label: payload.label ?? null,
  }, { fallbackError: 'Não foi possível cadastrar a carteira crypto.' });
}

export async function upsertCryptoWalletAddress(
  cryptoWalletId: string,
  payload: { namespace: number; address: string; memo?: string | null },
) {
  return apiClient.put<CryptoWalletRow>(
    `/api/administrator/crypto-wallets/${encodeURIComponent(cryptoWalletId)}/addresses`,
    {
      Namespace: namespaceEnumName(payload.namespace),
      Address: payload.address,
      Memo: payload.memo ?? null,
    },
    { fallbackError: 'Não foi possível atualizar o endereço da carteira.' },
  );
}
