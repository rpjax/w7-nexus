import type { AccountPickerRow, SearchRequest } from './types';

export type AccountPickerSearchParams = {
  limit: number;
  offset: number;
  keyword: string | null;
};

export type AccountPickerSearchResult =
  | { ok: true; total: number; items: AccountPickerRow[] }
  | { ok: false; error: string };

export type AccountPickerSearchFn = (
  params: AccountPickerSearchParams,
) => Promise<AccountPickerSearchResult>;

type ApiSearchResponse = {
  ok: true;
  data: { total: number; items: AccountPickerRow[] } | null;
} | {
  ok: false;
  error: string;
  status: number;
};

export function toAccountPickerSearchFn(
  search: (payload: SearchRequest) => Promise<ApiSearchResponse>,
): AccountPickerSearchFn {
  return async (params) => {
    const result = await search({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    });

    if (!result.ok) {
      return { ok: false, error: result.error };
    }

    return {
      ok: true,
      total: result.data?.total ?? 0,
      items: result.data?.items ?? [],
    };
  };
}
