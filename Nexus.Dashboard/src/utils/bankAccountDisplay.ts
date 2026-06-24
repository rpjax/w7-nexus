import type { BankAccountRow } from '../api/types';
import { BRAZILIAN_BANKS } from '../data/brazilianBanks';
import { bankAccountTypeLabel, formatMoney } from './financeLabels';
import { formatUtc, shortId } from './format';

export function resolveBankMetadata(bankKey: string) {
  return BRAZILIAN_BANKS.find((bank) => bank.key === bankKey) ?? null;
}

export function bankAccountSummary(row: BankAccountRow): string {
  const meta = resolveBankMetadata(row.bank);
  const account = `${row.agency}/${row.accountNumber}${row.accountDigit ? `-${row.accountDigit}` : ''}`;
  if (meta) return `${meta.code} — ${meta.name} · ${account}`;
  return `${row.bank} · ${account}`;
}

export function bankAccountPickerLabel(row: BankAccountRow): string {
  if (row.label?.trim()) return row.label.trim();
  return bankAccountSummary(row);
}

export function bankAccountSearchText(row: BankAccountRow): string {
  const meta = resolveBankMetadata(row.bank);
  return [
    row.label,
    meta?.name,
    meta?.code,
    row.bank,
    row.agency,
    row.accountNumber,
    row.accountDigit,
    bankAccountTypeLabel(row.accountType),
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase();
}

export function bankAccountCopyText(row: BankAccountRow): string {
  const digit = row.accountDigit ? `-${row.accountDigit}` : '';
  return `Ag ${row.agency} · Cc ${row.accountNumber}${digit} (${bankAccountTypeLabel(row.accountType)})`;
}

export function formatBankAccountMeta(row: BankAccountRow): string {
  return `${shortId(row.id)} · ${formatUtc(row.updatedAt)}`;
}

export function formatBankAccountBalance(row: BankAccountRow): string | null {
  if (row.totalBalanceBrl === undefined) return null;
  return formatMoney(row.totalBalanceBrl);
}
