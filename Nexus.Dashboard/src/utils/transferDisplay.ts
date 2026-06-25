import type { TransferEndpoint, TransferRow, TransferTimelineStep } from '../api/types';
import { resolveBankMetadata } from './bankAccountDisplay';
import { formatMoney } from './financeLabels';
import { shortId } from './format';

export function formatTransferEndpointTitle(endpoint: TransferEndpoint): string {
  if (endpoint.label?.trim()) return endpoint.label.trim();
  if (endpoint.username?.trim()) return `@${endpoint.username.trim()}`;
  const bank = resolveBankMetadata(endpoint.displayName);
  if (bank) return bank.name;
  return humanizeAccountKey(endpoint.displayName);
}

export function formatTransferEndpointSubtitle(endpoint: TransferEndpoint): string | null {
  if (endpoint.bankSummary?.trim()) {
    const bank = resolveBankMetadata(endpoint.displayName);
    if (bank) return `${bank.code} · ${endpoint.bankSummary.trim()}`;
    return endpoint.bankSummary.trim();
  }
  if (endpoint.cryptoSummary?.trim()) return endpoint.cryptoSummary.trim();
  return null;
}

function humanizeAccountKey(key: string): string {
  const trimmed = key.trim();
  if (!trimmed) return '—';
  const bank = resolveBankMetadata(trimmed);
  if (bank) return bank.name;
  return trimmed
    .replace(/_/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim();
}

export function resolveStepPrimaryAmount(step: TransferTimelineStep): number | null {
  const credit = step.balanceEffects.find((effect) => effect.direction === 'Credit');
  if (credit) return credit.amount;

  const debit = step.balanceEffects.find((effect) => effect.direction === 'Debit');
  if (debit) return debit.amount;

  const match = step.summary.match(/R\$\s*([\d.]+,\d{2}|\d+)/);
  if (!match) return null;

  const normalized = match[1].includes(',')
    ? match[1].replace(/\./g, '').replace(',', '.')
    : match[1];
  const value = Number(normalized);
  return Number.isFinite(value) ? value : null;
}

export function formatStepAmount(step: TransferTimelineStep): string | null {
  const amount = resolveStepPrimaryAmount(step);
  if (amount === null) return null;

  const effect = step.balanceEffects[0];
  if (effect && (effect.asset || effect.currency !== 'BRL')) {
    return `${amount} ${effect.asset ?? effect.currency}`.trim();
  }

  return formatMoney(amount);
}

function endpointLabel(type: string | null | undefined, accountId: string): string {
  const prefix = type === 'CryptoWallet' ? 'Carteira' : 'Conta';
  return `${prefix} · ${shortId(accountId, 10)}`;
}

export function formatTransferOriginSummary(transfer: TransferRow): string {
  if (transfer.originBankAccount) {
    return endpointLabel('BankAccount', transfer.originBankAccount.bankAccountId);
  }
  if (transfer.originCryptoWallet) {
    return endpointLabel('CryptoWallet', transfer.originCryptoWallet.cryptoWalletId);
  }
  return '—';
}

export function formatTransferDestinationSummary(transfer: TransferRow): string {
  if (transfer.destinationBankAccount) {
    return endpointLabel('BankAccount', transfer.destinationBankAccount.bankAccountId);
  }
  if (transfer.destinationCryptoWallet) {
    return endpointLabel('CryptoWallet', transfer.destinationCryptoWallet.cryptoWalletId);
  }
  return '—';
}
