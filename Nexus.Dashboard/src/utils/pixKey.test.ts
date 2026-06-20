import { describe, expect, it } from 'vitest';
import { formatPixKeyInput, normalizePixKey, validatePixKey } from './pixKey';

describe('formatPixKeyInput', () => {
  it('masks cpf while typing', () => {
    expect(formatPixKeyInput('Cpf', '52998224725')).toBe('529.982.247-25');
    expect(formatPixKeyInput('Cpf', '0111000000000000000000')).toBe('011.100.000-00');
  });

  it('masks cnpj while typing', () => {
    expect(formatPixKeyInput('Cnpj', '11444777000161')).toBe('11.444.777/0001-61');
  });

  it('masks phone while typing', () => {
    expect(formatPixKeyInput('Phone', '11987654321')).toBe('+55 (11) 98765-4321');
  });

  it('masks random uuid while typing', () => {
    expect(formatPixKeyInput('Random', '123e4567e89b42d3a456426614174000'))
      .toBe('123e4567-e89b-42d3-a456-426614174000');
  });

  it('limits email length', () => {
    expect(formatPixKeyInput('Email', 'a'.repeat(80))).toHaveLength(77);
  });
});

describe('normalizePixKey', () => {
  it('normalizes email to lowercase', () => {
    expect(normalizePixKey('Email', '  Conta@Example.COM ')).toBe('conta@example.com');
  });

  it('normalizes cpf to digits', () => {
    expect(normalizePixKey('Cpf', '529.982.247-25')).toBe('52998224725');
  });

  it('normalizes phone to E.164', () => {
    expect(normalizePixKey('Phone', '(11) 98765-4321')).toBe('+5511987654321');
  });

  it('accepts uuid v4 evp keys', () => {
    expect(normalizePixKey('Random', '123E4567-E89B-42D3-A456-426614174000'))
      .toBe('123e4567-e89b-42d3-a456-426614174000');
  });

  it('rejects invalid cpf', () => {
    expect(normalizePixKey('Cpf', '111.111.111-11')).toBeNull();
  });
});

describe('validatePixKey', () => {
  it('requires a value', () => {
    expect(validatePixKey('Email', '   ')).toBe('Informe a chave PIX.');
  });

  it('accepts valid cnpj', () => {
    expect(validatePixKey('Cnpj', '11.444.777/0001-61')).toBeNull();
  });

  it('rejects invalid email', () => {
    expect(validatePixKey('Email', 'invalid-email')).toBe('Informe um e-mail válido (até 77 caracteres).');
  });
});
