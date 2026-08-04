import { useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';

export const DEFAULT_PAGE_SIZE = 20;

type PaginatedResult<T> = {
  items: T[];
  total: number;
};

type ApiSearchResult<T> =
  | { ok: false; error: string; status?: number }
  | { ok: true; data: { items?: T[]; total?: number } | null };

export function adaptSearchResponse<T>(
  result: ApiSearchResult<T>,
): { ok: true; data: PaginatedResult<T> } | { ok: false; error: string } {
  if (!result.ok) return result;
  return {
    ok: true,
    data: {
      items: result.data?.items ?? [],
      total: result.data?.total ?? 0,
    },
  };
}

type UsePaginatedQueryOptions<T> = {
  queryKey: readonly unknown[];
  fetchPage: (params: { limit: number; offset: number; keyword: string | null }) => Promise<
    { ok: true; data: PaginatedResult<T> } | { ok: false; error: string }
  >;
  pageSize?: number;
  enabled?: boolean;
};

export function usePaginatedQuery<T>({
  queryKey,
  fetchPage,
  pageSize = DEFAULT_PAGE_SIZE,
  enabled = true,
}: UsePaginatedQueryOptions<T>) {
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);

  const { data, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: [...queryKey, currentPage, query, pageSize],
    enabled,
    queryFn: async () => {
      const result = await fetchPage({
        limit: pageSize,
        offset: (currentPage - 1) * pageSize,
        keyword: query.trim() || null,
      });
      if (!result.ok) {
        throw new Error(result.error);
      }
      return result.data;
    },
  });

  const totalItems = data?.total ?? 0;
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / pageSize);

  function submitSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  function clearSearch() {
    setSearch('');
    setCurrentPage(1);
    setQuery('');
  }

  function goPrev() {
    setCurrentPage((page) => Math.max(1, page - 1));
  }

  function goNext() {
    setCurrentPage((page) => Math.min(totalPages, page + 1));
  }

  const items = useMemo(() => data?.items ?? [], [data?.items]);

  return {
    search,
    setSearch,
    query,
    currentPage,
    totalItems,
    totalPages,
    items,
    isLoading,
    isFetching,
    error: error instanceof Error ? error.message : null,
    refetch,
    submitSearch,
    clearSearch,
    goPrev,
    goNext,
  };
}
