import type { ReleaseSummary } from '../../api/scripts/types';
import { formatRelativeTime } from '../../features/scripts/formatRelativeTime';
import { formatScriptFileSize } from '../../features/scripts/readScriptFile';
import { channelToneClass, channelToneFromRoute } from '@/lib/channel-tones';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

type ReleaseTimelineProps = {
  releases: ReleaseSummary[];
  selectedId: string | null;
  onSelect: (releaseId: string) => void;
};

export function ReleaseTimeline({
  releases,
  selectedId,
  onSelect,
}: ReleaseTimelineProps) {
  if (releases.length === 0) {
    return <p className="px-4 py-6 text-sm text-muted-foreground">Nenhum release publicado ainda.</p>;
  }

  return (
    <div className="flex flex-col" role="listbox" aria-label="Histórico de releases">
      {releases.map((release, index) => {
        const isSelected = release.id === selectedId;
        const isLast = index === releases.length - 1;

        return (
          <Button
            key={release.id}
            type="button"
            variant="ghost"
            role="option"
            aria-selected={isSelected}
            className={cn(
              'h-auto w-full justify-start gap-3 rounded-none px-4 py-3',
              isSelected && 'bg-warning/8 hover:bg-warning/10',
            )}
            onClick={() => onSelect(release.id)}
          >
            <div className="flex w-4 shrink-0 flex-col items-center pt-1" aria-hidden="true">
              <span
                className={cn(
                  'size-2.5 rounded-full border-2',
                  isSelected
                    ? 'border-warning bg-warning'
                    : 'border-muted-foreground/40 bg-transparent',
                )}
              />
              {!isLast ? <span className="mt-1 w-px flex-1 bg-border/60" /> : null}
            </div>

            <div className="min-w-0 flex-1 text-left">
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="font-mono text-sm font-medium">{release.version}</span>
                {release.promotedChannelRouteValues.map((route) => (
                  <Badge
                    key={route}
                    variant="outline"
                    className={cn(
                      'font-mono text-[0.65rem] font-normal',
                      channelToneClass(channelToneFromRoute(route)),
                    )}
                  >
                    {route}
                  </Badge>
                ))}
                {release.isDeprecated ? (
                  <Badge variant="destructive" className="text-[0.65rem] font-normal">
                    Deprecated
                  </Badge>
                ) : null}
              </div>

              <div className="mt-1 flex items-center gap-1.5 text-xs text-muted-foreground">
                <span>
                  {release.sourceCodeSizeBytes > 0
                    ? formatScriptFileSize(release.sourceCodeSizeBytes)
                    : '—'}
                </span>
                <span aria-hidden="true">·</span>
                <span>{formatRelativeTime(release.createdAt)}</span>
              </div>
            </div>
          </Button>
        );
      })}
    </div>
  );
}
