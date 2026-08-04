export function paymentStatusLabel(status: string): string {
  switch (status) {
    case 'Pending': return 'Pendente';
    case 'Paid': return 'Pago';
    case 'Refunded': return 'Estornado';
    case 'Dead': return 'Expirado';
    default: return status;
  }
}

export function paymentStatusTone(status: string): 'info' | 'success' | 'warn' | 'danger' {
  switch (status) {
    case 'Paid': return 'success';
    case 'Pending': return 'info';
    case 'Refunded': return 'warn';
    case 'Dead': return 'danger';
    default: return 'info';
  }
}

export function settlementStatusLabel(status: string): string {
  switch (status) {
    case 'Unsettled': return 'Não sacado';
    case 'Withdrawn': return 'Sacado';
    default: return status;
  }
}

export function settlementStatusTone(status: string): 'info' | 'success' | 'warn' {
  switch (status) {
    case 'Unsettled': return 'info';
    case 'Withdrawn': return 'success';
    default: return 'info';
  }
}

export function distributionStatusLabel(status: string): string {
  switch (status) {
    case 'Pending': return 'Pendente de repasse';
    case 'Complete': return 'Repassado';
    default: return status;
  }
}

export function distributionStatusTone(status: string): 'info' | 'success' | 'warn' {
  switch (status) {
    case 'Pending': return 'warn';
    case 'Complete': return 'success';
    default: return 'info';
  }
}

export function formatMoney(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export type StatusTone = 'info' | 'success' | 'warn' | 'danger';

export function statusToneToBadgeVariant(
  tone: StatusTone,
): 'info' | 'success' | 'warning' | 'destructive' {
  if (tone === 'success') return 'success';
  if (tone === 'warn') return 'warning';
  if (tone === 'danger') return 'destructive';
  return 'info';
}
