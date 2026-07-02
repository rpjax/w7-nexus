import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getStrawManSettings } from '../../api/strawMen/strawMan';
import type { StrawManSettings } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { PageHeading } from '../../layouts/PageHeading';
import { paymentsPath } from '../../features/strawMen/strawManPaths';
import { formatDateTime, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

function formatFee(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
}

export function StrawManSelfSettingsPage() {
  const { user } = useAuth();
  const { notifyError } = useNotifications();
  const [settings, setSettings] = useState<StrawManSettings | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void (async () => {
      setLoading(true);
      const result = await getStrawManSettings();
      if (!result.ok) {
        notifyError(result.error);
        setSettings(null);
      } else {
        setSettings(result.data ?? null);
      }
      setLoading(false);
    })();
  }, [notifyError]);

  const fee = settings?.movementFeePercentage ?? 0;

  return (
    <div className="ops-page straw-man-page">
      <PageHeading
        kicker="Laranjas"
        title="Minhas configurações"
        subtitle="Parâmetros da sua conta laranja definidos pela administração."
      />

      <section className="pix-workspace straw-man-workspace" aria-labelledby="straw-man-self-settings">
        <header className="pix-workspace__hero straw-man-workspace__hero">
          <div className="pix-workspace__hero-main">
            <span className="straw-man-workspace__badge">Conta laranja</span>
            <p className="straw-man-workspace__identity" aria-live="polite">
              @{user?.username ?? '—'}
            </p>
            <p className="pix-workspace__hero-hint muted small">
              Taxa aplicada quando saldos são movimentados entre contas.
            </p>
          </div>
          <div className="pix-workspace__hero-mark straw-man-workspace__mark" aria-hidden="true">
            <span className="straw-man-workspace__mark-icon">SM</span>
          </div>
        </header>

        <div className="pix-workspace__divider" aria-hidden="true" />

        <div className="pix-workspace__body">
          {loading ? (
            <p className="muted">Carregando configurações…</p>
          ) : (
            <section className="straw-man-settings-panel" id="straw-man-self-settings">
              <div className="straw-man-settings-panel__metric">
                <span className="straw-man-settings-panel__metric-label">Taxa de movimentação</span>
                <strong className="straw-man-settings-panel__metric-value">{formatFee(fee)}%</strong>
                <p className="muted small straw-man-settings-panel__metric-hint">
                  Percentual retido em movimentações entre contas do mesmo titular ou de terceiros.
                </p>
              </div>

              <dl className="straw-man-settings-panel__meta">
                <div>
                  <dt>Identificador</dt>
                  <dd className="mono">{settings?.strawManId ? shortId(settings.strawManId, 16) : '—'}</dd>
                </div>
                <div>
                  <dt>Última atualização</dt>
                  <dd>{settings?.updatedAt ? formatDateTime(settings.updatedAt) : 'Padrão do sistema (0%)'}</dd>
                </div>
              </dl>

              <div className="straw-man-settings-panel__actions">
                <Link className="btn btn-primary btn-sm" to={paymentsPath('self')}>
                  Ver meus pagamentos
                </Link>
              </div>
            </section>
          )}
        </div>
      </section>
    </div>
  );
}
