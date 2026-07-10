import type { ChannelSummary } from '../../api/scripts/types';

type ChannelVersionPillsProps = {
  channels: ChannelSummary[];
  compact?: boolean;
};

const CHANNEL_CLASS: Record<string, string> = {
  prod: 'scripts-pill--prod',
  staging: 'scripts-pill--staging',
  development: 'scripts-pill--dev',
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
    <div className={`scripts-channel-pills ${compact ? 'scripts-channel-pills--compact' : ''}`}>
      {ordered.map((channel) => {
        const cls = CHANNEL_CLASS[channel.routeValue] ?? 'scripts-pill--custom';
        const label = compact
          ? (COMPACT_LABEL[channel.routeValue] ?? channel.routeValue)
          : channel.displayName;
        const hasVersion = Boolean(channel.version);
        const versionTitle = hasVersion
          ? `${channel.version}${channel.hash ? ` · ${channel.hash}` : ''}`
          : 'Nenhum release promovido neste canal';

        return (
          <span
            key={channel.routeValue}
            className={`scripts-pill ${cls} ${hasVersion ? 'scripts-pill--filled' : 'scripts-pill--empty'}`.trim()}
            title={versionTitle}
          >
            <span className="scripts-pill__label">{label}</span>
            {hasVersion ? (
              <>
                <span className="scripts-pill__dot" aria-hidden="true" />
                <span className="scripts-pill__version">{channel.version}</span>
              </>
            ) : (
              <span className="scripts-pill__empty">sem release</span>
            )}
            {channel.isDeprecated ? <span className="scripts-pill__warn">dep</span> : null}
          </span>
        );
      })}
    </div>
  );
}
