import type { ChannelSummary, ReleaseSummary } from '../../api/scripts/types';

type PromoteChannelDrawerProps = {
  open: boolean;
  busy: boolean;
  scriptName: string;
  hostPatterns: string[];
  channel: ChannelSummary | null;
  releases: ReleaseSummary[];
  selectedReleaseId: string;
  onSelectRelease: (releaseId: string) => void;
  onClose: () => void;
  onConfirm: () => void;
};

export function PromoteChannelDrawer({
  open,
  busy,
  scriptName,
  hostPatterns,
  channel,
  releases,
  selectedReleaseId,
  onSelectRelease,
  onClose,
  onConfirm,
}: PromoteChannelDrawerProps) {
  if (!open || !channel) return null;

  const selected = releases.find((r) => r.id === selectedReleaseId);
  const beforeVersion = channel.version ?? '—';
  const afterVersion = selected?.version ?? '—';

  return (
    <div className="scripts-drawer-backdrop" onClick={onClose}>
      <aside className="scripts-drawer scripts-drawer--promote" onClick={(e) => e.stopPropagation()}>
        <header className="scripts-drawer__header">
          <div className="scripts-drawer__header-main">
            <p className="scripts-drawer__kicker">Promover · {scriptName}</p>
            <h3>{channel.displayName}</h3>
          </div>
          <button type="button" className="account-picker-close scripts-drawer__close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </header>

        <div className="scripts-drawer__body">
          <div className="scripts-promote-compare">
            <div className="scripts-promote-card">
              <span className="muted small">Atual</span>
              <strong className="mono">{beforeVersion}</strong>
            </div>
            <span className="scripts-promote-arrow" aria-hidden="true">→</span>
            <div className="scripts-promote-card scripts-promote-card--next">
              <span className="muted small">Novo</span>
              <strong className="mono">{afterVersion}</strong>
            </div>
          </div>

          <div className="field">
            <label htmlFor="promoteReleaseSelect">Release</label>
            <select
              id="promoteReleaseSelect"
              className="nexus-input"
              value={selectedReleaseId}
              onChange={(e) => onSelectRelease(e.target.value)}
            >
              {releases.map((release) => (
                <option key={release.id} value={release.id}>
                  {release.version}{release.isDeprecated ? ' (deprecated)' : ''}
                </option>
              ))}
            </select>
          </div>

          <div className="scripts-impact-panel">
            <h4>Impacto</h4>
            {hostPatterns.length > 0 ? (
              <ul className="scripts-impact-list">
                {hostPatterns.map((host) => (
                  <li key={host}><code>{host}</code></li>
                ))}
              </ul>
            ) : (
              <p className="muted small">Este script só é resolvido por nome — promoção afeta <code>?name={scriptName}</code>.</p>
            )}
          </div>

          <div className="scripts-cache-warning">
            <strong>Cache invalidado</strong>
            <p className="muted small">
              O ScriptCache (L1, TTL ~60s) será limpo. Clientes podem ver a versão anterior por até 1 minuto.
            </p>
          </div>
        </div>

        <footer className="scripts-drawer__footer">
          <button type="button" className="btn btn-scripts-outline" onClick={onClose} disabled={busy}>Cancelar</button>
          <button type="button" className="btn btn-scripts-accent" onClick={onConfirm} disabled={busy || !selectedReleaseId}>
            {busy ? 'Promovendo…' : 'Confirmar promoção'}
          </button>
        </footer>
      </aside>
    </div>
  );
}
