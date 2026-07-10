import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createScript, searchScripts } from '../../../api/scripts/administrator';
import type { ChannelFilter, ResolutionModeFilter, ScriptSummary } from '../../../api/scripts/types';
import { OpsWorkspace } from '../../../components/admin/OpsWorkspace';
import { EmptyState } from '../../../components/EmptyState';
import { PaginationBar } from '../../../components/ListControls';
import { CreateScriptModal } from '../../../components/scripts/CreateScriptModal';
import { ScriptCard } from '../../../components/scripts/ScriptCard';
import { ScriptFilterBar } from '../../../components/scripts/ScriptFilterBar';
import { ScriptInventoryKpis } from '../../../components/scripts/ScriptInventoryKpis';
import { scriptStudioPath } from '../../../features/scripts/scriptPaths';
import { useNotifications } from '../../../notifications/NotificationContext';

const PAGE_SIZE = 20;

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
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [mode, setMode] = useState<ResolutionModeFilter>('all');
  const [channel, setChannel] = useState<ChannelFilter>('all');
  const [rows, setRows] = useState<ScriptSummary[]>([]);
  const [kpiRows, setKpiRows] = useState<ScriptSummary[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [busy, setBusy] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    setBusy(true);
    const result = await searchScripts({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword,
    });
    setBusy(false);

    if (!result.ok) {
      notifyError(result.error);
      setRows([]);
      setTotalItems(0);
      return;
    }

    setRows(result.data?.items ?? []);
    setTotalItems(result.data?.total ?? 0);
  }, [notifyError]);

  const loadKpis = useCallback(async (keyword: string) => {
    const result = await searchScripts({ limit: 100, offset: 0, keyword });
    if (result.ok) setKpiRows(result.data?.items ?? []);
  }, []);

  useEffect(() => {
    void load(currentPage, query);
  }, [currentPage, query, load]);

  useEffect(() => {
    void loadKpis(query);
  }, [query, loadKpis]);

  const visibleRows = useMemo(
    () => filterByChannel(filterByMode(rows, mode), channel),
    [rows, mode, channel],
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

  function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

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
      <OpsWorkspace
        className="admin-surface scripts-page"
        title="Inventário de runtime patches"
        kicker="Admin · Scripts"
        kickerVariant="admin"
        lead="Gerencie scripts, releases e promoções de canal com visibilidade operacional."
        searchId="scripts-search"
        searchLabel="Buscar scripts"
        searchPlaceholder="Nome ou descrição"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={handleSearch}
        onRefresh={() => {
          void load(currentPage, query);
          void loadKpis(query);
        }}
        totalItems={totalItems}
        showTotal={false}
        onCreate={() => setCreateOpen(true)}
        createLabel="Novo script"
        footer={(
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((page) => Math.max(1, page - 1))}
            onNext={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
            disabled={busy}
          />
        )}
      >
        <div className="scripts-command-center">
          <ScriptInventoryKpis {...kpis} />

          <section className="scripts-inventory-panel" aria-label="Lista de scripts">
            <ScriptFilterBar
              mode={mode}
              channel={channel}
              visibleCount={visibleRows.length}
              totalCount={totalItems}
              onModeChange={(next) => { setMode(next); setCurrentPage(1); }}
              onChannelChange={(next) => { setChannel(next); setCurrentPage(1); }}
            />

            {busy && rows.length === 0 ? (
              <div className="scripts-inventory-panel__body">
                <div className="scripts-skeleton-grid" aria-hidden="true">
                  <div className="scripts-skeleton-card" />
                  <div className="scripts-skeleton-card" />
                </div>
                <p className="muted scripts-skeleton">Carregando scripts…</p>
              </div>
            ) : visibleRows.length === 0 ? (
              <div className="scripts-inventory-panel__body">
                <EmptyState
                  title="Nenhum script encontrado"
                  message={query || mode !== 'all' || channel !== 'all'
                    ? 'Tente outro termo de busca ou limpe os filtros.'
                    : 'Crie o primeiro script para começar.'}
                />
              </div>
            ) : (
              <div className="scripts-inventory-panel__body">
                <div className="scripts-card-grid">
                  {visibleRows.map((script) => (
                    <ScriptCard key={script.id} script={script} />
                  ))}
                </div>
              </div>
            )}
          </section>
        </div>
      </OpsWorkspace>

      <CreateScriptModal
        open={createOpen}
        busy={createBusy}
        onClose={() => setCreateOpen(false)}
        onSubmit={handleCreate}
      />
    </>
  );
}
