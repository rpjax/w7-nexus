import type { OlxAdminAdPatchRow, OlxOperatorAdPatchRow } from '../../api/olx/types';
import { StatusPill } from '../../components/finance/StatusPill';
import { formatDateTime } from '../../utils/format';
import {
  formatAdPatchTitle,
  formatOptionalPrice,
  patchStatusLabel,
  patchStatusTone,
} from './adPatchDisplay';
import { resolveOlxOperationLabel } from './useOlxOperationLabels';

type AdPatchListItemProps = {
  row: OlxOperatorAdPatchRow | OlxAdminAdPatchRow;
  scope: 'operator' | 'admin';
  operationLabels?: Record<string, string>;
  operatorLabel?: string | null;
  currentAccountId?: string | null;
  busy?: boolean;
  onImpersonate?: () => void;
  onUnimpersonate?: () => void;
  onEditPrices?: () => void;
  onForceUnimpersonate?: () => void;
};

export function AdPatchListItem({
  row,
  scope,
  operationLabels = {},
  operatorLabel,
  currentAccountId,
  busy = false,
  onImpersonate,
  onUnimpersonate,
  onEditPrices,
  onForceUnimpersonate,
}: AdPatchListItemProps) {
  const operatorId = 'operatorId' in row ? row.operatorId : null;
  const isOwn = scope === 'operator' || (operatorId && currentAccountId && operatorId === currentAccountId);
  const canEditPrices = scope === 'operator' && row.isImpersonating && isOwn;
  const canUnimpersonate = scope === 'operator' && row.isImpersonating && isOwn;
  const canImpersonate = scope === 'operator' && !row.isImpersonating;
  const canForceUnimpersonate = scope === 'admin' && row.isImpersonating && Boolean(operatorId);
  const hasPatchedPrices = row.originalPrice != null || row.promotionalPrice != null;
  const operationTitle = resolveOlxOperationLabel(row.operationId, operationLabels);
  const hasActions = canImpersonate || canEditPrices || canUnimpersonate || canForceUnimpersonate;

  return (
    <article className={`olx-ad-card${row.isImpersonating ? ' olx-ad-card--active' : ''}`}>
      <header className="olx-ad-card__head">
        <div className="olx-ad-card__lead">
          <span className="olx-ad-card__kicker">OLX · Patch</span>
          <h3 className="olx-ad-card__title">{formatAdPatchTitle(row.adId)}</h3>
          <div className="olx-ad-card__pills">
            <StatusPill label={patchStatusLabel(row.isImpersonating)} tone={patchStatusTone(row.isImpersonating)} />
            <StatusPill
              label={hasPatchedPrices ? 'Preços definidos' : 'Sem preços patch'}
              tone={hasPatchedPrices ? 'warn' : 'info'}
            />
          </div>
        </div>
        <time className="olx-ad-card__time muted small" dateTime={row.updatedAt}>
          {formatDateTime(row.updatedAt)}
        </time>
      </header>

      <div className="olx-ad-card__prices">
        <div className={`olx-ad-card__price${row.promotionalPrice != null ? ' olx-ad-card__price--highlight' : ''}`}>
          <span className="olx-ad-card__price-label">Promocional</span>
          <strong className="olx-ad-card__price-value">{formatOptionalPrice(row.promotionalPrice)}</strong>
        </div>
        <div className="olx-ad-card__price">
          <span className="olx-ad-card__price-label">Original</span>
          <strong className="olx-ad-card__price-value">{formatOptionalPrice(row.originalPrice)}</strong>
        </div>
      </div>

      <div className="olx-ad-card__context">
        <span className="olx-ad-card__context-item olx-ad-card__context-item--full">
          <span className="olx-ad-card__context-label">URL</span>
          {row.adUrl ? (
            <a
              className="olx-ad-card__context-link"
              href={row.adUrl}
              target="_blank"
              rel="noreferrer noopener"
              title={row.adUrl}
            >
              Abrir anúncio
            </a>
          ) : (
            <span className="olx-ad-card__context-value">—</span>
          )}
        </span>
        <span className="olx-ad-card__context-item">
          <span className="olx-ad-card__context-label">Operação</span>
          <span className="olx-ad-card__context-value" title={row.operationId}>{operationTitle}</span>
        </span>
        {scope === 'admin' ? (
          <span className="olx-ad-card__context-item">
            <span className="olx-ad-card__context-label">Operador OLX</span>
            <span className="olx-ad-card__context-value" title={operatorId ?? undefined}>
              {operatorLabel ? `@${operatorLabel}` : operatorId ? 'Conta vinculada' : '—'}
            </span>
          </span>
        ) : null}
      </div>

      {hasActions ? (
        <footer className="olx-ad-card__actions">
          {canImpersonate ? (
            <button type="button" className="btn btn-primary btn-small" disabled={busy} onClick={onImpersonate}>
              Assumir
            </button>
          ) : null}
          {canEditPrices ? (
            <button type="button" className="btn btn-small olx-ad-card__btn olx-ad-card__btn--secondary" disabled={busy} onClick={onEditPrices}>
              Editar preços
            </button>
          ) : null}
          {canUnimpersonate ? (
            <button type="button" className="btn btn-ghost btn-small" disabled={busy} onClick={onUnimpersonate}>
              Liberar
            </button>
          ) : null}
          {canForceUnimpersonate ? (
            <button type="button" className="btn btn-danger btn-small" disabled={busy} onClick={onForceUnimpersonate}>
              Forçar liberação
            </button>
          ) : null}
        </footer>
      ) : null}
    </article>
  );
}
