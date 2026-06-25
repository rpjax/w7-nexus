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
  destinationBankAccountId?: string | null;
  destinationCryptoWalletId?: string | null;
  paymentIds: string[];
  onrampingMethod?: string | null;
  producedAmount?: number | null;
  producedAsset?: string | null;
  producedChain?: string | null;
  proof?: {
    pixTransactionId?: string | null;
    pixAuthenticationCode?: string | null;
    cryptoTransactionId?: string | null;
  } | null;
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/withdrawal', {
    DestinationBankAccountId: payload.destinationBankAccountId ?? null,
    DestinationCryptoWalletId: payload.destinationCryptoWalletId ?? null,
    PaymentIds: payload.paymentIds,
    OnrampingMethod: payload.onrampingMethod ?? null,
    ProducedAmount: payload.producedAmount ?? null,
    ProducedAsset: payload.producedAsset ?? null,
    ProducedChain: payload.producedChain ?? null,
    Proof: payload.proof
      ? {
          PixTransactionId: payload.proof.pixTransactionId ?? null,
          PixAuthenticationCode: payload.proof.pixAuthenticationCode ?? null,
          CryptoTransactionId: payload.proof.cryptoTransactionId ?? null,
        }
      : null,
  }, { fallbackError: 'Não foi possível registrar a transferência de saque. Verifique os dados e tente novamente.' });
}

export async function createBankAccountMovement(payload: {
  sourceBalanceId: string;
  amount: number;
  destinationBankAccountId?: string | null;
  destinationCryptoWalletId?: string | null;
  onrampingMethod?: string | null;
  producedAmount?: number | null;
  producedAsset?: string | null;
  producedChain?: string | null;
  proof?: {
    pixTransactionId?: string | null;
    pixAuthenticationCode?: string | null;
    cryptoTransactionId?: string | null;
  } | null;
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/bank-accounts/movement', {
    SourceBalanceId: payload.sourceBalanceId,
    Amount: payload.amount,
    DestinationBankAccountId: payload.destinationBankAccountId ?? null,
    DestinationCryptoWalletId: payload.destinationCryptoWalletId ?? null,
    OnrampingMethod: payload.onrampingMethod ?? null,
    ProducedAmount: payload.producedAmount ?? null,
    ProducedAsset: payload.producedAsset ?? null,
    ProducedChain: payload.producedChain ?? null,
    Proof: payload.proof
      ? {
          PixTransactionId: payload.proof.pixTransactionId ?? null,
          PixAuthenticationCode: payload.proof.pixAuthenticationCode ?? null,
          CryptoTransactionId: payload.proof.cryptoTransactionId ?? null,
        }
      : null,
  }, { fallbackError: 'Não foi possível registrar a movimentação.' });
}

export async function createCryptoWalletMovement(payload: {
  sourceBalanceId: string;
  amount: number;
  destinationBankAccountId?: string | null;
  destinationCryptoWalletId?: string | null;
  producedAmount?: number | null;
  proof?: {
    pixTransactionId?: string | null;
    pixAuthenticationCode?: string | null;
    cryptoTransactionId?: string | null;
  } | null;
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/crypto-wallets/movement', {
    SourceBalanceId: payload.sourceBalanceId,
    Amount: payload.amount,
    DestinationBankAccountId: payload.destinationBankAccountId ?? null,
    DestinationCryptoWalletId: payload.destinationCryptoWalletId ?? null,
    ProducedAmount: payload.producedAmount ?? null,
    Proof: payload.proof
      ? {
          PixTransactionId: payload.proof.pixTransactionId ?? null,
          PixAuthenticationCode: payload.proof.pixAuthenticationCode ?? null,
          CryptoTransactionId: payload.proof.cryptoTransactionId ?? null,
        }
      : null,
  }, { fallbackError: 'Não foi possível registrar a movimentação.' });
}

export async function createPayoutTransfer(payload: {
  sourceBalanceId: string;
  amount: number;
  destinationBankAccountId?: string | null;
  destinationCryptoWalletId?: string | null;
  proof: {
    pixTransactionId?: string | null;
    pixAuthenticationCode?: string | null;
    cryptoTransactionId?: string | null;
  };
}) {
  return apiClient.post<TransferRow>('/api/administrator/transfers/payout', {
    SourceBalanceId: payload.sourceBalanceId,
    Amount: payload.amount,
    DestinationBankAccountId: payload.destinationBankAccountId ?? null,
    DestinationCryptoWalletId: payload.destinationCryptoWalletId ?? null,
    Proof: {
      PixTransactionId: payload.proof.pixTransactionId ?? null,
      PixAuthenticationCode: payload.proof.pixAuthenticationCode ?? null,
      CryptoTransactionId: payload.proof.cryptoTransactionId ?? null,
    },
  }, { fallbackError: 'Não foi possível registrar o repasse.' });
}
