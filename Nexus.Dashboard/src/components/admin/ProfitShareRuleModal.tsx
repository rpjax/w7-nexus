import { useEffect, useState } from 'react';
import { Check, ChevronRight, Link2, Plus, Trash2, X } from 'lucide-react';
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { shortId } from '../../utils/format';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Progress } from '@/components/ui/progress';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

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

  function handleSliderChange(raw: number) {
    onChange(clampCutToBudget(cuts, index, Math.round(raw)));
  }

  return (
    <li className="space-y-2 rounded-lg border border-border/60 bg-background/40 p-3">
      <div className="flex items-center gap-2">
        <Button
          type="button"
          variant="outline"
          className={cn(
            'h-auto min-w-0 flex-1 justify-start gap-2 px-2 py-1.5 font-normal',
            !hasAccount && 'text-muted-foreground',
          )}
          onClick={onPickAccount}
          disabled={busy}
          title={hasAccount ? cut.accountId : 'Vincular conta'}
        >
          <Avatar size="sm">
            <AvatarFallback>
              {hasAccount ? personInitial(cut.label ?? cut.accountId) : <Link2 className="size-3" />}
            </AvatarFallback>
          </Avatar>
          <span className="min-w-0 flex-1 truncate text-left">
            {hasAccount ? accountLabel : 'Vincular conta'}
          </span>
          <ChevronRight className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
        </Button>

        <label className="flex items-center gap-0.5 rounded-md border border-border/60 bg-card px-2 py-1" htmlFor={`ps-input-${index}`}>
          <Input
            id={`ps-input-${index}`}
            className="h-7 w-12 border-0 bg-transparent p-0 text-center shadow-none focus-visible:ring-0"
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
          <span className="text-xs text-muted-foreground" aria-hidden="true">%</span>
        </label>

        <Separator orientation="vertical" className="h-6" />
        <Button
          type="button"
          variant="destructive"
          size="icon-sm"
          aria-label="Remover fatia"
          disabled={!canRemove || busy}
          onClick={onRemove}
        >
          <Trash2 className="size-4" />
        </Button>
      </div>

      <div className="space-y-1">
        <Progress value={Math.min(Math.max(sliderValue, 0), PROFIT_SHARE_MAX_CUT)} className="h-1.5" />
        <input
          id={`ps-slider-${index}`}
          className="w-full accent-primary"
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
      <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl" showCloseButton={false}>
          <DialogHeader>
            <div className="flex items-start justify-between gap-3">
              <div className="space-y-1">
                <DialogTitle>Regra de repasse</DialogTitle>
                <DialogDescription>Operador: {operatorName}</DialogDescription>
              </div>
              <Button type="button" variant="ghost" size="icon-sm" aria-label="Fechar" onClick={onClose}>
                <X className="size-4" />
              </Button>
            </div>
          </DialogHeader>

          <div className="space-y-4">
            <div
              className={cn(
                'space-y-2 rounded-lg border px-3 py-2',
                totalValid ? 'border-success/40 bg-success/5' : 'border-warning/40 bg-warning/5',
              )}
            >
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-sm text-muted-foreground">Total</span>
                <strong className="text-base text-foreground">{formatProfitShareInput(total) || '0'}%</strong>
                {totalComplete ? (
                  <Badge variant="success" className="gap-1">
                    <Check className="size-3" />
                    100%
                  </Badge>
                ) : remaining >= PROFIT_SHARE_MIN_CUT ? (
                  <span className="text-sm text-muted-foreground">Faltam {formatProfitShareInput(remaining)}%</span>
                ) : total > PROFIT_SHARE_MAX_CUT ? (
                  <span className="text-sm text-destructive">Excedeu {formatProfitShareInput(total - PROFIT_SHARE_MAX_CUT)}%</span>
                ) : null}
              </div>
              <Progress value={totalProgress} className={cn('h-1.5', !totalValid && '[&_[data-slot=progress-indicator]]:bg-warning')} />
            </div>

            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" size="sm" onClick={addCut} disabled={busy}>
                <Plus className="size-4" />
                Fatia
              </Button>
              {cuts.length >= 2 ? (
                <Button type="button" variant="outline" size="sm" onClick={splitEvenly} disabled={busy}>
                  Dividir igual
                </Button>
              ) : null}
              {!totalValid && remaining >= PROFIT_SHARE_MIN_CUT && cuts.length > 0 ? (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => fillRemaining(cuts.length - 1)}
                  disabled={busy}
                >
                  Completar 100%
                </Button>
              ) : null}
            </div>

            {cuts.length === 0 ? (
              <p className="text-sm text-muted-foreground">Nenhuma fatia. Toque em &quot;Fatia&quot; para começar.</p>
            ) : (
              <ul className="space-y-2" aria-label="Fatias de repasse">
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

            {error ? <p className="text-sm text-destructive" role="alert">{error}</p> : null}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={busy}>
              Cancelar
            </Button>
            <Button type="button" onClick={handleSave} disabled={busy || !totalValid}>
              {busy ? 'Salvando…' : 'Salvar repasse'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AlertDialog open={pendingRemoveIndex !== null} onOpenChange={(open) => { if (!open) setPendingRemoveIndex(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remover fatia</AlertDialogTitle>
            <AlertDialogDescription>
              {`Deseja remover a fatia de repasse${pendingCutLabel ? ` de ${pendingCutLabel}` : ''}?`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (pendingRemoveIndex !== null) removeCut(pendingRemoveIndex);
              }}
            >
              Confirmar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
