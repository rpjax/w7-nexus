import type { ActiveBalanceRow } from '../api/types';
import { cryptoAssetLabel, formatCryptoAmount } from './cryptoWalletDisplay';
import { chainLabel, formatMoney } from './financeLabels';
import { formatTransferEndpointSubtitle, formatTransferEndpointTitle } from './transferDisplay';

export function formatActiveBalanceAmount(balance: ActiveBalanceRow): string {
  if (balance.asset || balance.currency !== 'BRL') {
    const chainPrefix = balance.chain ? `${chainLabel(balance.chain)} · ` : '';
    return `${chainPrefix}${cryptoAssetLabel(balance.asset ?? balance.currency)} ${formatCryptoAmount(balance.amount)}`;
  }
  return `R$ ${formatMoney(balance.amount)}`;
}

export function formatActiveBalanceSource(balance: ActiveBalanceRow): string {
  const title = formatTransferEndpointTitle(balance.account);
  const subtitle = formatTransferEndpointSubtitle(balance.account);
  return subtitle ? `${title} · ${subtitle}` : title;
}

export function formatActiveBalanceAmountInput(balance: ActiveBalanceRow): string {
  if (balance.asset || balance.currency !== 'BRL') {
    return String(balance.amount);
  }
  return formatMoney(balance.amount);
}

export function parseMovementAmount(value: string): number {
  const trimmed = value.trim();
  if (!trimmed) return 0;
  if (trimmed.includes(',')) {
    const normalized = trimmed.replace(/\./g, '').replace(',', '.');
    const parsed = Number(normalized);
    return Number.isFinite(parsed) ? parsed : 0;
  }
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function isMovementAmountWithinLimit(amount: number, maxAmount: number): boolean {
  if (!(amount > 0)) return false;
  const epsilon = maxAmount >= 1 ? 0.0001 : 0.00000001;
  return amount <= maxAmount + epsilon;
}
