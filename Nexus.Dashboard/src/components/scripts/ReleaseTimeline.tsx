import type { ReleaseSummary } from '../../api/scripts/types';
import { formatRelativeTime } from '../../features/scripts/formatRelativeTime';
import { formatScriptFileSize } from '../../features/scripts/readScriptFile';

type ReleaseTimelineProps = {
  releases: ReleaseSummary[];
  selectedId: string | null;
  onSelect: (releaseId: string) => void;
};

const CHANNEL_ROUTE_CLASS: Record<string, string> = {
  prod: 'scripts-timeline__channel--prod',
  staging: 'scripts-timeline__channel--staging',
  development: 'scripts-timeline__channel--dev',
};

export function ReleaseTimeline({
  releases,
  selectedId,
  onSelect,
}: ReleaseTimelineProps) {
  if (releases.length === 0) {
    return <p className="muted scripts-timeline__empty">Nenhum release publicado ainda.</p>;
  }

  return (
    <div className="scripts-timeline" role="listbox" aria-label="Histórico de releases">
      {releases.map((release, index) => {
        const isSelected = release.id === selectedId;
        const isLast = index === releases.length - 1;

        return (
          <button
            key={release.id}
            type="button"
            role="option"
            aria-selected={isSelected}
            className={`scripts-timeline__item ${isSelected ? 'is-selected' : ''} ${isLast ? 'is-last' : ''}`}
            onClick={() => onSelect(release.id)}
          >
            <div className="scripts-timeline__rail" aria-hidden="true">
              <span className="scripts-timeline__dot" />
              {!isLast ? <span className="scripts-timeline__line" /> : null}
            </div>

            <div className="scripts-timeline__body">
              <div className="scripts-timeline__top">
                <span className="scripts-timeline__version mono">{release.version}</span>
                {release.promotedChannelRouteValues.map((route) => (
                  <span
                    key={route}
                    className={`scripts-timeline__channel mono ${CHANNEL_ROUTE_CLASS[route] ?? 'scripts-timeline__channel--custom'}`}
                  >
                    {route}
                  </span>
                ))}
                {release.isDeprecated ? (
                  <span className="scripts-badge scripts-badge--deprecated">Deprecated</span>
                ) : null}
              </div>

              <div className="scripts-timeline__meta">
                <span className="scripts-timeline__size">
                  {release.sourceCodeSizeBytes > 0
                    ? formatScriptFileSize(release.sourceCodeSizeBytes)
                    : '—'}
                </span>
                <span className="scripts-timeline__meta-sep" aria-hidden="true">·</span>
                <span className="scripts-timeline__time">{formatRelativeTime(release.createdAt)}</span>
              </div>
            </div>
          </button>
        );
      })}
    </div>
  );
}
