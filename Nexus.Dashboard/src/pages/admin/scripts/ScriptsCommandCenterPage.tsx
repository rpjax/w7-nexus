import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createScript, searchScripts } from '@/api/scripts/administrator';
import type { ChannelFilter, ResolutionModeFilter, ScriptSummary } from '@/api/scripts/types';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { CreateScriptModal } from '@/components/scripts/CreateScriptModal';
import { ScriptFilterBar } from '@/components/scripts/ScriptFilterBar';
import { ScriptInventoryKpis } from '@/components/scripts/ScriptInventoryKpis';
import { createScriptColumns } from '@/features/scripts/script-columns';
import { scriptStudioPath } from '@/features/scripts/scriptPaths';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { useNotifications } from '@/notifications/NotificationContext';
import { Button } from '@/components/ui/button';

function channelVersion(script: ScriptSummary, routeValue: string): string | null {
  return script.channels.find((channel) => channel.routeValue === routeValue)?.version ?? null;
}

function filterByMode(items: ScriptSummary[], mode: ResolutionModeFilter): ScriptSummary[] {
  if (mode === 'host') return items.filter((item) => item.hostPatterns.length > 0);
  if (mode === 'name-only') return items.filter((item) => item.hostPatterns.length === 0);
  return items;
}

function filterByChannel(items: ScriptSummary[], channel: ChannelFilter): ScriptSummary[] {
  if (channel === 'all') return items;
  if (channel === 'missing-prod') return items.filter((item) => !channelVersion(item, 'prod'));
  return items.filter((item) => Boolean(channelVersion(item, channel)));
}

export function ScriptsCommandCenterPage() {
  const navigate = useNavigate();
  const { notifyError, notifySuccess } = useNotifications();
  const [mode, setMode] = useState<ResolutionModeFilter>('all');
  const [channel, setChannel] = useState<ChannelFilter>('all');
  const [kpiRows, setKpiRows] = useState<ScriptSummary[]>([]);
  const [createOpen, setCreateOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);

  const {
    search,
    setSearch,
    query,
    currentPage,
    totalItems,
    totalPages,
    items,
    isLoading,
    error,
    refetch,
    submitSearch,
    goPrev,
    goNext,
  } = usePaginatedQuery({
    queryKey: ['admin-scripts'],
    fetchPage: async (params) => adaptSearchResponse(await searchScripts({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword ?? '',
    })),
  });

  const loadKpis = useCallback(async (keyword: string) => {
    const result = await searchScripts({ limit: 100, offset: 0, keyword });
    if (result.ok) setKpiRows(result.data?.items ?? []);
  }, []);

  useEffect(() => {
    void loadKpis(query);
  }, [query, loadKpis]);

  const visibleRows = useMemo(
    () => filterByChannel(filterByMode(items, mode), channel),
    [items, mode, channel],
  );

  const kpis = useMemo(() => {
    const hostScoped = kpiRows.filter((r) => r.hostPatterns.length > 0).length;
    const nameOnly = kpiRows.filter((r) => r.hostPatterns.length === 0).length;
    const missingProd = kpiRows.filter((r) => !channelVersion(r, 'prod')).length;
    return {
      total: kpiRows.length,
      hostScoped,
      nameOnly,
      missingProd,
    };
  }, [kpiRows]);

  const columns = useMemo(() => createScriptColumns(), []);

  async function handleCreate(payload: {
    name: string;
    hostPatterns: string[];
    priority: number;
    description: string | null;
  }) {
    setCreateBusy(true);
    const result = await createScript(payload);
    setCreateBusy(false);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess('Script criado.');
    setCreateOpen(false);
    navigate(scriptStudioPath(result.data!.id));
  }

  return (
    <>
      <ListPageLayout
        kicker="Admin · Scripts"
        kickerVariant="admin"
        title="Inventário de runtime patches"
        description="Gerencie scripts, releases e promoções de canal com visibilidade operacional."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Scripts' },
        ]}
        searchId="scripts-search"
        searchLabel="Buscar scripts"
        searchPlaceholder="Nome ou descrição"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={submitSearch}
        onRefresh={() => {
          void refetch();
          void loadKpis(query);
        }}
        createAction={(
          <Button type="button" onClick={() => setCreateOpen(true)}>
            Novo script
          </Button>
        )}
        isLoading={isLoading}
        error={error}
        isEmpty={!isLoading && !error && visibleRows.length === 0}
        emptyTitle="Nenhum script encontrado"
        emptyMessage={query || mode !== 'all' || channel !== 'all'
          ? 'Tente outro termo de busca ou limpe os filtros.'
          : 'Crie o primeiro script para começar.'}
        footer={(
          <ListPagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={goPrev}
            onNext={goNext}
            disabled={isLoading}
          />
        )}
      >
        <div className="mb-6 space-y-5">
          <ScriptInventoryKpis {...kpis} />

          <ScriptFilterBar
            mode={mode}
            channel={channel}
            visibleCount={visibleRows.length}
            totalCount={totalItems}
            onModeChange={(next) => { setMode(next); }}
            onChannelChange={(next) => { setChannel(next); }}
          />
        </div>

        <DataTable
          columns={columns}
          data={visibleRows}
          getRowId={(row) => row.id}
          onRowClick={(row) => navigate(scriptStudioPath(row.id))}
        />
      </ListPageLayout>

      <CreateScriptModal
        open={createOpen}
        busy={createBusy}
        onClose={() => setCreateOpen(false)}
        onSubmit={handleCreate}
      />
    </>
  );
}
