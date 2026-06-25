import type { CryptoWalletRow } from '../api/types';
import { chainLabel, namespaceLabel } from './financeLabels';
import { shortId } from './format';

const CRYPTO_ASSET_LABELS: Record<string, string> = {
  Usdt: 'USDT',
  Usdc: 'USDC',
  Btc: 'BTC',
  Eth: 'ETH',
  Ltc: 'LTC',
};

export function cryptoAssetLabel(asset: string): string {
  return CRYPTO_ASSET_LABELS[asset] ?? asset;
}

export function formatCryptoAmount(amount: number): string {
  return amount.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 8 });
}

export function formatCryptoWalletAddresses(row: CryptoWalletRow): string {
  const addresses = row.addresses ?? [];
  if (addresses.length === 0) return 'Sem endereços';
  return addresses
    .map((entry) => `${namespaceLabel(entry.namespace)}: ${shortId(entry.address, 12)}`)
    .join(' · ');
}

export function formatCryptoWalletBalances(row: CryptoWalletRow): string {
  const balances = row.balancesByChainAsset ?? [];
  if (balances.length === 0) return 'Sem saldo';
  return balances
    .map((b) => `${chainLabel(b.chain)} ${cryptoAssetLabel(b.asset)} ${formatCryptoAmount(b.totalAmount)}`)
    .join(' · ');
}

export function cryptoWalletSearchText(row: CryptoWalletRow): string {
  const parts = [
    row.label ?? '',
    row.id,
    ...(row.addresses ?? []).flatMap((entry) => [entry.address, entry.namespace, entry.memo ?? '']),
  ];
  return parts.join(' ').toLowerCase();
}

export function cryptoWalletPickerLabel(row: CryptoWalletRow, addressChars = 16): string {
  const balances = formatCryptoWalletBalances(row);
  const balanceHint = balances === 'Sem saldo' ? '' : ` · ${balances}`;
  const firstAddress = row.addresses?.[0];
  const addressHint = firstAddress
    ? `${namespaceLabel(firstAddress.namespace)} ${shortId(firstAddress.address, addressChars)}`
    : 'Sem endereço';
  return `${addressHint}${balanceHint}`;
}
