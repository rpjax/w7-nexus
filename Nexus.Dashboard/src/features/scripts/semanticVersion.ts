export type VersionTuple = [major: number, minor: number, patch: number];
export type VersionBump = 'patch' | 'minor' | 'major';
export type VersionMode = VersionBump | 'manual';

export const BUMP_OPTIONS: {
  id: VersionBump;
  title: string;
  hint: string;
  recommended?: boolean;
}[] = [
  {
    id: 'patch',
    title: 'Patch (+1)',
    hint: 'Correções e ajustes menores',
    recommended: true,
  },
  {
    id: 'minor',
    title: 'Minor (+1, patch → 0)',
    hint: 'Novas funcionalidades compatíveis',
  },
  {
    id: 'major',
    title: 'Major (+1, minor/patch → 0)',
    hint: 'Mudanças incompatíveis ou breaking',
  },
];

export function parseVersion(version: string): VersionTuple | null {
  const parts = version.trim().split('.');
  if (parts.length !== 3) return null;

  const nums = parts.map((part) => Number(part));
  if (nums.some((value) => !Number.isInteger(value) || value < 0)) return null;

  return [nums[0], nums[1], nums[2]];
}

export function formatVersion(version: VersionTuple): string {
  return `${version[0]}.${version[1]}.${version[2]}`;
}

export function bumpVersion(latestVersion: string | null, level: VersionBump): VersionTuple {
  if (!latestVersion) {
    if (level === 'major') return [1, 0, 0];
    if (level === 'minor') return [0, 1, 0];
    return [0, 0, 1];
  }

  const parsed = parseVersion(latestVersion);
  if (!parsed) {
    if (level === 'major') return [1, 0, 0];
    if (level === 'minor') return [0, 1, 0];
    return [0, 0, 1];
  }

  const [major, minor, patch] = parsed;
  if (level === 'major') return [major + 1, 0, 0];
  if (level === 'minor') return [major, minor + 1, 0];
  return [major, minor, patch + 1];
}

export function compareVersions(left: VersionTuple, right: VersionTuple): number {
  if (left[0] !== right[0]) return left[0] - right[0];
  if (left[1] !== right[1]) return left[1] - right[1];
  return left[2] - right[2];
}

export type VersionValidation =
  | { ok: true }
  | { ok: false; message: string };

export function validateReleaseVersion(
  candidate: VersionTuple,
  latestVersion: string | null,
): VersionValidation {
  if (candidate.some((value) => !Number.isInteger(value) || value < 0)) {
    return { ok: false, message: 'Use inteiros ≥ 0 em major, minor e patch.' };
  }

  if (!latestVersion) return { ok: true };

  const latest = parseVersion(latestVersion);
  if (!latest) return { ok: true };

  if (compareVersions(candidate, latest) <= 0) {
    return { ok: false, message: `A versão deve ser maior que ${latestVersion}.` };
  }

  return { ok: true };
}

export function versionModeLabel(mode: VersionMode): string {
  if (mode === 'manual') return 'Manual';
  return BUMP_OPTIONS.find((option) => option.id === mode)?.title ?? mode;
}
