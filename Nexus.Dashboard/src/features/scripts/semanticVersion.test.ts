import { describe, expect, it } from 'vitest';
import {
  bumpVersion,
  compareVersions,
  formatVersion,
  parseVersion,
  validateReleaseVersion,
} from './semanticVersion';

describe('semanticVersion', () => {
  it('parses and formats versions', () => {
    expect(parseVersion('1.2.3')).toEqual([1, 2, 3]);
    expect(parseVersion('1.2')).toBeNull();
    expect(formatVersion([0, 0, 1])).toBe('0.0.1');
  });

  it('bumps from latest release', () => {
    expect(bumpVersion('1.2.3', 'patch')).toEqual([1, 2, 4]);
    expect(bumpVersion('1.2.3', 'minor')).toEqual([1, 3, 0]);
    expect(bumpVersion('1.2.3', 'major')).toEqual([2, 0, 0]);
  });

  it('bumps first release defaults', () => {
    expect(bumpVersion(null, 'patch')).toEqual([0, 0, 1]);
    expect(bumpVersion(null, 'minor')).toEqual([0, 1, 0]);
    expect(bumpVersion(null, 'major')).toEqual([1, 0, 0]);
  });

  it('compares versions', () => {
    expect(compareVersions([1, 0, 0], [0, 9, 9])).toBeGreaterThan(0);
    expect(compareVersions([1, 2, 3], [1, 2, 3])).toBe(0);
  });

  it('validates against latest', () => {
    expect(validateReleaseVersion([1, 2, 4], '1.2.3')).toEqual({ ok: true });
    expect(validateReleaseVersion([1, 2, 3], '1.2.3').ok).toBe(false);
    expect(validateReleaseVersion([1, 2, 2], '1.2.3').ok).toBe(false);
    expect(validateReleaseVersion([0, 0, 1], null)).toEqual({ ok: true });
  });
});
