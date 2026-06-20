export const WITHDRAWAL_TYPE_OPTIONS = [
  { value: 'Pix' as const, label: 'PIX (conta bancária)' },
  { value: 'Crypto' as const, label: 'Crypto (carteira)' },
];

export const BANK_ACCOUNT_TYPE_OPTIONS = [
  { value: 'Checking' as const, label: 'Conta corrente' },
  { value: 'Savings' as const, label: 'Conta poupança' },
];

export const CHAIN_OPTIONS = [
  { value: 1, label: 'Tron' },
  { value: 2, label: 'BNB Smart Chain' },
  { value: 3, label: 'Ethereum' },
  { value: 4, label: 'Polygon' },
  { value: 5, label: 'Solana' },
  { value: 6, label: 'Arbitrum One' },
  { value: 7, label: 'Optimism' },
  { value: 8, label: 'Base' },
  { value: 9, label: 'Avalanche C-Chain' },
  { value: 10, label: 'Bitcoin' },
  { value: 11, label: 'zkSync Era' },
  { value: 12, label: 'Linea' },
  { value: 13, label: 'Scroll' },
  { value: 14, label: 'Mantle' },
  { value: 15, label: 'Manta Pacific' },
  { value: 16, label: 'Starknet' },
  { value: 17, label: 'TON' },
  { value: 18, label: 'Litecoin' },
];

export const CRYPTO_ASSET_OPTIONS = [
  { value: 1, label: 'USDT' },
  { value: 2, label: 'USDC' },
  { value: 3, label: 'BTC' },
  { value: 4, label: 'ETH' },
  { value: 5, label: 'LTC' },
];

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

export function withdrawalTypeLabel(type: string): string {
  switch (type) {
    case 'Pix': return 'PIX';
    case 'Crypto': return 'Crypto';
    default: return type;
  }
}

export function bankAccountTypeLabel(type: string): string {
  switch (type) {
    case 'Checking': return 'Corrente';
    case 'Savings': return 'Poupança';
    default: return type;
  }
}

export function formatMoney(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
