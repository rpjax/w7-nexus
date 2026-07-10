import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { ScriptSummary } from '../../api/scripts/types';
import { formatRelativeTime } from '../../features/scripts/formatRelativeTime';
import { scriptStudioPath } from '../../features/scripts/scriptPaths';
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
    <article className="scripts-card card admin-surface">
      <div className="scripts-card__header">
        <div className="scripts-card__identity">
          <div className="scripts-card__title-row">
            <h3 className="scripts-card__name">
              <Link className="scripts-card__name-link" to={studioPath}>
                {script.name}
              </Link>
            </h3>
            <span className="scripts-priority" title="Menor prioridade injeta primeiro">P{script.priority}</span>
            {liveProd ? (
              <span className="scripts-card__prod-badge mono" title="Versão em produção">
                prod · {liveProd}
              </span>
            ) : null}
          </div>
          <ResolutionModeBadge hostPatterns={script.hostPatterns} />
        </div>
        <Link className="btn btn-scripts-outline btn-sm scripts-card__cta" to={studioPath}>
          Abrir studio
        </Link>
      </div>

      {!hasHosts ? (
        <p className="scripts-card__resolve muted small mono">
          GET /scripts?name={script.name}
        </p>
      ) : null}

      {script.description ? (
        <div className="scripts-card__desc-wrap">
          <p
            className={`scripts-card__desc muted ${descriptionExpanded ? 'is-expanded' : ''}`}
            title={!descriptionExpanded ? script.description : undefined}
          >
            {script.description}
          </p>
          {canExpandDescription ? (
            <button
              type="button"
              className="scripts-card__desc-toggle"
              onClick={() => setDescriptionExpanded((open) => !open)}
            >
              {descriptionExpanded ? 'Menos' : 'Ver descrição'}
            </button>
          ) : null}
        </div>
      ) : null}

      {hasHosts ? <HostPatternChips patterns={script.hostPatterns} /> : null}

      {script.channels.length > 0 ? (
        <div className="scripts-card__channels">
          <span className="scripts-card__channels-label muted small">Canais</span>
          <ChannelVersionPills channels={script.channels} compact />
        </div>
      ) : null}

      <footer className="scripts-card__footer muted small">
        Atualizado {formatRelativeTime(script.updatedAt)}
      </footer>
    </article>
  );
}
