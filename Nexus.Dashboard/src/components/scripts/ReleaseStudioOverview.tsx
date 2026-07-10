import type { ChannelSummary, ReleaseSummary } from '../../api/scripts/types';

type ReleaseStudioOverviewProps = {
  releases: ReleaseSummary[];
  channels: ChannelSummary[];
};

export function ReleaseStudioOverview({ releases, channels }: ReleaseStudioOverviewProps) {
  const latest = releases[0] ?? null;
  const deprecatedCount = releases.filter((release) => release.isDeprecated).length;
  const promotedCount = releases.filter((release) => release.promotedChannelRouteValues.length > 0).length;
  const prod = channels.find((channel) => channel.routeValue === 'prod');

  return (
    <div className="scripts-release-overview">
      <div className="scripts-release-overview__item">
        <span className="scripts-release-overview__label">Releases</span>
        <strong>{releases.length}</strong>
      </div>
      <div className="scripts-release-overview__item">
        <span className="scripts-release-overview__label">Mais recente</span>
        <strong className="mono">{latest?.version ?? '—'}</strong>
      </div>
      <div className="scripts-release-overview__item">
        <span className="scripts-release-overview__label">Prod</span>
        <strong className="mono">{prod?.version ?? 'sem release'}</strong>
      </div>
      <div className="scripts-release-overview__item">
        <span className="scripts-release-overview__label">Em canal</span>
        <strong>{promotedCount}</strong>
      </div>
      <div className="scripts-release-overview__item">
        <span className="scripts-release-overview__label">Deprecated</span>
        <strong>{deprecatedCount}</strong>
      </div>
    </div>
  );
}
