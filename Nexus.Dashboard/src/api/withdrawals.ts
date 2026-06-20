import { apiClient } from './client';
import type {
  BankAccountRow,
  BankAccountType,
  CryptoWalletRow,
  SearchResponse,
  SearchScopedAccountsRequest,
  SearchWithdrawalsRequest,
  WithdrawalRow,
  WithdrawalType,
} from './types';

export async function searchWithdrawals(payload: SearchWithdrawalsRequest) {
  return apiClient.post<SearchResponse<WithdrawalRow>>('/api/withdrawals/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    OperationId: payload.operationId ?? null,
    StrawManAccountId: payload.strawManAccountId ?? null,
    Type: payload.type === 'Pix' ? 0 : payload.type === 'Crypto' ? 1 : null,
  }, { fallbackError: 'Não foi possível carregar os saques. Atualize a página e tente novamente.' });
}

export async function getWithdrawal(withdrawalId: string) {
  return apiClient.get<WithdrawalRow>(`/api/withdrawals/${encodeURIComponent(withdrawalId)}`, {
    fallbackError: 'Não foi possível carregar o saque.',
  });
}

export async function createWithdrawal(payload: {
  operationId: string;
  type: WithdrawalType;
  strawManAccountId: string;
  bankAccountId?: string | null;
  cryptoWalletId?: string | null;
  paymentIds: string[];
  costDescription?: string | null;
  costAmount: number;
  pixTransactionId?: string | null;
  pixAuthenticationCode?: string | null;
  cryptoTransactionId?: string | null;
}) {
  return apiClient.post<WithdrawalRow>('/api/withdrawals', {
    OperationId: payload.operationId,
    Type: payload.type === 'Pix' ? 0 : 1,
    StrawManAccountId: payload.strawManAccountId,
    BankAccountId: payload.bankAccountId ?? null,
    CryptoWalletId: payload.cryptoWalletId ?? null,
    PaymentIds: payload.paymentIds,
    CostDescription: payload.costDescription ?? null,
    CostAmount: payload.costAmount,
    PixTransactionId: payload.pixTransactionId ?? null,
    PixAuthenticationCode: payload.pixAuthenticationCode ?? null,
    CryptoTransactionId: payload.cryptoTransactionId ?? null,
  }, { fallbackError: 'Não foi possível registrar o saque. Verifique os dados e tente novamente.' });
}

export async function searchBankAccounts(payload: SearchScopedAccountsRequest) {
  return apiClient.post<SearchResponse<BankAccountRow>>('/api/withdrawals/bank-accounts/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    StrawManAccountId: payload.strawManAccountId ?? null,
  }, { fallbackError: 'Não foi possível carregar as contas bancárias.' });
}

export async function updateBankAccountLabel(bankAccountId: string, label: string | null) {
  return apiClient.patch<BankAccountRow>(
    `/api/withdrawals/bank-accounts/${encodeURIComponent(bankAccountId)}/label`,
    { Label: label },
    { fallbackError: 'Não foi possível atualizar o apelido da conta.' },
  );
}

export async function createBankAccount(payload: {
  strawManAccountId: string;
  bank: number;
  agency: string;
  accountNumber: string;
  accountDigit?: string | null;
  accountType: BankAccountType;
  pixKey?: string | null;
  label?: string | null;
}) {
  return apiClient.post<BankAccountRow>('/api/withdrawals/bank-accounts', {
    StrawManAccountId: payload.strawManAccountId,
    Bank: payload.bank,
    Agency: payload.agency,
    AccountNumber: payload.accountNumber,
    AccountDigit: payload.accountDigit ?? null,
    AccountType: payload.accountType === 'Checking' ? 0 : 1,
    PixKey: payload.pixKey ?? null,
    Label: payload.label ?? null,
  }, { fallbackError: 'Não foi possível cadastrar a conta bancária.' });
}

export async function searchCryptoWallets(payload: SearchScopedAccountsRequest) {
  return apiClient.post<SearchResponse<CryptoWalletRow>>('/api/withdrawals/crypto-wallets/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    StrawManAccountId: payload.strawManAccountId ?? null,
  }, { fallbackError: 'Não foi possível carregar as carteiras crypto.' });
}

export async function createCryptoWallet(payload: {
  strawManAccountId: string;
  chain: number;
  asset: number;
  address: string;
  memo?: string | null;
  label?: string | null;
}) {
  return apiClient.post<CryptoWalletRow>('/api/withdrawals/crypto-wallets', {
    StrawManAccountId: payload.strawManAccountId,
    Chain: payload.chain,
    Asset: payload.asset,
    Address: payload.address,
    Memo: payload.memo ?? null,
    Label: payload.label ?? null,
  }, { fallbackError: 'Não foi possível cadastrar a carteira crypto.' });
}
