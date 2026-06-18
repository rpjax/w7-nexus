import { useEffect, useState } from 'react';
import type { ProfitShareCutInput } from '../../api/types';
import {
  clampCutToBudget,
  formatProfitShareInput,
  isProfitShareCutValid,
  isProfitShareTotalComplete,
  isProfitShareTotalValid,
  maxPercentageForCut,
  normalizeProfitShareCuts,
  parseProfitShareInput,
  PROFIT_SHARE_MAX_CUT,
  PROFIT_SHARE_MIN_CUT,
  PROFIT_SHARE_TOTAL_TOLERANCE,
  remainingPercentage,
  roundProfitSharePercentage,
  splitEvenlyPercentages,
  sumProfitSharePercentages,
} from '../../utils/profitShare';
import { Icon, IconButton } from '../IconButton';
import { ConfirmDialog } from '../ConfirmDialog';
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

function personInitial(label: string): string {
  const trimmed = label.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

type ProfitShareCutRowProps = {
  index: number;
  cut: ProfitShareCutDraft;
  cuts: ProfitShareCutDraft[];
  busy: boolean;
  canRemove: boolean;
  onPickAccount: () => void;
  onRemove: () => void;
  onChange: (value: number) => void;
};

function ProfitShareCutRow({
  index,
  cut,
  cuts,
  busy,
  canRemove,
  onPickAccount,
  onRemove,
  onChange,
}: ProfitShareCutRowProps) {
  const [text, setText] = useState(formatProfitShareInput(cut.percentage));
  const maxValue = maxPercentageForCut(cuts, index);

  useEffect(() => {
    setText(formatProfitShareInput(cut.percentage));
  }, [cut.percentage]);

  function commitText(raw: string) {
    const parsed = parseProfitShareInput(raw);
    if (parsed === null) {
      setText(formatProfitShareInput(cut.percentage));
      return;
    }
    const next = clampCutToBudget(cuts, index, parsed);
    onChange(next);
    setText(formatProfitShareInput(next));
  }

  const accountLabel = cut.label || (cut.accountId ? shortId(cut.accountId, 18) : 'Escolher conta');
  const hasAccount = Boolean(cut.accountId.trim());
  const sliderValue = Math.round(cut.percentage);
  const fillPct = Math.min(Math.max(sliderValue, 0), PROFIT_SHARE_MAX_CUT);

  function handleSliderChange(raw: number) {
    onChange(clampCutToBudget(cuts, index, Math.round(raw)));
  }

  return (
    <li className="ps-cut">
      {/* Row 1: account button | pct input | divider | trash */}
      <div className="ps-cut__top">
        {/* Left: clearly a button — avatar + name + chevron */}
        <button
          type="button"
          className={`ps-cut__account${hasAccount ? '' : ' is-empty'}`}
          onClick={onPickAccount}
          disabled={busy}
          title={hasAccount ? cut.accountId : 'Vincular conta'}
        >
          <span className="admin-op-person-avatar admin-op-person-avatar--sm" aria-hidden="true">
            {hasAccount ? personInitial(cut.label ?? cut.accountId) : <Icon name="link" />}
          </span>
          <span className="ps-cut__account-body">
            {hasAccount ? (
              <>
                <span className="ps-cut__account-name">{accountLabel}</span>
                <Icon name="chevron-right" className="ps-cut__account-chevron" />
              </>
            ) : (
              <>
                <span className="ps-cut__account-name">Vincular conta</span>
                <Icon name="chevron-right" className="ps-cut__account-chevron" />
              </>
            )}
          </span>
        </button>

        {/* Right: editable pct pill */}
        <label className="ps-cut__pct-pill" htmlFor={`ps-input-${index}`}>
          <input
            id={`ps-input-${index}`}
            className="ps-cut__pct-input"
            type="text"
            inputMode="decimal"
            enterKeyHint="done"
            autoComplete="off"
            aria-label={`Percentual de ${accountLabel}`}
            disabled={busy}
            value={text}
            onChange={(e) => setText(e.target.value)}
            onBlur={() => commitText(text)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') { e.preventDefault(); commitText(text); }
            }}
          />
          <span className="ps-cut__pct-sym" aria-hidden="true">%</span>
        </label>

        {/* Divider + trash — clearly separated */}
        <span className="ps-cut__sep" aria-hidden="true" />
        <IconButton
          icon="trash"
          label="Remover fatia"
          variant="danger"
          className="ps-cut__remove"
          disabled={!canRemove || busy}
          onClick={onRemove}
        />
      </div>

      {/* Row 2: full-width slider */}
      <div
        className="ps-cut__slider-row"
        style={{ ['--ps-fill' as string]: `${fillPct}%` }}
      >
        <div className="ps-cut__track" aria-hidden="true">
          <span className="ps-cut__fill" />
        </div>
        <input
          id={`ps-slider-${index}`}
          className="ps-cut__slider"
          type="range"
          min={0}
          max={PROFIT_SHARE_MAX_CUT}
          step={1}
          value={sliderValue}
          disabled={busy}
          aria-valuemin={0}
          aria-valuemax={maxValue}
          aria-valuenow={sliderValue}
          aria-label={`Ajustar percentual de ${accountLabel}`}
          onChange={(e) => handleSliderChange(Number(e.target.value))}
        />
      </div>
    </li>
  );
}

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
  const [pendingRemoveIndex, setPendingRemoveIndex] = useState<number | null>(null);

  useEffect(() => {
    if (!open) {
      setError('');
      setPendingRemoveIndex(null);
    }
  }, [open]);

  if (!open) return null;

  const total = sumProfitSharePercentages(cuts);
  const totalValid = isProfitShareTotalValid(total);
  const totalComplete = isProfitShareTotalComplete(total);
  const totalProgress = Math.min(Math.max(total, 0), PROFIT_SHARE_MAX_CUT);
  const remaining = remainingPercentage(cuts);

  function updatePercentage(index: number, value: number) {
    const nextValue = clampCutToBudget(cuts, index, value);
    onCutsChange(cuts.map((cut, i) => (
      i === index ? { ...cut, percentage: nextValue } : cut
    )));
  }

  function removeCut(index: number) {
    onCutsChange(cuts.filter((_, i) => i !== index));
    setPendingRemoveIndex(null);
  }

  function requestRemoveCut(index: number) {
    setPendingRemoveIndex(index);
  }

  const pendingCut = pendingRemoveIndex === null ? null : cuts[pendingRemoveIndex];
  const pendingCutLabel = pendingCut
    ? (pendingCut.label || (pendingCut.accountId ? shortId(pendingCut.accountId, 18) : 'esta fatia'))
    : '';

  function addCut() {
    if (cuts.length === 0) {
      onCutsChange([{ accountId: '', percentage: 100 }]);
      return;
    }

    if (remaining >= PROFIT_SHARE_MIN_CUT) {
      onCutsChange([...cuts, { accountId: '', percentage: remaining }]);
      return;
    }

    const shares = splitEvenlyPercentages(cuts.length + 1);
    onCutsChange([
      ...cuts.map((cut, index) => ({ ...cut, percentage: shares[index] ?? cut.percentage })),
      { accountId: '', percentage: shares[cuts.length] ?? 0 },
    ]);
  }

  function splitEvenly() {
    if (cuts.length === 0) return;
    const shares = splitEvenlyPercentages(cuts.length);
    onCutsChange(cuts.map((cut, index) => ({
      ...cut,
      percentage: shares[index] ?? cut.percentage,
    })));
  }

  function fillRemaining(index: number) {
    const room = remainingPercentage(cuts, index);
    if (room < PROFIT_SHARE_MIN_CUT) return;
    updatePercentage(index, roundProfitSharePercentage(cuts[index]!.percentage + room));
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
    if (!isProfitShareTotalValid(total)) {
      setError(`As fatias devem totalizar 100% (atual: ${total}%, tolerância ±${PROFIT_SHARE_TOTAL_TOLERANCE}%).`);
      return;
    }
    if (cuts.some((cut) => !isProfitShareCutValid(cut.percentage))) {
      setError(`Cada fatia deve ter entre ${PROFIT_SHARE_MIN_CUT}% e ${PROFIT_SHARE_MAX_CUT}%.`);
      return;
    }
    setError('');
    onSave(normalizeProfitShareCuts(cuts.map((cut) => ({
      accountId: cut.accountId.trim(),
      percentage: cut.percentage,
    }))));
  }

  return (
    <>
      <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
        <div
          className="dialog-card dialog-card--wide profit-share-modal"
          role="dialog"
          aria-modal="true"
          aria-labelledby="profit-share-modal-title"
          onClick={(e) => e.stopPropagation()}
        >
        <header className="account-picker-header profit-share-modal__head">
          <div className="account-picker-heading">
            <h3 id="profit-share-modal-title" className="account-picker-title">Regra de repasse</h3>
            <p className="account-picker-sub">Operador: {operatorName}</p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        <div className="profit-share-modal__body">
          <div className={`ps-total ps-total--${totalValid ? 'ok' : 'warn'}${totalComplete ? ' ps-total--complete' : ''}`}>
            <div className="ps-total__row">
              <span className="ps-total__label">Total</span>
              <strong className="ps-total__value">{formatProfitShareInput(total) || '0'}%</strong>
              {totalComplete ? (
                <span className="ps-total__badge">
                  <Icon name="check" />
                  100%
                </span>
              ) : remaining >= PROFIT_SHARE_MIN_CUT ? (
                <span className="ps-total__remaining muted small">Faltam {formatProfitShareInput(remaining)}%</span>
              ) : total > PROFIT_SHARE_MAX_CUT ? (
                <span className="ps-total__remaining ps-total__remaining--over">Excedeu {formatProfitShareInput(total - PROFIT_SHARE_MAX_CUT)}%</span>
              ) : null}
            </div>
            <div className="ps-total__bar" aria-hidden="true">
              <span className="ps-total__fill" style={{ width: `${totalProgress}%` }} />
              <span className="ps-total__target" />
            </div>
          </div>

          <div className="ps-toolbar">
            <button
              type="button"
              className="btn btn-ghost btn-small btn-with-icon"
              onClick={addCut}
              disabled={busy}
            >
              <Icon name="plus" />
              Fatia
            </button>
            {cuts.length >= 2 ? (
              <button
                type="button"
                className="btn btn-ghost btn-small"
                onClick={splitEvenly}
                disabled={busy}
              >
                Dividir igual
              </button>
            ) : null}
            {!totalValid && remaining >= PROFIT_SHARE_MIN_CUT && cuts.length > 0 ? (
              <button
                type="button"
                className="btn btn-ghost btn-small"
                onClick={() => fillRemaining(cuts.length - 1)}
                disabled={busy}
              >
                Completar 100%
              </button>
            ) : null}
          </div>

          {cuts.length === 0 ? (
            <p className="admin-op-empty muted small">Nenhuma fatia. Toque em &quot;Fatia&quot; para começar.</p>
          ) : (
            <ul className="ps-cut-list" aria-label="Fatias de repasse">
              {cuts.map((cut, index) => (
                <ProfitShareCutRow
                  key={`${cut.accountId}-${index}`}
                  index={index}
                  cut={cut}
                  cuts={cuts}
                  busy={busy}
                  canRemove={cuts.length > 1}
                  onPickAccount={() => onPickAccount(index)}
                  onRemove={() => requestRemoveCut(index)}
                  onChange={(value) => updatePercentage(index, value)}
                />
              ))}
            </ul>
          )}

          {error ? <p className="profit-share-modal__error" role="alert">{error}</p> : null}
        </div>

        <footer className="profit-share-modal__foot">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </button>
          <button type="button" className="btn btn-primary" onClick={handleSave} disabled={busy || !totalValid}>
            {busy ? 'Salvando…' : 'Salvar repasse'}
          </button>
        </footer>
        </div>
      </div>

      <ConfirmDialog
        open={pendingRemoveIndex !== null}
        title="Remover fatia"
        message={`Deseja remover a fatia de repasse${pendingCutLabel ? ` de ${pendingCutLabel}` : ''}?`}
        onCancel={() => setPendingRemoveIndex(null)}
        onConfirm={() => {
          if (pendingRemoveIndex !== null) removeCut(pendingRemoveIndex);
        }}
      />
    </>
  );
}
