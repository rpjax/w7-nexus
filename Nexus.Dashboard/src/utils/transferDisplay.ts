import type { EnrichedAccountNode, TransferTimelineStep } from '../api/types';
import { resolveBankMetadata } from './bankAccountDisplay';
import { formatMoney } from './financeLabels';

export function formatEnrichedAccountTitle(node: EnrichedAccountNode): string {
  if (node.label?.trim()) return node.label.trim();
  if (node.username?.trim()) return `@${node.username.trim()}`;
  const bank = resolveBankMetadata(node.displayName);
  if (bank) return bank.name;
  return humanizeAccountKey(node.displayName);
}

export function formatEnrichedAccountSubtitle(node: EnrichedAccountNode): string | null {
  if (node.bankSummary?.trim()) {
    const bank = resolveBankMetadata(node.displayName);
    if (bank) return `${bank.code} · ${node.bankSummary.trim()}`;
    return node.bankSummary.trim();
  }
  if (node.cryptoSummary?.trim()) return node.cryptoSummary.trim();
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
