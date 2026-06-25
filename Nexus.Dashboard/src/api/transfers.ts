import { apiClient } from './client';
import type { SearchResponse, SearchTransfersRequest, TransferRow, TransferTimelineDetails } from './types';

export async function searchTransfers(payload: SearchTransfersRequest) {
  return apiClient.post<SearchResponse<TransferRow>>('/api/administrator/transfers/search', {
    Limit: payload.limit,
    Offset: payload.offset,
    StrawManId: payload.strawManId ?? null,
    Type: payload.type ?? null,
  }, { fallbackError: 'Não foi possível carregar as transferências. Atualize a página e tente novamente.' });
}

export async function getTransfer(transferId: string) {
  return apiClient.get<TransferRow>(`/api/administrator/transfers/${encodeURIComponent(transferId)}`, {
    fallbackError: 'Não foi possível carregar a transferência.',
  });
}

export async function getTransferTimeline(transferId: string) {
  return apiClient.get<TransferTimelineDetails>(`/api/administrator/transfers/${encodeURIComponent(transferId)}/timeline`, {
    fallbackError: 'Não foi possível carregar a linha do tempo da transferência.',
  });
}

export async function createWithdrawalTransfer(payload: {
  strawManId: string;
  bankAccountId?: string | null;
  cryptoWalletId?: string | null;
  paymentIds: string[];
  onrampingMethod?: string | null;
  producedAmount?: number | null;
  producedAsset?: string | null;
  producedChain?: string | null;
  pixTransactionId?: string | null;
  pixAuthenticationCode?: string | null;
  cryptoTransactionId?: string | null;
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/withdrawal', {
    StrawManId: payload.strawManId,
    BankAccountId: payload.bankAccountId ?? null,
    CryptoWalletId: payload.cryptoWalletId ?? null,
    PaymentIds: payload.paymentIds,
    OnrampingMethod: payload.onrampingMethod ?? null,
    ProducedAmount: payload.producedAmount ?? null,
    ProducedAsset: payload.producedAsset ?? null,
    ProducedChain: payload.producedChain ?? null,
    PixTransactionId: payload.pixTransactionId ?? null,
    PixAuthenticationCode: payload.pixAuthenticationCode ?? null,
    CryptoTransactionId: payload.cryptoTransactionId ?? null,
  }, { fallbackError: 'Não foi possível registrar a transferência de saque. Verifique os dados e tente novamente.' });
}

export async function createMovementTransfer(payload: {
  strawManId: string;
  sourceBankAccountId?: string | null;
  sourceCryptoWalletId?: string | null;
  sourceBalanceId: string;
  sourceAmount: number;
  destinationBankAccountId?: string | null;
  destinationCryptoWalletId?: string | null;
  onrampingMethod?: string | null;
  producedAmount?: number | null;
  producedAsset?: string | null;
  producedChain?: string | null;
  pixTransactionId?: string | null;
  pixAuthenticationCode?: string | null;
  cryptoTransactionId?: string | null;
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/movement', {
    StrawManId: payload.strawManId,
    SourceBankAccountId: payload.sourceBankAccountId ?? null,
    SourceCryptoWalletId: payload.sourceCryptoWalletId ?? null,
    SourceBalanceId: payload.sourceBalanceId,
    SourceAmount: payload.sourceAmount,
    DestinationBankAccountId: payload.destinationBankAccountId ?? null,
    DestinationCryptoWalletId: payload.destinationCryptoWalletId ?? null,
    OnrampingMethod: payload.onrampingMethod ?? null,
    ProducedAmount: payload.producedAmount ?? null,
    ProducedAsset: payload.producedAsset ?? null,
    ProducedChain: payload.producedChain ?? null,
    PixTransactionId: payload.pixTransactionId ?? null,
    PixAuthenticationCode: payload.pixAuthenticationCode ?? null,
    CryptoTransactionId: payload.cryptoTransactionId ?? null,
  }, { fallbackError: 'Não foi possível registrar a movimentação.' });
}

export async function createPayoutTransfer(payload: {
  strawManId: string;
  sourceBankAccountId: string;
  sourceBalanceId: string;
  sourceAmount: number;
  destinationBankAccountId?: string | null;
  destinationCryptoWalletId?: string | null;
  pixTransactionId?: string | null;
  pixAuthenticationCode?: string | null;
  cryptoTransactionId?: string | null;
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/payout', {
    StrawManId: payload.strawManId,
    SourceBankAccountId: payload.sourceBankAccountId,
    SourceBalanceId: payload.sourceBalanceId,
    SourceAmount: payload.sourceAmount,
    DestinationBankAccountId: payload.destinationBankAccountId ?? null,
    DestinationCryptoWalletId: payload.destinationCryptoWalletId ?? null,
    PixTransactionId: payload.pixTransactionId ?? null,
    PixAuthenticationCode: payload.pixAuthenticationCode ?? null,
    CryptoTransactionId: payload.cryptoTransactionId ?? null,
  }, { fallbackError: 'Não foi possível registrar o repasse.' });
}
