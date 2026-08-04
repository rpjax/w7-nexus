import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { ScriptSummary } from '../../api/scripts/types';
import { formatRelativeTime } from '../../features/scripts/formatRelativeTime';
import { scriptStudioPath } from '../../features/scripts/scriptPaths';
import { channelToneClass } from '@/lib/channel-tones';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { ChannelVersionPills } from './ChannelVersionPills';
import { HostPatternChips } from './HostPatternChips';
import { ResolutionModeBadge } from './ResolutionModeBadge';

type ScriptCardProps = {
  script: ScriptSummary;
};

const DESCRIPTION_CLAMP_THRESHOLD = 140;

function prodVersion(script: ScriptSummary): string | null {
  return script.channels.find((channel) => channel.routeValue === 'prod')?.version ?? null;
}

export function ScriptCard({ script }: ScriptCardProps) {
  const studioPath = scriptStudioPath(script.id);
  const hasHosts = script.hostPatterns.length > 0;
  const liveProd = prodVersion(script);
  const [descriptionExpanded, setDescriptionExpanded] = useState(false);
  const canExpandDescription = (script.description?.length ?? 0) > DESCRIPTION_CLAMP_THRESHOLD;

  return (
    <Card className="border-border/60 bg-card/80 transition-colors hover:border-warning/35 hover:shadow-lg">
      <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 pb-2">
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-semibold leading-tight">
              <Link className="text-foreground hover:text-warning" to={studioPath}>
                {script.name}
              </Link>
            </h3>
            <Badge
              variant="outline"
              className={cn(channelToneClass('accent'), 'font-mono text-xs font-normal')}
              title="Menor prioridade injeta primeiro"
            >
              P{script.priority}
            </Badge>
            {liveProd ? (
              <Badge
                variant="outline"
                className={cn(channelToneClass('prod'), 'font-mono text-xs font-normal')}
                title="Versão em produção"
              >
                prod · {liveProd}
              </Badge>
            ) : null}
          </div>
          <ResolutionModeBadge hostPatterns={script.hostPatterns} />
        </div>
        <Button variant="outline" size="sm" className="shrink-0" asChild>
          <Link to={studioPath}>Abrir studio</Link>
        </Button>
      </CardHeader>

      <CardContent className="flex flex-col gap-2.5 pt-0">
        {!hasHosts ? (
          <p className="font-mono text-xs text-muted-foreground">
            GET /scripts?name={script.name}
          </p>
        ) : null}

        {script.description ? (
          <div className="flex flex-col gap-1">
            <p
              className={cn(
                'text-sm text-muted-foreground',
                !descriptionExpanded && 'line-clamp-2',
              )}
              title={!descriptionExpanded ? script.description : undefined}
            >
              {script.description}
            </p>
            {canExpandDescription ? (
              <Button
                type="button"
                variant="link"
                size="sm"
                className="h-auto self-start px-0 text-xs"
                onClick={() => setDescriptionExpanded((open) => !open)}
              >
                {descriptionExpanded ? 'Menos' : 'Ver descrição'}
              </Button>
            ) : null}
          </div>
        ) : null}

        {hasHosts ? <HostPatternChips patterns={script.hostPatterns} /> : null}

        {script.channels.length > 0 ? (
          <div className="flex flex-col gap-1.5">
            <span className="text-xs text-muted-foreground">Canais</span>
            <ChannelVersionPills channels={script.channels} compact />
          </div>
        ) : null}
      </CardContent>

      <CardFooter className="border-t border-border/40 pt-3 text-xs text-muted-foreground">
        Atualizado {formatRelativeTime(script.updatedAt)}
      </CardFooter>
    </Card>
  );
}
