import type { PaymentRow, PaymentSplitRow } from '../../api/types';
import { roleLabel } from '../../utils/accountAccess';
import { shortTx } from '../../utils/format';

export function formatPaymentParticipant(username?: string | null, fallback = 'Não definido'): string {
  if (username?.trim()) return `@${username.trim()}`;
  return fallback;
}

export function formatPaymentOperation(payment: PaymentRow): string {
  if (payment.operationName?.trim()) return payment.operationName.trim();
  return 'Operação';
}

export function formatSplitParticipant(split: PaymentSplitRow): string {
  return formatPaymentParticipant(split.username, 'Participante');
}

export function formatSplitRole(split: PaymentSplitRow): string | null {
  if (!split.role?.trim()) return null;
  return roleLabel(split.role);
}

export function formatGatewayLabel(gateway: string): string {
  return gateway.trim() || 'Gateway';
}

export function formatGatewayTransaction(transactionId: string): string {
  return shortTx(transactionId);
}

export function participantInitials(label: string): string {
  const cleaned = label.replace(/^@/, '').trim();
  if (!cleaned) return '?';
  const parts = cleaned.split(/[\s._-]+/).filter(Boolean);
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  return cleaned.slice(0, 2).toUpperCase();
}
