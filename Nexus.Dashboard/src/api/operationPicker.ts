import type { OperationPickerRow, SearchRequest } from './types';

export type OperationPickerSearchParams = {
  limit: number;
  offset: number;
  keyword: string | null;
};

export type OperationPickerSearchResult =
  | { ok: true; total: number; items: OperationPickerRow[] }
  | { ok: false; error: string };

export type OperationPickerSearchFn = (
  params: OperationPickerSearchParams,
) => Promise<OperationPickerSearchResult>;

type ApiSearchResponse = {
  ok: true;
  data: { total: number; items: OperationPickerRow[] } | null;
} | {
  ok: false;
  error: string;
  status: number;
};

export function toOperationPickerSearchFn(
  search: (payload: SearchRequest) => Promise<ApiSearchResponse>,
): OperationPickerSearchFn {
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
