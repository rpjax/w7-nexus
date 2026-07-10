import { describe, expect, it } from 'vitest';
import { validateHostPattern, validateHostPatterns } from './hostPatternValidation';

describe('hostPatternValidation', () => {
  it('accepts wildcard and domain patterns', () => {
    expect(validateHostPattern('*')).toBeNull();
    expect(validateHostPattern('*.olx.com.br')).toBeNull();
    expect(validateHostPattern('olx.com.br')).toBeNull();
  });

  it('rejects invalid patterns', () => {
    expect(validateHostPattern('https://olx.com.br')).not.toBeNull();
    expect(validateHostPattern('*.com')).not.toBeNull();
    expect(validateHostPattern('host:443')).not.toBeNull();
  });

  it('validates lists', () => {
    expect(validateHostPatterns(['olx.com.br', '*.olx.com.br'])).toBeNull();
    expect(validateHostPatterns(['*.com'])).not.toBeNull();
  });
});
