import type { ChannelSummary } from '../../api/scripts/types';
import { channelToneClass, channelToneFromRoute } from '@/lib/channel-tones';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

type ChannelVersionPillsProps = {
  channels: ChannelSummary[];
  compact?: boolean;
};

const COMPACT_LABEL: Record<string, string> = {
  prod: 'prod',
  staging: 'staging',
  development: 'dev',
};

export function ChannelVersionPills({ channels, compact = false }: ChannelVersionPillsProps) {
  const defaults = ['prod', 'staging', 'development'];
  const ordered = [
    ...channels.filter((c) => defaults.includes(c.routeValue)),
    ...channels.filter((c) => c.isCustom),
  ];

  return (
    <div className={cn('flex flex-wrap gap-1.5', compact && 'gap-1')}>
      {ordered.map((channel) => {
        const toneClass = channelToneClass(channelToneFromRoute(channel.routeValue));
        const label = compact
          ? (COMPACT_LABEL[channel.routeValue] ?? channel.routeValue)
          : channel.displayName;
        const hasVersion = Boolean(channel.version);
        const versionTitle = hasVersion
          ? `${channel.version}${channel.hash ? ` · ${channel.hash}` : ''}`
          : 'Nenhum release promovido neste canal';

        return (
          <Badge
            key={channel.routeValue}
            variant="outline"
            title={versionTitle}
            className={cn(
              'gap-1 font-mono text-xs font-normal',
              toneClass,
              !hasVersion && 'opacity-60',
            )}
          >
            <span>{label}</span>
            {hasVersion ? (
              <>
                <span className="opacity-50" aria-hidden="true">·</span>
                <span>{channel.version}</span>
              </>
            ) : (
              <span className="opacity-70">sem release</span>
            )}
            {channel.isDeprecated ? (
              <span className="text-destructive">dep</span>
            ) : null}
          </Badge>
        );
      })}
    </div>
  );
}
