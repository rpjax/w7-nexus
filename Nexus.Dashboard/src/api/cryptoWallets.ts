import { apiClient } from './client';
import type {
  CryptoWalletRow,
  SearchResponse,
  SearchScopedAccountsRequest,
} from './types';
import { namespaceEnumName } from '../utils/financeLabels';

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
