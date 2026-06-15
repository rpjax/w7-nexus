import { useEffect, useState } from 'react';
import type { ProfitShareCutInput } from '../../api/types';
import { shortId } from '../../utils/format';

export type ProfitShareCutDraft = ProfitShareCutInput & {
  label?: string;
};

type ProfitShareRuleModalProps = {
  open: boolean;
  operatorName: string;
  busy: boolean;
  onClose: () => void;
  onPickAccount: (cutIndex: number) => void;
  onSave: (cuts: ProfitShareCutInput[]) => void;
  cuts: ProfitShareCutDraft[];
  onCutsChange: (cuts: ProfitShareCutDraft[]) => void;
};

export function ProfitShareRuleModal({
  open,
  operatorName,
  busy,
  onClose,
  onPickAccount,
  onSave,
  cuts,
  onCutsChange,
}: ProfitShareRuleModalProps) {
  const [error, setError] = useState('');

  useEffect(() => {
    if (!open) setError('');
  }, [open]);

  if (!open) return null;

  const total = cuts.reduce((sum, cut) => sum + (Number(cut.percentage) || 0), 0);
  const totalRounded = Math.round(total * 100) / 100;

  function updatePercentage(index: number, value: string) {
    const next = cuts.map((cut, i) => (
      i === index ? { ...cut, percentage: value === '' ? 0 : Number(value) } : cut
    ));
    onCutsChange(next);
  }

  function removeCut(index: number) {
    onCutsChange(cuts.filter((_, i) => i !== index));
  }

  function addCut() {
    onCutsChange([...cuts, { accountId: '', percentage: 0 }]);
  }

  function handleSave() {
    if (cuts.length === 0) {
      setError('Adicione pelo menos uma fatia de repasse.');
      return;
    }
    if (cuts.some((cut) => !cut.accountId.trim())) {
      setError('Todas as fatias precisam de uma conta vinculada.');
      return;
    }
    if (totalRounded !== 100) {
      setError(`As fatias devem totalizar 100% (atual: ${totalRounded}%).`);
      return;
    }
    if (cuts.some((cut) => cut.percentage <= 0 || cut.percentage > 100)) {
      setError('Cada fatia deve ter entre 0,01% e 100%.');
      return;
    }
    setError('');
    onSave(cuts.map((cut) => ({
      accountId: cut.accountId.trim(),
      percentage: cut.percentage,
    })));
  }

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div className="dialog-card dialog-card--wide profit-share-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-stack-header">
          <div>
            <h3>Regra de repasse</h3>
            <p className="muted small">Operador: {operatorName}</p>
          </div>
          <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </div>

        <div className="modal-stack">
          <p className="muted small profit-share-lead">
            Defina como o faturamento deste operador é dividido entre contas. A soma deve ser exatamente 100%.
          </p>

          <ul className="profit-share-editor-list">
            {cuts.map((cut, index) => (
              <li key={`${cut.accountId}-${index}`} className="profit-share-editor-row">
                <div className="profit-share-editor-account">
                  <span className="profit-share-editor-label">Conta</span>
                  <button
                    type="button"
                    className="btn btn-ghost profit-share-pick-btn"
                    onClick={() => onPickAccount(index)}
                  >
                    {cut.label || (cut.accountId ? shortId(cut.accountId, 18) : 'Selecionar conta…')}
                  </button>
                </div>
                <div className="profit-share-editor-pct">
                  <label className="profit-share-editor-label" htmlFor={`profitPct-${index}`}>%</label>
                  <input
                    id={`profitPct-${index}`}
                    className="nexus-input"
                    type="number"
                    min={0.01}
                    max={100}
                    step={0.01}
                    value={cut.percentage || ''}
                    onChange={(e) => updatePercentage(index, e.target.value)}
                  />
                </div>
                <button
                  type="button"
                  className="btn btn-ghost btn-small"
                  onClick={() => removeCut(index)}
                  disabled={cuts.length <= 1}
                >
                  Remover
                </button>
              </li>
            ))}
          </ul>

          <div className="profit-share-editor-footer">
            <button type="button" className="btn btn-ghost btn-small" onClick={addCut}>Adicionar fatia</button>
            <span className={`profit-share-total ${totalRounded === 100 ? 'is-valid' : 'is-invalid'}`}>
              Total: {totalRounded}%
            </span>
          </div>

          {error ? <p className="profit-share-error">{error}</p> : null}

          <div className="dialog-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose}>Cancelar</button>
            <button type="button" className="btn btn-primary" onClick={handleSave} disabled={busy}>
              {busy ? 'Salvando…' : 'Salvar repasse'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
