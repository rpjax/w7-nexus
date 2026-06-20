import type { PixKeyType } from '../api/types';

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/i;
const PHONE_PATTERN = /^\+55[1-9]\d{9,10}$/;
const RANDOM_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function extractDigits(value: string): string {
  return value.replace(/\D/g, '');
}

function allSameDigit(digits: string): boolean {
  return digits.length > 0 && digits.split('').every((d) => d === digits[0]);
}

function calculateMod11Verifier(source: number[], initialWeight: number): number {
  let sum = 0;
  for (let i = 0; i < source.length; i += 1) {
    sum += source[i]! * (initialWeight - i);
  }
  const mod = sum % 11;
  return mod < 2 ? 0 : 11 - mod;
}

function isValidCpf(raw: string): boolean {
  const digits = extractDigits(raw);
  if (digits.length !== 11 || allSameDigit(digits)) return false;
  const numbers = digits.split('').map(Number);
  const first = calculateMod11Verifier(numbers.slice(0, 9), 10);
  if (numbers[9] !== first) return false;
  const second = calculateMod11Verifier(numbers.slice(0, 10), 11);
  return numbers[10] === second;
}

function isValidCnpj(raw: string): boolean {
  const digits = extractDigits(raw);
  if (digits.length !== 14 || allSameDigit(digits)) return false;
  const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const numbers = digits.split('').map(Number);
  const first = weights1.reduce((sum, weight, index) => sum + numbers[index]! * weight, 0) % 11;
  const firstDigit = first < 2 ? 0 : 11 - first;
  if (numbers[12] !== firstDigit) return false;
  const second = weights2.reduce((sum, weight, index) => sum + numbers[index]! * weight, 0) % 11;
  const secondDigit = second < 2 ? 0 : 11 - second;
  return numbers[13] === secondDigit;
}

function normalizePhone(raw: string): string | null {
  const trimmed = raw.trim();
  let candidate: string;
  if (trimmed.startsWith('+')) {
    candidate = `+${extractDigits(trimmed)}`;
  } else {
    const digits = extractDigits(trimmed);
    if (digits.length >= 10 && digits.length <= 11) candidate = `+55${digits}`;
    else if (digits.startsWith('55') && (digits.length === 12 || digits.length === 13)) candidate = `+${digits}`;
    else return null;
  }
  return PHONE_PATTERN.test(candidate) ? candidate : null;
}

function normalizeRandom(raw: string): string | null {
  let value = raw.trim().toLowerCase();
  const hex = value.replace(/[^0-9a-f]/gi, '');
  if (hex.length === 32) {
    value = `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
  }
  return RANDOM_PATTERN.test(value) ? value : null;
}

function formatCpfDigits(digits: string): string {
  const d = digits.slice(0, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

function formatCnpjDigits(digits: string): string {
  const d = digits.slice(0, 14);
  if (d.length <= 2) return d;
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`;
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`;
  if (d.length <= 12) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

function formatPhoneDigits(digits: string): string {
  let local = digits;
  if (local.startsWith('55')) local = local.slice(2);
  local = local.slice(0, 11);

  if (local.length === 0) return '';
  if (local.length <= 2) return `+55 (${local}`;
  if (local.length <= 6) return `+55 (${local.slice(0, 2)}) ${local.slice(2)}`;
  if (local.length <= 10) {
    return `+55 (${local.slice(0, 2)}) ${local.slice(2, 6)}-${local.slice(6)}`;
  }
  return `+55 (${local.slice(0, 2)}) ${local.slice(2, 7)}-${local.slice(7)}`;
}

function formatRandomHex(raw: string): string {
  const hex = raw.replace(/[^0-9a-fA-F]/g, '').slice(0, 32).toLowerCase();
  if (hex.length <= 8) return hex;
  if (hex.length <= 12) return `${hex.slice(0, 8)}-${hex.slice(8)}`;
  if (hex.length <= 16) return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12)}`;
  if (hex.length <= 20) {
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16)}`;
  }
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

export function formatPixKeyInput(type: PixKeyType, raw: string): string {
  switch (type) {
    case 'Cpf':
      return formatCpfDigits(extractDigits(raw));
    case 'Cnpj':
      return formatCnpjDigits(extractDigits(raw));
    case 'Email':
      return raw.slice(0, 77);
    case 'Phone':
      return formatPhoneDigits(extractDigits(raw));
    case 'Random':
      return formatRandomHex(raw);
    default:
      return raw;
  }
}

export function pixKeyMaxLength(type: PixKeyType): number | undefined {
  switch (type) {
    case 'Cpf':
      return 14;
    case 'Cnpj':
      return 18;
    case 'Email':
      return 77;
    case 'Phone':
      return 20;
    case 'Random':
      return 36;
    default:
      return undefined;
  }
}

export function pixKeyPlaceholder(type: PixKeyType): string {
  switch (type) {
    case 'Cpf': return '000.000.000-00';
    case 'Cnpj': return '00.000.000/0000-00';
    case 'Email': return 'nome@dominio.com';
    case 'Phone': return '+55 (11) 98765-4321';
    case 'Random': return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx';
    default: return '';
  }
}

export function pixKeyInputMode(type: PixKeyType): 'numeric' | 'email' | 'tel' | 'text' {
  switch (type) {
    case 'Cpf':
    case 'Cnpj':
      return 'numeric';
    case 'Email':
      return 'email';
    case 'Phone':
      return 'tel';
    default:
      return 'text';
  }
}

export function pixKeyHint(type: PixKeyType): string {
  switch (type) {
    case 'Cpf': return '11 dígitos. A máscara é aplicada automaticamente.';
    case 'Cnpj': return '14 dígitos. A máscara é aplicada automaticamente.';
    case 'Email': return 'E-mail em minúsculas, até 77 caracteres (regras DICT/Bacen).';
    case 'Phone': return 'DDD + número. Salvo como +55DDDNNNNNNNNN.';
    case 'Random': return 'UUID v4 (EVP). Hífens inseridos automaticamente.';
    default: return '';
  }
}

export function normalizePixKey(type: PixKeyType, raw: string): string | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;

  switch (type) {
    case 'Cpf':
      return isValidCpf(trimmed) ? extractDigits(trimmed) : null;
    case 'Cnpj':
      return isValidCnpj(trimmed) ? extractDigits(trimmed) : null;
    case 'Email': {
      const email = trimmed.toLowerCase();
      return email.length <= 77 && EMAIL_PATTERN.test(email) ? email : null;
    }
    case 'Phone':
      return normalizePhone(trimmed);
    case 'Random':
      return normalizeRandom(trimmed);
    default:
      return null;
  }
}

export function validatePixKey(type: PixKeyType, raw: string): string | null {
  if (!raw.trim()) return 'Informe a chave PIX.';

  switch (type) {
    case 'Cpf':
      return normalizePixKey(type, raw) ? null : 'Informe um CPF válido com 11 dígitos.';
    case 'Cnpj':
      return normalizePixKey(type, raw) ? null : 'Informe um CNPJ válido com 14 dígitos.';
    case 'Email':
      return normalizePixKey(type, raw) ? null : 'Informe um e-mail válido (até 77 caracteres).';
    case 'Phone':
      return normalizePixKey(type, raw) ? null : 'Informe um telefone válido (+55DDDNNNNNNNNN).';
    case 'Random':
      return normalizePixKey(type, raw) ? null : 'Informe uma chave aleatória UUID válida.';
    default:
      return 'Tipo de chave PIX inválido.';
  }
}

export const PIX_KEY_TYPE_VALUE: Record<PixKeyType, number> = {
  Cpf: 0,
  Cnpj: 1,
  Email: 2,
  Phone: 3,
  Random: 4,
};
