import type { ChannelFilter, ResolutionModeFilter } from '../../api/scripts/types';

type ScriptFilterBarProps = {
  mode: ResolutionModeFilter;
  channel: ChannelFilter;
  visibleCount: number;
  totalCount: number;
  onModeChange: (mode: ResolutionModeFilter) => void;
  onChannelChange: (channel: ChannelFilter) => void;
};

export function ScriptFilterBar({
  mode,
  channel,
  visibleCount,
  totalCount,
  onModeChange,
  onChannelChange,
}: ScriptFilterBarProps) {
  const countLabel = visibleCount === totalCount
    ? `${totalCount} script${totalCount === 1 ? '' : 's'}`
    : `${visibleCount} de ${totalCount} scripts`;

  return (
    <div className="scripts-filter-panel">
      <div className="scripts-filter-panel__controls">
        <div className="scripts-filter-bar__group">
          <label className="scripts-filter-bar__label" htmlFor="scripts-mode-filter">Modo</label>
          <select
            id="scripts-mode-filter"
            className="nexus-input scripts-filter-bar__select"
            value={mode}
            onChange={(e) => onModeChange(e.target.value as ResolutionModeFilter)}
          >
            <option value="all">Todos</option>
            <option value="host">Por host</option>
            <option value="name-only">Somente por nome</option>
          </select>
        </div>

        <div className="scripts-filter-bar__group">
          <label className="scripts-filter-bar__label" htmlFor="scripts-channel-filter">Canal</label>
          <select
            id="scripts-channel-filter"
            className="nexus-input scripts-filter-bar__select"
            value={channel}
            onChange={(e) => onChannelChange(e.target.value as ChannelFilter)}
          >
            <option value="all">Todos</option>
            <option value="prod">Com release em prod</option>
            <option value="staging">Com release em staging</option>
            <option value="development">Com release em dev</option>
            <option value="missing-prod">Sem release em prod</option>
          </select>
        </div>

        <div className="scripts-filter-bar__group scripts-filter-bar__group--count">
          <span className="scripts-filter-bar__label">Resultado</span>
          <span className="scripts-filter-panel__count" aria-live="polite">{countLabel}</span>
        </div>
      </div>
    </div>
  );
}
