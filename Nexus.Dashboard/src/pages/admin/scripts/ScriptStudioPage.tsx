import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  addCustomChannel,
  deleteRelease,
  deprecateRelease,
  getReleaseSourceCode,
  getScript,
  listReleases,
  promoteRelease,
  publishRelease,
  restoreRelease,
  updateScript,
} from '../../../api/scripts/administrator';
import type { ChannelSummary, ReleaseSummary, ScriptDetail } from '../../../api/scripts/types';
import { ConfirmDialog } from '../../../components/ConfirmDialog';
import { EmptyState } from '../../../components/EmptyState';
import { AddCustomChannelModal } from '../../../components/scripts/AddCustomChannelModal';
import { ChannelMatrix } from '../../../components/scripts/ChannelMatrix';
import { HostPatternChips } from '../../../components/scripts/HostPatternChips';
import { PromoteChannelDrawer } from '../../../components/scripts/PromoteChannelDrawer';
import { PublishReleaseDrawer } from '../../../components/scripts/PublishReleaseDrawer';
import { ReleaseInspectorPanel } from '../../../components/scripts/ReleaseInspectorPanel';
import { ReleaseStudioOverview } from '../../../components/scripts/ReleaseStudioOverview';
import { ReleaseTimeline } from '../../../components/scripts/ReleaseTimeline';
import { ResolutionModeBadge } from '../../../components/scripts/ResolutionModeBadge';
import { ScriptMetadataPanel } from '../../../components/scripts/ScriptMetadataPanel';
import { SCRIPTS_ADMIN_LIST_PATH } from '../../../features/scripts/scriptPaths';
import { useNotifications } from '../../../notifications/NotificationContext';

type StudioTab = 'overview' | 'releases' | 'channels';

const TAB_LABELS: Record<StudioTab, string> = {
  overview: 'Visão geral',
  releases: 'Releases',
  channels: 'Canais',
};

function arraysEqual(a: string[], b: string[]) {
  if (a.length !== b.length) return false;
  return a.every((value, index) => value === b[index]);
}

export function ScriptStudioPage() {
  const { scriptId } = useParams<{ scriptId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { notifyError, notifySuccess } = useNotifications();

  const tab = (searchParams.get('tab') as StudioTab) || 'overview';

  const [script, setScript] = useState<ScriptDetail | null>(null);
  const [releases, setReleases] = useState<import('../../../api/scripts/types').ReleaseSummary[]>([]);
  const [selectedReleaseId, setSelectedReleaseId] = useState<string | null>(null);
  const [releaseSourceCode, setReleaseSourceCode] = useState<string | null>(null);
  const [releaseSourceOpen, setReleaseSourceOpen] = useState(false);
  const [releaseSourceLoading, setReleaseSourceLoading] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState(0);
  const [hostPatterns, setHostPatterns] = useState<string[]>([]);

  const [publishOpen, setPublishOpen] = useState(false);
  const [publishBusy, setPublishBusy] = useState(false);
  const [promoteChannel, setPromoteChannel] = useState<ChannelSummary | null>(null);
  const [promoteReleaseId, setPromoteReleaseId] = useState('');
  const [promotionSelections, setPromotionSelections] = useState<Record<string, string>>({});
  const [promoteBusy, setPromoteBusy] = useState(false);
  const [customChannelOpen, setCustomChannelOpen] = useState(false);
  const [customChannelBusy, setCustomChannelBusy] = useState(false);
  const [deprecateTarget, setDeprecateTarget] = useState<ReleaseSummary | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ReleaseSummary | null>(null);

  const selectedRelease = useMemo(
    () => releases.find((release) => release.id === selectedReleaseId) ?? null,
    [releases, selectedReleaseId],
  );

  const latestVersion = releases[0]?.version ?? null;

  const isDirty = useMemo(() => {
    if (!script) return false;
    const savedDescription = script.description ?? '';
    return description !== savedDescription
      || priority !== script.priority
      || !arraysEqual(hostPatterns, script.hostPatterns);
  }, [script, description, priority, hostPatterns]);

  useEffect(() => {
    if (releases.length === 0 || !script) return;

    setPromotionSelections((current) => {
      const next = { ...current };
      for (const channel of script.channels) {
        if (!next[channel.routeValue]) {
          next[channel.routeValue] = releases[0]?.id ?? channel.currentReleaseId ?? '';
        }
      }
      return next;
    });
  }, [releases, script]);

  function openPromoteDrawer(channel: ChannelSummary) {
    setPromoteChannel(channel);
    setPromoteReleaseId(
      promotionSelections[channel.routeValue]
      ?? releases[0]?.id
      ?? channel.currentReleaseId
      ?? '',
    );
  }

  const reload = useCallback(async () => {
    if (!scriptId) return;
    setLoading(true);

    const [detailResult, releasesResult] = await Promise.all([
      getScript(scriptId),
      listReleases(scriptId),
    ]);

    setLoading(false);

    if (!detailResult.ok) {
      notifyError(detailResult.error);
      setScript(null);
      return;
    }

    const detail = detailResult.data!;
    setScript(detail);
    setDescription(detail.description ?? '');
    setPriority(detail.priority);
    setHostPatterns([...detail.hostPatterns]);

    if (releasesResult.ok) {
      const items = (releasesResult.data?.items ?? []).map((item) => ({
        ...item,
        promotedChannelRouteValues: item.promotedChannelRouteValues ?? [],
      }));
      setReleases(items);
      setSelectedReleaseId((current) => current ?? items[0]?.id ?? null);
    }
  }, [notifyError, scriptId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    setReleaseSourceCode(null);
    setReleaseSourceOpen(false);
    setReleaseSourceLoading(false);
  }, [selectedReleaseId]);

  function setTab(next: StudioTab) {
    setSearchParams(next === 'overview' ? {} : { tab: next });
  }

  async function handleSaveOverview() {
    if (!scriptId) return;
    setSaving(true);
    const result = await updateScript(scriptId, {
      description: description.trim() || null,
      priority,
      hostPatterns,
    });
    setSaving(false);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess('Script atualizado.');
    setScript(result.data);
  }

  async function handlePublish(payload: {
    sourceCode: string;
    major?: number;
    minor?: number;
    patch?: number;
  }) {
    if (!scriptId) return;
    setPublishBusy(true);
    const result = await publishRelease(scriptId, payload);
    setPublishBusy(false);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess(`Release ${result.data!.version} publicado.`);
    setPublishOpen(false);
    setSelectedReleaseId(result.data!.id);
    await reload();
    setTab('releases');
  }

  async function handlePromote() {
    if (!scriptId || !promoteChannel || !promoteReleaseId) return;
    setPromoteBusy(true);
    const result = await promoteRelease(scriptId, promoteChannel.routeValue, promoteReleaseId);
    setPromoteBusy(false);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess(`Release promovido em ${promoteChannel.displayName}.`);
    setPromoteChannel(null);
    await reload();
  }

  async function handleAddChannel(name: string) {
    if (!scriptId) return;
    setCustomChannelBusy(true);
    const result = await addCustomChannel(scriptId, name);
    setCustomChannelBusy(false);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess('Canal adicionado.');
    setCustomChannelOpen(false);
    await reload();
  }

  async function handleDeprecateConfirm() {
    if (!scriptId || !deprecateTarget) return;
    const action = deprecateTarget.isDeprecated ? restoreRelease : deprecateRelease;
    const result = await action(scriptId, deprecateTarget.id);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    notifySuccess(deprecateTarget.isDeprecated ? 'Release restaurado.' : 'Release deprecado.');
    setDeprecateTarget(null);
    await reload();
  }

  async function handleDeleteConfirm() {
    if (!scriptId || !deleteTarget) return;
    const result = await deleteRelease(scriptId, deleteTarget.id);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    const cleared = result.data?.clearedChannelRouteValues ?? [];
    const clearedMessage = cleared.length > 0
      ? ` Ponteiros removidos em: ${cleared.join(', ')}.`
      : '';

    notifySuccess(`Release ${deleteTarget.version} excluído.${clearedMessage}`);
    setDeleteTarget(null);
    setSelectedReleaseId(null);
    setReleaseSourceCode(null);
    setReleaseSourceOpen(false);
    await reload();
  }

  async function handleLoadReleaseSourceCode() {
    if (!scriptId || !selectedReleaseId) return;
    setReleaseSourceLoading(true);
    const result = await getReleaseSourceCode(scriptId, selectedReleaseId);
    setReleaseSourceLoading(false);

    if (!result.ok) {
      notifyError(result.error);
      return;
    }

    setReleaseSourceCode(result.data!.sourceCode);
    setReleaseSourceOpen(true);
  }

  if (!scriptId) {
    navigate(SCRIPTS_ADMIN_LIST_PATH);
    return null;
  }

  if (loading && !script) {
    return (
      <div className="ops-page scripts-studio">
        <div className="scripts-studio__skeleton scripts-studio__hero" aria-hidden="true" />
        <div className="scripts-studio__skeleton scripts-studio__tabs" aria-hidden="true" />
        <div className="scripts-studio__skeleton scripts-studio__panel" aria-hidden="true" />
        <p className="muted scripts-skeleton">Carregando studio…</p>
      </div>
    );
  }

  if (!script) {
    return (
      <div className="ops-page scripts-studio">
        <EmptyState title="Script não encontrado" message="Volte ao inventário e tente novamente." />
        <Link className="btn btn-ghost" to={SCRIPTS_ADMIN_LIST_PATH}>← Inventário</Link>
      </div>
    );
  }

  return (
    <div className="ops-page scripts-studio">
      <header className="scripts-studio__hero">
        <div className="scripts-studio__hero-main">
          <Link className="scripts-studio__back" to={SCRIPTS_ADMIN_LIST_PATH}>
            ← Inventário
          </Link>
          <p className="scripts-studio__kicker">Admin · Script Studio</p>
          <div className="scripts-studio__title-row">
            <h1>{script.name}</h1>
            <span className="scripts-priority" title="Menor prioridade injeta primeiro">P{script.priority}</span>
            <ResolutionModeBadge hostPatterns={hostPatterns} />
          </div>
          {hostPatterns.length > 0 ? (
            <HostPatternChips patterns={hostPatterns} />
          ) : (
            <p className="scripts-studio__resolve-hint muted small">
              Resolve via <code>GET /scripts?name={script.name}</code>
            </p>
          )}
        </div>

        <div className="scripts-studio__actions">
          <button
            type="button"
            className="btn btn-scripts-outline"
            onClick={() => setPublishOpen(true)}
          >
            Publicar release
          </button>
          <button
            type="button"
            className="btn btn-scripts-accent"
            onClick={() => setTab('channels')}
          >
            Promover
          </button>
        </div>
      </header>

      <nav className="scripts-tabs" aria-label="Seções do studio">
        {(['overview', 'releases', 'channels'] as const).map((item) => (
          <button
            key={item}
            type="button"
            className={`scripts-tabs__item ${tab === item ? 'is-active' : ''}`}
            onClick={() => setTab(item)}
            aria-current={tab === item ? 'page' : undefined}
          >
            {TAB_LABELS[item]}
          </button>
        ))}
      </nav>

      <div className="scripts-studio__overview">
        <ReleaseStudioOverview releases={releases} channels={script.channels} />
      </div>

      <div className="scripts-studio__content">
        {tab === 'overview' ? (
          <div className="scripts-studio-grid">
            <ScriptMetadataPanel
              scriptName={script.name}
              description={description}
              priority={priority}
              hostPatterns={hostPatterns}
              saving={saving}
              isDirty={isDirty}
              onDescriptionChange={setDescription}
              onPriorityChange={setPriority}
              onHostPatternsChange={setHostPatterns}
              onSave={() => void handleSaveOverview()}
            />

            <section className="scripts-panel scripts-panel--channels">
              <header className="scripts-panel__head scripts-channels-panel__head">
                <div>
                  <h2>Canais</h2>
                  <p className="scripts-panel__sub muted small">Versão promovida em cada ambiente.</p>
                </div>
              </header>
              <div className="scripts-panel__body scripts-channels-panel__body">
                <ChannelMatrix
                  channels={script.channels}
                  releases={releases}
                  onPromote={openPromoteDrawer}
                  compact
                />
              </div>
            </section>

            {hostPatterns.length > 0 ? (
              <section className="scripts-panel scripts-panel--full scripts-impact-callout">
                <header className="scripts-panel__head">
                  <h2>Impacto em produção</h2>
                </header>
                <div className="scripts-panel__body scripts-impact-callout__body">
                  <p className="muted small">
                    Clientes com hosts correspondentes recebem o release promovido em{' '}
                    <code>prod</code> via:
                  </p>
                  <code className="scripts-impact-callout__code">GET /scripts?host=…&channel=prod</code>
                  <HostPatternChips patterns={hostPatterns} />
                </div>
              </section>
            ) : null}
          </div>
        ) : null}

        {tab === 'releases' ? (
          <div className="scripts-releases-layout">
            <aside className="scripts-panel scripts-releases-layout__timeline">
              <header className="scripts-panel__head scripts-panel__head--compact scripts-releases-layout__toolbar">
                <h2>Histórico</h2>
                <button type="button" className="btn btn-scripts-outline btn-sm" onClick={() => setPublishOpen(true)}>
                  + Nova release
                </button>
              </header>
              <div className="scripts-panel__body scripts-panel__body--flush-timeline">
                <ReleaseTimeline
                  releases={releases}
                  selectedId={selectedReleaseId}
                  onSelect={setSelectedReleaseId}
                />
              </div>
            </aside>

            <section className="scripts-panel scripts-releases-layout__editor">
              {selectedRelease ? (
                <ReleaseInspectorPanel
                  release={selectedRelease}
                  script={script}
                  sourceCode={releaseSourceCode}
                  sourceCodeOpen={releaseSourceOpen}
                  sourceCodeLoading={releaseSourceLoading}
                  onLoadSourceCode={() => void handleLoadReleaseSourceCode()}
                  onCloseSourceCode={() => {
                    setReleaseSourceOpen(false);
                    setReleaseSourceCode(null);
                  }}
                  onCopyHash={() => navigator.clipboard.writeText(selectedRelease.hash)}
                  onDeprecate={() => setDeprecateTarget(selectedRelease)}
                  onDelete={() => setDeleteTarget(selectedRelease)}
                />
              ) : (
                <div className="scripts-panel__body">
                  <EmptyState
                    title="Selecione um release"
                    message="Escolha um item no histórico para ver metadados, canais e estado de promoção."
                  />
                </div>
              )}
            </section>
          </div>
        ) : null}

        {tab === 'channels' ? (
          <section className="scripts-panel scripts-panel--full scripts-promo-panel">
            <header className="scripts-panel__head scripts-panel__head--compact scripts-promo-panel__head">
              <h2>Promoção</h2>
              <button type="button" className="btn btn-scripts-outline btn-sm" onClick={() => setCustomChannelOpen(true)}>
                + Canal
              </button>
            </header>

            <div className="scripts-panel__body scripts-promo-panel__body">
              <ChannelMatrix
                channels={script.channels}
                releases={releases}
                promotionSelections={promotionSelections}
                onPromotionSelect={(routeValue, releaseId) => {
                  setPromotionSelections((current) => ({ ...current, [routeValue]: releaseId }));
                }}
                onPromote={openPromoteDrawer}
              />

              {script.hostPatterns.length > 0 ? (
                <div className="scripts-promo-panel__hosts muted small">
                  <span>Hosts:</span>
                  {script.hostPatterns.map((host) => (
                    <code key={host}>{host}</code>
                  ))}
                </div>
              ) : null}

              <p className="scripts-promo-panel__cache muted small">
                <strong>Cache L1 (~60s)</strong>
                {' — '}
                promoções invalidam o ScriptCache; clientes podem ver a versão anterior por até 1 minuto.
              </p>
            </div>
          </section>
        ) : null}
      </div>

      <PublishReleaseDrawer
        open={publishOpen}
        busy={publishBusy}
        scriptName={script.name}
        latestVersion={latestVersion}
        onClose={() => setPublishOpen(false)}
        onSubmit={handlePublish}
      />

      <PromoteChannelDrawer
        open={promoteChannel !== null}
        busy={promoteBusy}
        scriptName={script.name}
        hostPatterns={script.hostPatterns}
        channel={promoteChannel}
        releases={releases}
        selectedReleaseId={promoteReleaseId}
        onSelectRelease={(releaseId) => {
          setPromoteReleaseId(releaseId);
          if (promoteChannel) {
            setPromotionSelections((current) => ({ ...current, [promoteChannel.routeValue]: releaseId }));
          }
        }}
        onClose={() => setPromoteChannel(null)}
        onConfirm={handlePromote}
      />

      <AddCustomChannelModal
        open={customChannelOpen}
        busy={customChannelBusy}
        onClose={() => setCustomChannelOpen(false)}
        onSubmit={handleAddChannel}
      />

      <ConfirmDialog
        open={deprecateTarget !== null}
        title={deprecateTarget?.isDeprecated ? 'Restaurar release?' : 'Deprecar release?'}
        message={
          deprecateTarget?.isDeprecated
            ? 'O release voltará a ser elegível no resolve público (salvo filtro allowDeprecated).'
            : 'Releases deprecados são omitidos do GET /scripts por padrão.'
        }
        onCancel={() => setDeprecateTarget(null)}
        onConfirm={handleDeprecateConfirm}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Excluir release?"
        message={
          deleteTarget
            ? `A versão ${deleteTarget.version} será removida permanentemente. Canais que apontam para este release terão o ponteiro anulado automaticamente.`
            : ''
        }
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDeleteConfirm}
      />
    </div>
  );
}
