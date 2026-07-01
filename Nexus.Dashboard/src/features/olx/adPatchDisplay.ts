import { formatMoney } from '../../utils/financeLabels';
import { shortId } from '../../utils/format';

export function formatOptionalPrice(value?: number | null): string {
  if (value === null || value === undefined) return '—';
  return formatMoney(value);
}

export function spoofStatusLabel(isImpersonating: boolean): string {
  return isImpersonating ? 'Impersonando' : 'Livre';
}

export function spoofStatusTone(isImpersonating: boolean): 'success' | 'info' {
  return isImpersonating ? 'success' : 'info';
}

export function formatAdSpoofTitle(adId: string): string {
  if (!adId.trim()) return 'Anúncio';
  const trimmed = adId.trim();
  if (/^\d+$/.test(trimmed)) return `#${trimmed}`;
  return trimmed.length <= 20 ? trimmed : `Anúncio ${shortId(trimmed, 18)}`;
}

export function formatEntityRef(value?: string | null, fallback = '—'): string {
  if (!value?.trim()) return fallback;
  return value.length <= 16 ? value : shortId(value, 14);
}

export function parseIdFilter(raw: string): string[] {
  return raw
    .split(/[,;\s]+/)
    .map((part) => part.trim())
    .filter(Boolean);
}

/** OLX listing IDs are numeric (typically ~10 digits). */
export const OLX_AD_ID_MIN_LENGTH = 6;
export const OLX_AD_ID_MAX_LENGTH = 15;

export function isValidOlxAdId(value: string): boolean {
  const trimmed = value.trim();
  return /^\d+$/.test(trimmed)
    && trimmed.length >= OLX_AD_ID_MIN_LENGTH
    && trimmed.length <= OLX_AD_ID_MAX_LENGTH;
}

function extractOlxAdIdFromSlug(slug: string): string | null {
  const trailing = slug.match(/-(\d+)$/);
  if (trailing) return trailing[1];
  if (/^\d+$/.test(slug)) return slug;
  return null;
}

/** Extracts a numeric OLX ad ID from a plain ID or full listing URL. */
export function extractOlxAdId(raw: string): string | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;
  if (/^\d+$/.test(trimmed)) return trimmed;

  try {
    const href = /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`;
    const url = new URL(href);
    const slug = url.pathname.split('/').filter(Boolean).pop() ?? '';
    const fromSlug = extractOlxAdIdFromSlug(slug);
    if (fromSlug) return fromSlug;
  } catch {
    // not a full URL — try path-like fragments below
  }

  const withoutQuery = trimmed.replace(/\?.*$/, '');
  const lastSegment = withoutQuery.split('/').filter(Boolean).pop() ?? withoutQuery;
  return extractOlxAdIdFromSlug(lastSegment);
}

/** Normalizes user input to digits or an ID extracted from a pasted OLX URL. */
export function parseOlxAdIdInput(raw: string): { value: string; fromUrl: boolean } {
  const trimmed = raw.trim();
  if (!trimmed) return { value: '', fromUrl: false };

  if (/^\d+$/.test(trimmed)) {
    return { value: trimmed, fromUrl: false };
  }

  const looksLikeUrl = /https?:\/\/|olx\.com|[/?]/i.test(raw);
  if (looksLikeUrl) {
    const extracted = extractOlxAdId(raw);
    if (extracted) return { value: extracted, fromUrl: true };
    return { value: raw, fromUrl: false };
  }

  return { value: raw.replace(/\D/g, ''), fromUrl: false };
}

export function olxAdIdValidationMessage(value: string): string | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  if (!/^\d+$/.test(trimmed)) {
    return 'Use apenas números ou cole a URL completa do anúncio OLX.';
  }
  if (trimmed.length < OLX_AD_ID_MIN_LENGTH) {
    return `O ID deve ter pelo menos ${OLX_AD_ID_MIN_LENGTH} dígitos.`;
  }
  if (trimmed.length > OLX_AD_ID_MAX_LENGTH) {
    return `O ID deve ter no máximo ${OLX_AD_ID_MAX_LENGTH} dígitos.`;
  }
  return null;
}

export function isValidAdUrl(value: string): boolean {
  return olxAdUrlValidationMessage(value) === null;
}

export function olxAdUrlValidationMessage(value: string): string | null {
  const trimmed = value.trim();
  if (!trimmed) return 'A URL do anúncio é obrigatória.';

  try {
    const href = /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`;
    const url = new URL(href);
    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      return 'A URL deve usar HTTP ou HTTPS.';
    }
    if (!url.hostname) return 'A URL do anúncio é inválida.';
    return null;
  } catch {
    return 'A URL do anúncio é inválida.';
  }
}

export function normalizeAdUrl(value: string): string {
  const trimmed = value.trim();
  if (!trimmed) return '';
  const href = /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`;
  const url = new URL(href);
  return `${url.origin}${url.pathname}${url.search}`;
}
