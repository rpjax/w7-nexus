import type { ChannelSummary, ReleaseSummary } from '../../api/scripts/types';
import { truncateHash } from '../../features/scripts/formatRelativeTime';

type ChannelMatrixProps = {
  channels: ChannelSummary[];
  releases?: ReleaseSummary[];
  promotionSelections?: Record<string, string>;
  onPromotionSelect?: (channelRouteValue: string, releaseId: string) => void;
  onPromote?: (channel: ChannelSummary) => void;
  /** Denser rows for the studio overview panel. */
  compact?: boolean;
};

const ROUTE_CLASS: Record<string, string> = {
  prod: 'scripts-matrix__row--prod',
  staging: 'scripts-matrix__row--staging',
  development: 'scripts-matrix__row--dev',
};

const DEFAULT_CHANNEL_ORDER = ['prod', 'staging', 'development'];

function orderChannels(channels: ChannelSummary[]): ChannelSummary[] {
  return [
    ...DEFAULT_CHANNEL_ORDER.flatMap((route) => channels.filter((channel) => channel.routeValue === route)),
    ...channels.filter((channel) => !DEFAULT_CHANNEL_ORDER.includes(channel.routeValue)),
  ];
}

function ChannelIdentity({ channel, compact }: { channel: ChannelSummary; compact: boolean }) {
  if (compact) {
    return (
      <div className="scripts-matrix__channel scripts-matrix__channel--overview">
        <div className="scripts-matrix__channel-main">
          <span className="scripts-matrix__channel-dot" aria-hidden="true" />
          <strong className="scripts-matrix__channel-name">{channel.displayName}</strong>
        </div>
        <span className="scripts-matrix__route mono">{channel.routeValue}</span>
      </div>
    );
  }

  return (
    <div className="scripts-matrix__channel">
      <div className="scripts-matrix__channel-main">
        <span className="scripts-matrix__channel-dot" aria-hidden="true" />
        <strong>{channel.displayName}</strong>
      </div>
      <span className="scripts-matrix__route mono">{channel.routeValue}</span>
    </div>
  );
}

function CurrentRelease({ channel, compact }: { channel: ChannelSummary; compact: boolean }) {
  if (!channel.version) {
    return (
      <span className="scripts-matrix__empty" aria-label="Sem release">
        Sem release
      </span>
    );
  }

  if (compact) {
    return (
      <div className="scripts-matrix__release-stack">
        <div className="scripts-matrix__release-main">
          <span className="scripts-matrix__version mono">{channel.version}</span>
          {channel.isDeprecated ? (
            <span className="scripts-badge scripts-badge--deprecated">Deprecated</span>
          ) : null}
        </div>
        {channel.hash ? (
          <span className="scripts-matrix__hash muted small mono" title={channel.hash}>
            {truncateHash(channel.hash)}
          </span>
        ) : null}
      </div>
    );
  }

  return (
    <>
      <div className="scripts-matrix__release-main">
        <span className="scripts-matrix__version mono">{channel.version}</span>
        {channel.isDeprecated ? (
          <span className="scripts-badge scripts-badge--deprecated">Deprecated</span>
        ) : null}
      </div>
      {channel.hash ? (
        <span className="scripts-matrix__hash muted small mono" title={channel.hash}>
          {truncateHash(channel.hash)}
        </span>
      ) : null}
    </>
  );
}

export function ChannelMatrix({
  channels,
  releases,
  promotionSelections,
  onPromotionSelect,
  onPromote,
  compact = false,
}: ChannelMatrixProps) {
  const promotionMode = Boolean(releases?.length && onPromotionSelect);
  const canPromote = Boolean(onPromote && (releases?.length ?? 0) > 0);
  const orderedChannels = orderChannels(channels);

  if (promotionMode) {
    return (
      <div className="scripts-promo-stage">
        {orderedChannels.map((channel) => {
          const selectedReleaseId = promotionSelections?.[channel.routeValue] ?? releases?.[0]?.id ?? '';
          const selectedRelease = releases!.find((release) => release.id === selectedReleaseId) ?? null;
          const isCurrentSelection = Boolean(
            channel.currentReleaseId
            && selectedReleaseId
            && channel.currentReleaseId === selectedReleaseId,
          );

          return (
            <article
              key={channel.routeValue}
              className={`scripts-promo-card ${ROUTE_CLASS[channel.routeValue] ?? 'scripts-matrix__row--custom'}`}
            >
              <header className="scripts-promo-card__head">
                <div className="scripts-promo-card__identity">
                  <span className="scripts-matrix__channel-dot" aria-hidden="true" />
                  <span className="scripts-matrix__route mono">{channel.routeValue}</span>
                  <span className="scripts-promo-card__name">{channel.displayName}</span>
                </div>
                {isCurrentSelection ? (
                  <span className="scripts-matrix__status">Atual</span>
                ) : null}
              </header>

              <div className="scripts-promo-card__current">
                <span className="scripts-promo-card__label muted small">Em produção no canal</span>
                <div className="scripts-promo-card__current-value">
                  {channel.version ? (
                    <>
                      <span className="scripts-matrix__version mono">{channel.version}</span>
                      {channel.hash ? (
                        <span className="scripts-matrix__hash muted small mono" title={channel.hash}>
                          · {truncateHash(channel.hash)}
                        </span>
                      ) : null}
                    </>
                  ) : (
                    <span className="scripts-promo-card__none muted">Nenhum release</span>
                  )}
                </div>
              </div>

              <div className="scripts-promo-card__controls">
                <label className="scripts-promo-card__label muted small" htmlFor={`promo-${channel.routeValue}`}>
                  Promover para
                </label>
                <div className="scripts-promo-card__control-row">
                  <select
                    id={`promo-${channel.routeValue}`}
                    className="nexus-input scripts-matrix__select scripts-promo-card__select"
                    value={selectedReleaseId}
                    onChange={(e) => onPromotionSelect?.(channel.routeValue, e.target.value)}
                  >
                    {releases!.map((release) => (
                      <option key={release.id} value={release.id}>
                        {release.version}{release.isDeprecated ? ' (deprecated)' : ''}
                      </option>
                    ))}
                  </select>
                  {isCurrentSelection ? (
                    <span className="scripts-promo-card__noop muted small">Já nesta versão</span>
                  ) : (
                    <button
                      type="button"
                      className="btn btn-scripts-accent btn-sm scripts-promo-card__promote"
                      disabled={!canPromote || !selectedReleaseId}
                      title={
                        !canPromote
                          ? 'Publique um release antes de promover'
                          : `Promover ${selectedRelease?.version ?? 'release'} em ${channel.displayName}`
                      }
                      onClick={() => onPromote!(channel)}
                    >
                      Promover
                    </button>
                  )}
                </div>
              </div>
            </article>
          );
        })}

        {!canPromote ? (
          <p className="scripts-matrix__footnote muted small">
            Publique ao menos um release para habilitar promoções entre canais.
          </p>
        ) : null}
      </div>
    );
  }

  return (
    <div className={`scripts-matrix ${compact ? 'scripts-matrix--compact' : ''}`}>
      <div className="scripts-matrix__header muted small" aria-hidden={compact ? undefined : true}>
        <span>Canal</span>
        <span>Release promovido</span>
        {onPromote ? <span className="scripts-matrix__header-action">Ação</span> : null}
      </div>

      {orderedChannels.map((channel) => {
        const selectedReleaseId = releases?.[0]?.id ?? '';
        const isOnLatestRelease = Boolean(
          channel.currentReleaseId
          && selectedReleaseId
          && channel.currentReleaseId === selectedReleaseId,
        );
        const showCurrentStatus = Boolean(onPromote && isOnLatestRelease);
        const showPromoteAction = Boolean(onPromote && !showCurrentStatus);

        return (
          <div
            key={channel.routeValue}
            className={`scripts-matrix__row ${ROUTE_CLASS[channel.routeValue] ?? 'scripts-matrix__row--custom'}`}
          >
            <ChannelIdentity channel={channel} compact={compact} />

            <div className="scripts-matrix__release">
              <CurrentRelease channel={channel} compact={compact} />
            </div>

            {showCurrentStatus ? (
              <div className="scripts-matrix__action">
                <span className="scripts-matrix__status">Atual</span>
              </div>
            ) : null}

            {showPromoteAction ? (
              <div className="scripts-matrix__action">
                <button
                  type="button"
                  className={`btn btn-scripts-outline btn-sm ${compact ? 'scripts-matrix__promote-btn' : ''}`}
                  disabled={!canPromote}
                  title={
                    !canPromote
                      ? 'Publique um release antes de promover'
                      : channel.version
                        ? `Trocar release em ${channel.displayName}`
                        : `Promover release em ${channel.displayName}`
                  }
                  onClick={() => onPromote!(channel)}
                >
                  {compact ? 'Promover' : 'Promover →'}
                </button>
              </div>
            ) : null}
          </div>
        );
      })}

      {onPromote && !canPromote ? (
        <p className="scripts-matrix__footnote muted small">
          Publique ao menos um release para habilitar promoções entre canais.
        </p>
      ) : null}
    </div>
  );
}
