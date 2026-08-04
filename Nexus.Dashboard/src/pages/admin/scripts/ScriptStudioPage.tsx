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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
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
import { channelToneClass } from '@/lib/channel-tones';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';

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
      <div className="flex flex-col gap-4 p-4">
        <Skeleton className="h-28 w-full" aria-hidden="true" />
        <Skeleton className="h-10 w-72" aria-hidden="true" />
        <Skeleton className="h-64 w-full" aria-hidden="true" />
        <p className="text-sm text-muted-foreground">Carregando studio…</p>
      </div>
    );
  }

  if (!script) {
    return (
      <div className="flex flex-col gap-4 p-4">
        <Alert>
          <AlertTitle>Script não encontrado</AlertTitle>
          <AlertDescription>Volte ao inventário e tente novamente.</AlertDescription>
        </Alert>
        <Button variant="ghost" asChild>
          <Link to={SCRIPTS_ADMIN_LIST_PATH}>← Inventário</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4 p-4">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <Button variant="ghost" size="sm" className="mb-2 -ml-2 h-auto px-2 py-1" asChild>
            <Link to={SCRIPTS_ADMIN_LIST_PATH}>← Inventário</Link>
          </Button>
          <p className="text-xs uppercase tracking-wide text-muted-foreground">Admin · Script Studio</p>
          <div className="mt-1 flex flex-wrap items-center gap-2">
            <h1 className="text-2xl font-semibold">{script.name}</h1>
            <Badge
              variant="outline"
              className={cn(channelToneClass('accent'), 'font-mono text-xs font-normal')}
              title="Menor prioridade injeta primeiro"
            >
              P{script.priority}
            </Badge>
            <ResolutionModeBadge hostPatterns={hostPatterns} />
          </div>
          {hostPatterns.length > 0 ? (
            <div className="mt-2">
              <HostPatternChips patterns={hostPatterns} />
            </div>
          ) : (
            <p className="mt-2 text-xs text-muted-foreground">
              Resolve via <code className="font-mono">GET /scripts?name={script.name}</code>
            </p>
          )}
        </div>

        <div className="flex shrink-0 gap-2">
          <Button type="button" variant="outline" onClick={() => setPublishOpen(true)}>
            Publicar release
          </Button>
          <Button
            type="button"
            variant="secondary"
            className={channelToneClass('accent', 'md')}
            onClick={() => setTab('channels')}
          >
            Promover
          </Button>
        </div>
      </header>

      <Tabs value={tab} onValueChange={(value) => setTab(value as StudioTab)}>
        <TabsList aria-label="Seções do studio">
          {(['overview', 'releases', 'channels'] as const).map((item) => (
            <TabsTrigger key={item} value={item}>
              {TAB_LABELS[item]}
            </TabsTrigger>
          ))}
        </TabsList>

        <div className="mt-3">
          <ReleaseStudioOverview releases={releases} channels={script.channels} />
        </div>

        <TabsContent value="overview" className="mt-4">
          <div className="grid gap-4 lg:grid-cols-2">
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

            <Card className="border-border/60">
              <CardHeader>
                <CardTitle>Canais</CardTitle>
                <CardDescription>Versão promovida em cada ambiente.</CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <ChannelMatrix
                  channels={script.channels}
                  releases={releases}
                  onPromote={openPromoteDrawer}
                  compact
                />
              </CardContent>
            </Card>

            {hostPatterns.length > 0 ? (
              <Card className={cn('lg:col-span-2', channelToneClass('prod', 'md'))}>
                <CardHeader>
                  <CardTitle>Impacto em produção</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-2 pt-0">
                  <p className="text-xs text-muted-foreground">
                    Clientes com hosts correspondentes recebem o release promovido em{' '}
                    <code className="font-mono">prod</code> via:
                  </p>
                  <code className="font-mono text-sm">GET /scripts?host=…&channel=prod</code>
                  <HostPatternChips patterns={hostPatterns} />
                </CardContent>
              </Card>
            ) : null}
          </div>
        </TabsContent>

        <TabsContent value="releases" className="mt-4">
          <div className="grid gap-4 lg:grid-cols-[minmax(0,320px)_1fr]">
            <Card className="overflow-hidden border-border/60 py-0">
              <CardHeader className="flex-row items-center justify-between space-y-0 border-b border-border/50 py-3">
                <CardTitle className="text-base">Histórico</CardTitle>
                <CardAction>
                  <Button type="button" variant="outline" size="sm" onClick={() => setPublishOpen(true)}>
                    + Nova release
                  </Button>
                </CardAction>
              </CardHeader>
              <CardContent className="p-0">
                <ReleaseTimeline
                  releases={releases}
                  selectedId={selectedReleaseId}
                  onSelect={setSelectedReleaseId}
                />
              </CardContent>
            </Card>

            <Card className="border-border/60 py-0">
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
                <CardContent className="py-8">
                  <Alert>
                    <AlertTitle>Selecione um release</AlertTitle>
                    <AlertDescription>
                      Escolha um item no histórico para ver metadados, canais e estado de promoção.
                    </AlertDescription>
                  </Alert>
                </CardContent>
              )}
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="channels" className="mt-4">
          <Card className="border-border/60">
            <CardHeader className="flex-row items-center justify-between space-y-0">
              <CardTitle>Promoção</CardTitle>
              <CardAction>
                <Button type="button" variant="outline" size="sm" onClick={() => setCustomChannelOpen(true)}>
                  + Canal
                </Button>
              </CardAction>
            </CardHeader>

            <CardContent className="flex flex-col gap-4 pt-0">
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
                <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                  <span>Hosts:</span>
                  {script.hostPatterns.map((host) => (
                    <code key={host} className="font-mono">{host}</code>
                  ))}
                </div>
              ) : null}

              <p className="text-xs text-muted-foreground">
                <strong>Cache L1 (~60s)</strong>
                {' — '}
                promoções invalidam o ScriptCache; clientes podem ver a versão anterior por até 1 minuto.
              </p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <PublishReleaseDrawer
        open={publishOpen}
        busy={publishBusy}
        scriptName={script.name}
        releases={releases}
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

      <AlertDialog open={deprecateTarget !== null} onOpenChange={(open) => { if (!open) setDeprecateTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{deprecateTarget?.isDeprecated ? 'Restaurar release?' : 'Deprecar release?'}</AlertDialogTitle>
            <AlertDialogDescription>
              {deprecateTarget?.isDeprecated
                ? 'O release voltará a ser elegível no resolve público (salvo filtro allowDeprecated).'
                : 'Releases deprecados são omitidos do GET /scripts por padrão.'}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={handleDeprecateConfirm}>Confirmar</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={deleteTarget !== null} onOpenChange={(open) => { if (!open) setDeleteTarget(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Excluir release?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `A versão ${deleteTarget.version} será removida permanentemente. Canais que apontam para este release terão o ponteiro anulado automaticamente.`
                : ''}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={handleDeleteConfirm}>Confirmar</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
