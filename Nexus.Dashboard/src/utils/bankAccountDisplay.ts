import type { BankAccountRow } from '../api/types';
import { bankAccountTypeLabel } from './financeLabels';
import { formatUtc, shortId } from './format';

export function bankAccountSummary(row: BankAccountRow): string {
  const account = `${row.agency}/${row.accountNumber}${row.accountDigit ? `-${row.accountDigit}` : ''}`;
  return `${row.bankCode} — ${row.bankName} · ${account}`;
}

export function bankAccountPickerLabel(row: BankAccountRow): string {
  if (row.label?.trim()) return row.label.trim();
  return bankAccountSummary(row);
}

export function bankAccountSearchText(row: BankAccountRow): string {
  return [
    row.label,
    row.bankName,
    row.bankCode,
    row.agency,
    row.accountNumber,
    row.accountDigit,
    row.pixKey,
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
