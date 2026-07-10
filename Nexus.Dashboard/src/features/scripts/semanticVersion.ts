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
    title: 'Patch',
    hint: 'Correções e ajustes menores. Sobe só o último número da versão.',
    recommended: true,
  },
  {
    id: 'minor',
    title: 'Minor',
    hint: 'Funcionalidades novas compatíveis. Sobe o número do meio e zera o patch.',
  },
  {
    id: 'major',
    title: 'Major',
    hint: 'Mudanças incompatíveis. Sobe o primeiro número e zera minor e patch.',
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

/** Naive single-step semver bump from a base (ignores existing releases). */
export function bumpVersion(baseVersion: string | null, level: VersionBump): VersionTuple {
  if (!baseVersion) {
    if (level === 'major') return [1, 0, 0];
    if (level === 'minor') return [0, 1, 0];
    return [0, 0, 1];
  }

  const parsed = parseVersion(baseVersion);
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

function versionExists(version: VersionTuple, existing: ReadonlySet<string>): boolean {
  return existing.has(formatVersion(version));
}

/**
 * Finds the next available version in the bump lane, skipping occupied slots.
 * Enables parallel lines (e.g. continue 1.x from 1.0.1 while 2.0.0 exists).
 */
export function resolveBumpFromBase(
  baseVersion: string | null,
  level: VersionBump,
  existingVersions: readonly string[],
): VersionTuple {
  const existing = new Set(existingVersions);
  const naive = bumpVersion(baseVersion, level);

  if (!versionExists(naive, existing)) {
    return naive;
  }

  const base = baseVersion ? parseVersion(baseVersion) : null;
  if (!base) {
    let candidate = naive;
    while (versionExists(candidate, existing)) {
      if (level === 'patch') candidate = [candidate[0], candidate[1], candidate[2] + 1];
      else if (level === 'minor') candidate = [candidate[0], candidate[1] + 1, 0];
      else candidate = [candidate[0] + 1, 0, 0];
    }
    return candidate;
  }

  let [maj, min, pat] = base;

  if (level === 'patch') {
    pat += 1;
    while (versionExists([maj, min, pat], existing)) pat += 1;
    return [maj, min, pat];
  }

  if (level === 'minor') {
    min += 1;
    pat = 0;
    while (versionExists([maj, min, pat], existing)) {
      min += 1;
      pat = 0;
    }
    return [maj, min, pat];
  }

  maj += 1;
  min = 0;
  pat = 0;
  while (versionExists([maj, min, pat], existing)) {
    maj += 1;
    min = 0;
    pat = 0;
  }
  return [maj, min, pat];
}

export function bumpSkipsOccupiedSlots(
  baseVersion: string | null,
  level: VersionBump,
  existingVersions: readonly string[],
): boolean {
  const naive = bumpVersion(baseVersion, level);
  const resolved = resolveBumpFromBase(baseVersion, level, existingVersions);
  return formatVersion(naive) !== formatVersion(resolved);
}

/** Lists existing versions skipped between the naive bump and the resolved slot. */
export function listSkippedVersions(
  baseVersion: string | null,
  level: VersionBump,
  existingVersions: readonly string[],
): string[] {
  if (!bumpSkipsOccupiedSlots(baseVersion, level, existingVersions)) return [];

  const naive = bumpVersion(baseVersion, level);
  const resolved = resolveBumpFromBase(baseVersion, level, existingVersions);
  const existing = new Set(existingVersions);
  const skipped: string[] = [];

  if (level === 'patch') {
    let [maj, min, pat] = naive;
    const [rMaj, rMin, rPat] = resolved;
    while (maj === rMaj && min === rMin && pat < rPat) {
      const label = formatVersion([maj, min, pat]);
      if (existing.has(label)) skipped.push(label);
      pat += 1;
    }
    return skipped;
  }

  if (level === 'minor') {
    let [maj, min] = naive;
    const [rMaj, rMin] = resolved;
    while (maj === rMaj && min < rMin) {
      const label = formatVersion([maj, min, 0]);
      if (existing.has(label)) skipped.push(label);
      min += 1;
    }
    return skipped;
  }

  let maj = naive[0];
  const rMaj = resolved[0];
  while (maj < rMaj) {
    const label = formatVersion([maj, 0, 0]);
    if (existing.has(label)) skipped.push(label);
    maj += 1;
  }
  return skipped;
}

export function sortReleaseOptions<T extends { version: string }>(releases: readonly T[]): T[] {
  return [...releases].sort((a, b) => {
    const left = parseVersion(a.version);
    const right = parseVersion(b.version);
    if (!left || !right) return b.version.localeCompare(a.version);
    return compareVersions(right, left);
  });
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
  existingVersions: readonly string[],
): VersionValidation {
  if (candidate.some((value) => !Number.isInteger(value) || value < 0)) {
    return { ok: false, message: 'Use inteiros ≥ 0 em major, minor e patch.' };
  }

  const label = formatVersion(candidate);
  if (existingVersions.includes(label)) {
    return { ok: false, message: `A versão ${label} já existe neste script.` };
  }

  return { ok: true };
}

export function versionModeLabel(mode: VersionMode): string {
  if (mode === 'manual') return 'Manual';
  return BUMP_OPTIONS.find((option) => option.id === mode)?.title ?? mode;
}

export function sortVersionsDesc(versions: readonly string[]): string[] {
  return [...versions].sort((a, b) => {
    const left = parseVersion(a);
    const right = parseVersion(b);
    if (!left || !right) return a.localeCompare(b);
    return compareVersions(right, left);
  });
}
