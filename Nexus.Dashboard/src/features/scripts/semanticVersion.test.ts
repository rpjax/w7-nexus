import { describe, expect, it } from 'vitest';
import {
  bumpSkipsOccupiedSlots,
  bumpVersion,
  compareVersions,
  formatVersion,
  listSkippedVersions,
  parseVersion,
  resolveBumpFromBase,
  validateReleaseVersion,
} from './semanticVersion';

describe('semanticVersion', () => {
  it('parses and formats versions', () => {
    expect(parseVersion('1.2.3')).toEqual([1, 2, 3]);
    expect(parseVersion('1.2')).toBeNull();
    expect(formatVersion([0, 0, 1])).toBe('0.0.1');
  });

  it('bumps from base release (naive)', () => {
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

  it('validates against existing versions only', () => {
    const existing = ['1.0.1', '1.0.2', '2.0.0'];
    expect(validateReleaseVersion([1, 0, 3], existing)).toEqual({ ok: true });
    expect(validateReleaseVersion([1, 0, 1], existing).ok).toBe(false);
    expect(validateReleaseVersion([2, 0, 0], existing).ok).toBe(false);
    expect(validateReleaseVersion([0, 0, 1], [])).toEqual({ ok: true });
  });

  it('resolves patch bump skipping occupied slots', () => {
    const existing = ['1.0.1', '1.0.2', '2.0.0'];
    expect(resolveBumpFromBase('1.0.1', 'patch', existing)).toEqual([1, 0, 3]);
    expect(bumpSkipsOccupiedSlots('1.0.1', 'patch', existing)).toBe(true);
  });

  it('resolves minor bump on parallel line while higher major exists', () => {
    const existing = ['1.0.1', '1.1.0', '2.0.0'];
    expect(resolveBumpFromBase('1.0.1', 'minor', existing)).toEqual([1, 2, 0]);
  });

  it('resolves major bump skipping occupied major', () => {
    const existing = ['1.0.1', '2.0.0'];
    expect(resolveBumpFromBase('1.0.1', 'major', existing)).toEqual([3, 0, 0]);
  });

  it('returns naive bump when slot is free', () => {
    const existing = ['1.0.1', '2.0.0'];
    expect(resolveBumpFromBase('1.0.1', 'patch', existing)).toEqual([1, 0, 2]);
    expect(bumpSkipsOccupiedSlots('1.0.1', 'patch', existing)).toBe(false);
  });

  it('lists skipped versions in patch lane', () => {
    const existing = ['1.0.0', '1.0.1', '1.0.2', '2.0.0'];
    expect(listSkippedVersions('1.0.0', 'patch', existing)).toEqual(['1.0.1', '1.0.2']);
    expect(resolveBumpFromBase('1.0.0', 'patch', existing)).toEqual([1, 0, 3]);
  });
});
