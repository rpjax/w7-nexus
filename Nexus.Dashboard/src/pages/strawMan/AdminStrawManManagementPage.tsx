import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getAdministratorStrawManSettings, upsertStrawManSettings } from '../../api/strawMen/administrator';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import type { StrawManSettings } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { PixEntityField } from '../../components/finance/PixEntityField';
import { PageHeading } from '../../layouts/PageHeading';
import { paymentsPath } from '../../features/strawMen/strawManPaths';
import { formatDateTime, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

function formatFee(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
}

export function AdminStrawManManagementPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [strawManId, setStrawManId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [settings, setSettings] = useState<StrawManSettings | null>(null);
  const [feeInput, setFeeInput] = useState('0');
  const [pickerOpen, setPickerOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    if (!strawManId.trim()) {
      setSettings(null);
      setFeeInput('0');
      setDirty(false);
      return;
    }

    void (async () => {
      setLoading(true);
      const result = await getAdministratorStrawManSettings(strawManId.trim());
      if (!result.ok) {
        notifyError(result.error);
        setSettings(null);
      } else {
        setSettings(result.data ?? null);
        setFeeInput(String(result.data?.movementFeePercentage ?? 0));
        setDirty(false);
      }
      setLoading(false);
    })();
  }, [strawManId, notifyError]);

  async function handleSave() {
    if (!strawManId.trim()) {
      notifyError('Selecione o laranja.');
      return;
    }
    const movementFeePercentage = Number(feeInput.replace(',', '.'));
    if (!Number.isFinite(movementFeePercentage) || movementFeePercentage < 0) {
      notifyError('Informe uma taxa de movimentação válida.');
      return;
    }

    setBusy(true);
    try {
      const result = await upsertStrawManSettings(strawManId.trim(), movementFeePercentage);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      setSettings(result.data ?? null);
      setFeeInput(String(result.data?.movementFeePercentage ?? movementFeePercentage));
      setDirty(false);
      notifySuccess('Configurações do laranja atualizadas.');
    } finally {
      setBusy(false);
    }
  }

  function clearSelection() {
    setStrawManId('');
    setStrawLabel(null);
    setSettings(null);
    setFeeInput('0');
    setDirty(false);
  }

  return (
    <div className="ops-page straw-man-page">
      <PageHeading
        kicker="Laranjas"
        kickerVariant="admin"
        title="Gestão de laranjas"
        subtitle="Configure taxas e parâmetros de qualquer titular laranja do sistema."
        backLink={{ to: '/dashboard', label: 'Visão geral' }}
      />

      <section className="pix-workspace straw-man-workspace straw-man-workspace--admin" aria-labelledby="straw-man-admin-title">
        <header className="pix-workspace__hero straw-man-workspace__hero">
          <div className="pix-workspace__hero-main">
            <span className="straw-man-workspace__badge straw-man-workspace__badge--admin">Administração</span>
            <p className="straw-man-workspace__count" id="straw-man-admin-title" aria-live="polite">
              {strawLabel ? `@${strawLabel.split(' ')[0]?.replace('@', '')}` : 'Selecione um laranja'}
            </p>
            <p className="pix-workspace__hero-hint muted small">
              Escolha o titular e ajuste a taxa de movimentação aplicada nas transferências.
            </p>
          </div>
          <div className="pix-workspace__hero-mark straw-man-workspace__mark" aria-hidden="true">
            <span className="straw-man-workspace__mark-icon">ADM</span>
          </div>
        </header>

        <div className="pix-workspace__divider" aria-hidden="true" />

        <div className="pix-workspace__body">
          <div className="straw-man-admin-layout">
            <aside className="straw-man-admin-layout__picker card ops-card">
              <h2 className="straw-man-admin-layout__picker-title">Titular</h2>
              <p className="muted small">Busque e selecione a conta laranja.</p>
              <PixEntityField
                label="Laranja"
                emptyLabel="Selecionar laranja"
                name={strawLabel}
                id={strawManId || null}
                accent="warm"
                onPick={() => setPickerOpen(true)}
                onClear={clearSelection}
              />
              {strawManId ? (
                <p className="mono muted small straw-man-admin-layout__id" title={strawManId}>
                  {shortId(strawManId, 18)}
                </p>
              ) : null}
            </aside>

            <section className="straw-man-admin-layout__form card ops-card">
              {!strawManId ? (
                <div className="straw-man-admin-layout__empty">
                  <p className="muted">Selecione um laranja para visualizar e editar as configurações.</p>
                </div>
              ) : loading ? (
                <p className="muted">Carregando configurações…</p>
              ) : (
                <>
                  <header className="straw-man-settings-panel__head">
                    <div>
                      <h2 className="straw-man-settings-panel__title">Taxa de movimentação</h2>
                      <p className="muted small">
                        Valor atual: <strong>{formatFee(settings?.movementFeePercentage ?? 0)}%</strong>
                      </p>
                    </div>
                  </header>

                  <div className="straw-man-admin-fee-row">
                    <div className="field straw-man-admin-fee-row__input">
                      <label htmlFor="adminMovementFee">Nova taxa (%)</label>
                      <input
                        id="adminMovementFee"
                        type="number"
                        min={0}
                        step="0.01"
                        className="nexus-input"
                        value={feeInput}
                        onChange={(e) => {
                          setFeeInput(e.target.value);
                          setDirty(true);
                        }}
                      />
                    </div>
                    <div className="straw-man-settings-panel__metric straw-man-settings-panel__metric--inline">
                      <span className="straw-man-settings-panel__metric-label">Prévia</span>
                      <strong className="straw-man-settings-panel__metric-value straw-man-settings-panel__metric-value--sm">
                        {formatFee(Number(feeInput.replace(',', '.')) || 0)}%
                      </strong>
                    </div>
                  </div>

                  <dl className="straw-man-settings-panel__meta">
                    <div>
                      <dt>Última atualização</dt>
                      <dd>{settings?.updatedAt ? formatDateTime(settings.updatedAt) : 'Nunca configurado'}</dd>
                    </div>
                    {settings?.updatedByAdminId ? (
                      <div>
                        <dt>Atualizado por</dt>
                        <dd className="mono">{shortId(settings.updatedByAdminId, 12)}</dd>
                      </div>
                    ) : null}
                  </dl>

                  <footer className="straw-man-admin-layout__footer">
                    <Link className="btn btn-ghost btn-sm" to={paymentsPath('global-admin')}>
                      Ver pagamentos (admin)
                    </Link>
                    <button
                      type="button"
                      className="btn btn-primary"
                      disabled={busy || !dirty}
                      onClick={() => void handleSave()}
                    >
                      {busy ? 'Salvando…' : 'Salvar configurações'}
                    </button>
                  </footer>
                </>
              )}
            </section>
          </div>
        </div>
      </section>

      <AccountPickerModal
        open={pickerOpen}
        onClose={() => setPickerOpen(false)}
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        subtitle="Titular cujas configurações serão gerenciadas."
        onSelected={(row) => {
          setStrawManId(row.id);
          setStrawLabel(`${row.username} · ${shortId(row.id, 8)}`);
        }}
      />
    </div>
  );
}
