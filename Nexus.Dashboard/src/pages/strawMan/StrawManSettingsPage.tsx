import { useEffect, useState } from 'react';
import { upsertStrawManSettings } from '../../api/administrator/strawMen';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { getStrawManSettings } from '../../api/strawMan/settings';
import type { StrawManSettings } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { PageHeading } from '../../layouts/PageHeading';
import { shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

export function StrawManSettingsPage() {
  const { notifyError } = useNotifications();
  const [settings, setSettings] = useState<StrawManSettings | null>(null);
  const [feeInput, setFeeInput] = useState('');
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
        setFeeInput(String(result.data?.movementFeePercentage ?? 0));
      }
      setLoading(false);
    })();
  }, [notifyError]);

  if (loading) return <p className="muted">Carregando configurações…</p>;

  return (
    <>
      <PageHeading
        kicker="Laranja"
        title="Configurações"
        subtitle="Taxa aplicada em movimentações entre contas."
      />

      <section className="card ops-card">
        <div className="form-grid form-grid-wide">
          <div className="field">
            <label>Conta laranja</label>
            <input className="nexus-input mono" readOnly value={settings?.strawManId ? shortId(settings.strawManId) : '—'} />
          </div>
          <div className="field">
            <label>Taxa de movimentação (%)</label>
            <input className="nexus-input" readOnly value={feeInput} />
          </div>
        </div>
        {settings?.updatedAt ? (
          <p className="muted small">Última atualização: {new Date(settings.updatedAt).toLocaleString('pt-BR')}</p>
        ) : null}
      </section>
    </>
  );
}

export function AdminStrawManSettingsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [strawManId, setStrawManId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [feeInput, setFeeInput] = useState('0');
  const [pickerOpen, setPickerOpen] = useState(false);
  const [busy, setBusy] = useState(false);

  async function handleSave() {
    if (!strawManId.trim()) {
      notifyError('Selecione o laranja.');
      return;
    }
    const movementFeePercentage = Number(feeInput);
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
      notifySuccess('Configurações do laranja atualizadas.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeading
        kicker="Administração"
        title="Configurações do laranja"
        subtitle="Defina a taxa de movimentação para um titular laranja."
      />

      <section className="card ops-card">
        <div className="form-grid form-grid-wide">
          <div className="field span-2">
            <label>Laranja</label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setPickerOpen(true)}>
                {strawLabel ?? 'Selecionar laranja'}
              </button>
            </div>
          </div>
          <div className="field">
            <label htmlFor="adminMovementFee">Taxa de movimentação (%)</label>
            <input
              id="adminMovementFee"
              type="number"
              min={0}
              step="0.01"
              className="nexus-input"
              value={feeInput}
              onChange={(e) => setFeeInput(e.target.value)}
            />
          </div>
        </div>
        <div className="card-actions">
          <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void handleSave()}>
            {busy ? 'Salvando…' : 'Salvar configurações'}
          </button>
        </div>
      </section>

      <AccountPickerModal
        open={pickerOpen}
        onClose={() => setPickerOpen(false)}
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        onSelected={(row) => {
          setStrawManId(row.id);
          setStrawLabel(`${row.username} (${shortId(row.id)})`);
        }}
      />
    </>
  );
}
