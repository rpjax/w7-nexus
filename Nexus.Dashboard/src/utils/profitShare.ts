import type { ProfitShareCutInput } from '../api/types';

export const PROFIT_SHARE_TOTAL_TARGET = 100;
export const PROFIT_SHARE_TOTAL_TOLERANCE = 0.05;
export const PROFIT_SHARE_MIN_CUT = 0.01;
export const PROFIT_SHARE_MAX_CUT = 100;

export function roundProfitSharePercentage(value: number): number {
  return Math.round(value * 100) / 100;
}

export function isProfitShareTotalValid(total: number): boolean {
  return Math.abs(total - PROFIT_SHARE_TOTAL_TARGET) <= PROFIT_SHARE_TOTAL_TOLERANCE;
}

export function sumProfitSharePercentages(cuts: Pick<ProfitShareCutInput, 'percentage'>[]): number {
  return roundProfitSharePercentage(
    cuts.reduce((sum, cut) => sum + (Number(cut.percentage) || 0), 0),
  );
}

export function parseProfitShareInput(raw: string): number | null {
  const normalized = raw.trim().replace(',', '.');
  if (!normalized) return null;
  const value = Number(normalized);
  if (!Number.isFinite(value)) return null;
  return value;
}

export function formatProfitShareInput(value: number): string {
  if (value === 0) return '0';
  if (!value) return '';
  const rounded = roundProfitSharePercentage(value);
  if (Number.isInteger(rounded)) return String(rounded);
  return rounded.toFixed(2);
}

export function clampProfitSharePercentage(value: number): number {
  return roundProfitSharePercentage(
    Math.min(PROFIT_SHARE_MAX_CUT, Math.max(PROFIT_SHARE_MIN_CUT, value)),
  );
}

export function normalizeProfitShareCuts(cuts: ProfitShareCutInput[]): ProfitShareCutInput[] {
  const rounded = cuts.map((cut) => ({
    accountId: cut.accountId.trim(),
    percentage: roundProfitSharePercentage(cut.percentage),
  }));

  const total = sumProfitSharePercentages(rounded);
  const diff = roundProfitSharePercentage(PROFIT_SHARE_TOTAL_TARGET - total);
  if (Math.abs(diff) > PROFIT_SHARE_TOTAL_TOLERANCE || diff === 0 || rounded.length === 0) {
    return rounded;
  }

  const lastIndex = rounded.length - 1;
  rounded[lastIndex] = {
    ...rounded[lastIndex]!,
    percentage: roundProfitSharePercentage(rounded[lastIndex]!.percentage + diff),
  };

  return rounded;
}

export function isProfitShareCutValid(percentage: number): boolean {
  return percentage >= PROFIT_SHARE_MIN_CUT && percentage <= PROFIT_SHARE_MAX_CUT;
}

export function remainingPercentage(
  cuts: Pick<ProfitShareCutInput, 'percentage'>[],
  exceptIndex?: number,
): number {
  const used = cuts.reduce(
    (sum, cut, index) => (exceptIndex === index ? sum : sum + (Number(cut.percentage) || 0)),
    0,
  );
  return roundProfitSharePercentage(Math.max(0, PROFIT_SHARE_TOTAL_TARGET - used));
}

export function maxPercentageForCut(
  cuts: Pick<ProfitShareCutInput, 'percentage'>[],
  index: number,
): number {
  return roundProfitSharePercentage(remainingPercentage(cuts, index));
}

export function roomToGrowPercentage(
  cuts: Pick<ProfitShareCutInput, 'percentage'>[],
  index: number,
): number {
  const current = cuts[index]?.percentage ?? 0;
  return roundProfitSharePercentage(Math.max(0, maxPercentageForCut(cuts, index) - current));
}

export function clampCutToBudget(
  cuts: Pick<ProfitShareCutInput, 'percentage'>[],
  index: number,
  value: number,
): number {
  const max = maxPercentageForCut(cuts, index);
  const upper = Math.min(PROFIT_SHARE_MAX_CUT, max);
  return roundProfitSharePercentage(Math.max(0, Math.min(value, upper)));
}

export function splitEvenlyPercentages(count: number): number[] {
  if (count <= 0) return [];

  const base = roundProfitSharePercentage(PROFIT_SHARE_TOTAL_TARGET / count);
  const shares = Array.from({ length: count }, () => base);
  const diff = roundProfitSharePercentage(
    PROFIT_SHARE_TOTAL_TARGET - sumProfitSharePercentages(shares.map((percentage) => ({ percentage }))),
  );

  if (shares.length > 0 && diff !== 0) {
    shares[shares.length - 1] = roundProfitSharePercentage(shares[shares.length - 1]! + diff);
  }

  return shares;
}

export function isProfitShareTotalComplete(total: number): boolean {
  return Math.abs(total - PROFIT_SHARE_TOTAL_TARGET) < 0.001;
}
