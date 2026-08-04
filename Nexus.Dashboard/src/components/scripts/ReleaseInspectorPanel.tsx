import type { ReactNode } from 'react';
import type { ReleaseSummary, ScriptDetail } from '../../api/scripts/types';
import { formatRelativeTime } from '../../features/scripts/formatRelativeTime';
import { formatScriptFileSize } from '../../features/scripts/readScriptFile';
import { channelToneClass, channelToneFromRoute } from '@/lib/channel-tones';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { ReleaseSourceReader } from './ReleaseSourceReader';

type ReleaseInspectorPanelProps = {
  release: ReleaseSummary;
  script: ScriptDetail;
  sourceCode: string | null;
  sourceCodeOpen: boolean;
  sourceCodeLoading: boolean;
  onLoadSourceCode: () => void;
  onCloseSourceCode: () => void;
  onCopyHash: () => void;
  onDeprecate: () => void;
  onDelete: () => void;
};

export function ReleaseInspectorPanel({
  release,
  script,
  sourceCode,
  sourceCodeOpen,
  sourceCodeLoading,
  onLoadSourceCode,
  onCloseSourceCode,
  onCopyHash,
  onDeprecate,
  onDelete,
}: ReleaseInspectorPanelProps) {
  const prodChannel = script.channels.find((channel) => channel.routeValue === 'prod');
  const isLiveInProd = prodChannel?.currentReleaseId === release.id;
  const promoted = release.promotedChannelRouteValues;

  return (
    <div className="flex min-h-0 flex-col">
      <header className="flex flex-wrap items-start justify-between gap-3 border-b border-border/50 px-4 py-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="font-mono text-lg font-semibold">{release.version}</h2>
            {release.isDeprecated ? (
              <Badge variant="destructive">Deprecated</Badge>
            ) : isLiveInProd ? (
              <Badge variant="outline" className={channelToneClass('prod')}>
                Live em prod
              </Badge>
            ) : promoted.length > 0 ? (
              <span className="flex flex-wrap gap-1">
                {promoted.map((route) => (
                  <Badge
                    key={route}
                    variant="outline"
                    className={cn(
                      'font-mono text-xs font-normal',
                      channelToneClass(channelToneFromRoute(route)),
                    )}
                  >
                    {route}
                  </Badge>
                ))}
              </span>
            ) : (
              <span className="text-xs text-muted-foreground">Sem promoção</span>
            )}
          </div>
          <p className="mt-1 text-xs text-muted-foreground">
            Publicado {formatRelativeTime(release.createdAt)}
          </p>
        </div>

        <div className="flex shrink-0 gap-1">
          <Button type="button" variant="ghost" size="sm" onClick={onDeprecate}>
            {release.isDeprecated ? 'Restaurar' : 'Deprecar'}
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="text-destructive hover:text-destructive"
            onClick={onDelete}
          >
            Excluir
          </Button>
        </div>
      </header>

      <CardContent className="flex flex-col gap-4 pt-4">
        <dl className="grid gap-3 sm:grid-cols-2">
          <MetaItem label="SHA-256" className="sm:col-span-2">
            <div className="flex flex-wrap items-center gap-2">
              <code className="break-all font-mono text-xs">{release.hash}</code>
              <Button type="button" variant="ghost" size="sm" onClick={onCopyHash}>
                Copiar
              </Button>
            </div>
          </MetaItem>

          <MetaItem label="Tamanho">
            <span title={`${release.sourceCodeSizeBytes.toLocaleString('pt-BR')} bytes`}>
              {formatScriptFileSize(release.sourceCodeSizeBytes)}
            </span>
          </MetaItem>

          <MetaItem label="ID">
            <span className="break-all font-mono text-xs">{release.id}</span>
          </MetaItem>

          <MetaItem label="Resolve">
            {release.isDeprecated ? (
              <Badge variant="secondary">Oculto</Badge>
            ) : (
              <Badge variant="success">Público</Badge>
            )}
          </MetaItem>
        </dl>

        <ReleaseSourceReader
          version={release.version}
          sourceCode={sourceCode}
          sizeBytes={release.sourceCodeSizeBytes}
          open={sourceCodeOpen}
          loading={sourceCodeLoading}
          onOpen={onLoadSourceCode}
          onClose={onCloseSourceCode}
        />
      </CardContent>
    </div>
  );
}

function MetaItem({
  label,
  children,
  className,
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn('flex flex-col gap-1', className)}>
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm">{children}</dd>
    </div>
  );
}
