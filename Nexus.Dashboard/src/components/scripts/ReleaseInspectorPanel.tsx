import type { ReleaseSummary, ScriptDetail } from '../../api/scripts/types';
import { formatRelativeTime } from '../../features/scripts/formatRelativeTime';
import { formatScriptFileSize } from '../../features/scripts/readScriptFile';
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

const CHANNEL_ROUTE_CLASS: Record<string, string> = {
  prod: 'scripts-timeline__channel--prod',
  staging: 'scripts-timeline__channel--staging',
  development: 'scripts-timeline__channel--dev',
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
    <div className="scripts-release-inspector">
      <header className="scripts-release-inspector__head">
        <div className="scripts-release-inspector__identity">
          <div className="scripts-release-inspector__title-row">
            <h2 className="mono">{release.version}</h2>
            {release.isDeprecated ? (
              <span className="scripts-badge scripts-badge--deprecated">Deprecated</span>
            ) : isLiveInProd ? (
              <span className="scripts-badge scripts-badge--live-prod">Live em prod</span>
            ) : promoted.length > 0 ? (
              <span className="scripts-release-inspector__promoted">
                {promoted.map((route) => (
                  <span
                    key={route}
                    className={`scripts-timeline__channel mono ${CHANNEL_ROUTE_CLASS[route] ?? 'scripts-timeline__channel--custom'}`}
                  >
                    {route}
                  </span>
                ))}
              </span>
            ) : (
              <span className="scripts-release-inspector__unpromoted muted small">Sem promoção</span>
            )}
          </div>
          <p className="scripts-release-inspector__published muted small">
            Publicado {formatRelativeTime(release.createdAt)}
          </p>
        </div>

        <div className="scripts-release-inspector__actions">
          <button type="button" className="btn btn-ghost btn-sm" onClick={onDeprecate}>
            {release.isDeprecated ? 'Restaurar' : 'Deprecar'}
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-sm scripts-release-inspector__delete"
            onClick={onDelete}
          >
            Excluir
          </button>
        </div>
      </header>

      <div className="scripts-panel__body scripts-release-inspector__body">
        <dl className="scripts-release-inspector__meta">
          <div className="scripts-release-inspector__meta-item scripts-release-inspector__meta-item--hash">
            <dt>SHA-256</dt>
            <dd>
              <code className="scripts-release-inspector__hash mono">{release.hash}</code>
              <button
                type="button"
                className="btn btn-ghost btn-sm scripts-release-inspector__copy"
                onClick={onCopyHash}
              >
                Copiar
              </button>
            </dd>
          </div>

          <div className="scripts-release-inspector__meta-item">
            <dt>Tamanho</dt>
            <dd title={`${release.sourceCodeSizeBytes.toLocaleString('pt-BR')} bytes`}>
              {formatScriptFileSize(release.sourceCodeSizeBytes)}
            </dd>
          </div>

          <div className="scripts-release-inspector__meta-item">
            <dt>ID</dt>
            <dd className="mono scripts-release-inspector__id">{release.id}</dd>
          </div>

          <div className="scripts-release-inspector__meta-item">
            <dt>Resolve</dt>
            <dd>
              {release.isDeprecated ? (
                <span className="scripts-release-inspector__resolve scripts-release-inspector__resolve--off">
                  Oculto
                </span>
              ) : (
                <span className="scripts-release-inspector__resolve scripts-release-inspector__resolve--on">
                  Público
                </span>
              )}
            </dd>
          </div>
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
      </div>
    </div>
  );
}
