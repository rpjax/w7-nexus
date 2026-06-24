export const DESTINATION_TYPE_OPTIONS = [
  { value: 'Pix' as const, label: 'PIX (conta bancária)' },
  { value: 'Crypto' as const, label: 'Crypto (carteira)' },
];

export type DestinationType = (typeof DESTINATION_TYPE_OPTIONS)[number]['value'];

export const BANK_ACCOUNT_TYPE_OPTIONS = [
  { value: 'Checking' as const, label: 'Conta corrente' },
  { value: 'Savings' as const, label: 'Conta poupança' },
];

export const ADDRESS_NAMESPACE_OPTIONS = [
  { value: 1, label: 'EVM', enumName: 'Evm' },
  { value: 2, label: 'Tron', enumName: 'Tron' },
  { value: 3, label: 'Solana', enumName: 'Solana' },
  { value: 4, label: 'Bitcoin', enumName: 'Bitcoin' },
  { value: 5, label: 'Litecoin', enumName: 'Litecoin' },
  { value: 6, label: 'Starknet', enumName: 'Starknet' },
  { value: 7, label: 'TON', enumName: 'Ton' },
] as const;

export function namespaceLabel(namespace: string | number): string {
  if (typeof namespace === 'number') {
    return ADDRESS_NAMESPACE_OPTIONS.find((opt) => opt.value === namespace)?.label ?? String(namespace);
  }
  return ADDRESS_NAMESPACE_OPTIONS.find((opt) => opt.enumName === namespace)?.label ?? namespace;
}

export function namespaceEnumName(value: number): string {
  return ADDRESS_NAMESPACE_OPTIONS.find((opt) => opt.value === value)?.enumName ?? 'Evm';
}

export const CHAIN_OPTIONS = [
  { value: 1, label: 'Tron', enumName: 'Tron' },
  { value: 2, label: 'BNB Smart Chain', enumName: 'BnbSmartChain' },
  { value: 3, label: 'Ethereum', enumName: 'Ethereum' },
  { value: 4, label: 'Polygon', enumName: 'Polygon' },
  { value: 5, label: 'Solana', enumName: 'Solana' },
  { value: 6, label: 'Arbitrum One', enumName: 'ArbitrumOne' },
  { value: 7, label: 'Optimism', enumName: 'Optimism' },
  { value: 8, label: 'Base', enumName: 'Base' },
  { value: 9, label: 'Avalanche C-Chain', enumName: 'AvalancheCChain' },
  { value: 10, label: 'Bitcoin', enumName: 'Bitcoin' },
  { value: 11, label: 'zkSync Era', enumName: 'ZkSyncEra' },
  { value: 12, label: 'Linea', enumName: 'Linea' },
  { value: 13, label: 'Scroll', enumName: 'Scroll' },
  { value: 14, label: 'Mantle', enumName: 'Mantle' },
  { value: 15, label: 'Manta Pacific', enumName: 'MantaPacific' },
  { value: 16, label: 'Starknet', enumName: 'Starknet' },
  { value: 17, label: 'TON', enumName: 'Ton' },
  { value: 18, label: 'Litecoin', enumName: 'Litecoin' },
] as const;

export function chainLabel(chain: string | number): string {
  if (typeof chain === 'number') {
    return CHAIN_OPTIONS.find((opt) => opt.value === chain)?.label ?? String(chain);
  }
  return CHAIN_OPTIONS.find((opt) => opt.enumName === chain)?.label ?? chain;
}

export function chainEnumName(value: number): string {
  return CHAIN_OPTIONS.find((opt) => opt.value === value)?.enumName ?? 'Tron';
}

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

export function transferTypeLabel(type: string): string {
  switch (type) {
    case 'Withdrawal': return 'Saque';
    case 'Movement': return 'Movimentação';
    case 'Payout': return 'Repasse';
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
