import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../auth/AuthContext';
import { isAdministrator } from '../../auth/roles';
import {
  extractOlxAdId,
  isValidAdUrl,
  isValidOlxAdId,
  normalizeAdUrl,
  olxAdIdValidationMessage,
  olxAdUrlValidationMessage,
  parseOlxAdIdInput,
} from '../../features/olx/adPatchDisplay';
import { OperationPickerDialog } from '@/components/data/entity-picker-dialog';
import { searchAdministratorOperationsPicker } from '../../api/operationPickerSources';
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
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { OlxPickerField } from './OlxFilterPanel';

type ImpersonateAdModalProps = {
  open: boolean;
  busy: boolean;
  defaultOperationId?: string;
  defaultOperationLabel?: string | null;
  defaultAdId?: string;
  defaultAdUrl?: string;
  onClose: () => void;
  onSubmit: (operationId: string, adId: string, adUrl: string) => void;
};

export function ImpersonateAdModal({
  open,
  busy,
  defaultOperationId = '',
  defaultOperationLabel = null,
  defaultAdId = '',
  defaultAdUrl = '',
  onClose,
  onSubmit,
}: ImpersonateAdModalProps) {
  const { user } = useAuth();
  const adminView = isAdministrator(user);
  const [operationId, setOperationId] = useState(defaultOperationId);
  const [operationLabel, setOperationLabel] = useState<string | null>(defaultOperationLabel);
  const [adId, setAdId] = useState('');
  const [adUrl, setAdUrl] = useState('');
  const [adIdTouched, setAdIdTouched] = useState(false);
  const [adUrlTouched, setAdUrlTouched] = useState(false);
  const [adIdFromUrl, setAdIdFromUrl] = useState(false);
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);

  useEffect(() => {
    if (!open) {
      setOperationId(defaultOperationId);
      setOperationLabel(defaultOperationLabel);
      setAdId('');
      setAdUrl('');
      setAdIdTouched(false);
      setAdUrlTouched(false);
      setAdIdFromUrl(false);
    } else {
      setOperationId(defaultOperationId);
      setOperationLabel(defaultOperationLabel);
      setAdId(defaultAdId);
      setAdUrl(defaultAdUrl);
      setAdIdTouched(false);
      setAdUrlTouched(false);
      setAdIdFromUrl(false);
    }
  }, [open, defaultOperationId, defaultOperationLabel, defaultAdId, defaultAdUrl]);

  const adIdError = useMemo(() => {
    if (!adIdTouched) return null;
    return olxAdIdValidationMessage(adId);
  }, [adId, adIdTouched]);

  const adUrlError = useMemo(() => {
    if (!adUrlTouched) return null;
    return olxAdUrlValidationMessage(adUrl);
  }, [adUrl, adUrlTouched]);

  const canSubmit = Boolean(operationId.trim()) && isValidOlxAdId(adId) && isValidAdUrl(adUrl);

  function applyAdIdInput(raw: string) {
    const parsed = parseOlxAdIdInput(raw);
    setAdId(parsed.value);
    setAdIdFromUrl(parsed.fromUrl);
    setAdIdTouched(true);
  }

  function applyAdUrlInput(raw: string) {
    setAdUrl(raw);
    setAdUrlTouched(true);

    const extracted = extractOlxAdId(raw);
    if (extracted) {
      setAdId(extracted);
      setAdIdFromUrl(true);
      setAdIdTouched(true);
    }
  }

  function handleAdUrlPaste(event: React.ClipboardEvent<HTMLInputElement>) {
    const pasted = event.clipboardData.getData('text');
    if (!pasted.trim()) return;

    event.preventDefault();
    applyAdUrlInput(pasted);
  }

  function handleAdIdPaste(event: React.ClipboardEvent<HTMLInputElement>) {
    const pasted = event.clipboardData.getData('text');
    const extracted = extractOlxAdId(pasted);
    if (!extracted) return;

    event.preventDefault();
    setAdId(extracted);
    setAdIdFromUrl(extracted !== pasted.trim());
    setAdIdTouched(true);

    if (/https?:\/\/|olx\.com|[/?]/i.test(pasted)) {
      setAdUrl(pasted.trim());
      setAdUrlTouched(true);
    }
  }

  function handleSubmit() {
    const op = operationId.trim();
    const extracted = extractOlxAdId(adId) ?? adId.trim();
    const normalizedUrl = normalizeAdUrl(adUrl);
    if (!op || !isValidOlxAdId(extracted) || !isValidAdUrl(normalizedUrl)) {
      setAdIdTouched(true);
      setAdUrlTouched(true);
      return;
    }
    onSubmit(op, extracted, normalizedUrl);
  }

  return (
    <>
      <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
        <DialogContent className="sm:max-w-lg" showCloseButton>
          <DialogHeader>
            <DialogTitle>Impersonar anúncio</DialogTitle>
            <DialogDescription>
              Reserve o anúncio para patch. Apenas anúncios livres podem ser assumidos.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4">
            <div className="grid gap-2">
              <Label>Operação</Label>
              {adminView ? (
                <OlxPickerField
                  label=""
                  value={operationLabel}
                  placeholder="Selecionar operação…"
                  onPick={() => setOperationPickerOpen(true)}
                  fullWidth
                />
              ) : (
                <>
                  <Input
                    id="impersonateOpId"
                    value={operationId}
                    onChange={(e) => setOperationId(e.target.value)}
                    placeholder="ID da operação vinculada ao anúncio"
                    autoFocus
                  />
                  <p className="text-xs text-muted-foreground">Informe a operação à qual o anúncio pertence.</p>
                </>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="impersonateAdUrl">URL do anúncio OLX</Label>
              <Input
                id="impersonateAdUrl"
                className={cn(adUrlError && 'border-destructive')}
                value={adUrl}
                type="url"
                inputMode="url"
                autoComplete="off"
                spellCheck={false}
                onChange={(e) => applyAdUrlInput(e.target.value)}
                onPaste={handleAdUrlPaste}
                onBlur={() => setAdUrlTouched(true)}
                placeholder="https://www.olx.com.br/…"
                aria-invalid={adUrlError ? true : undefined}
                aria-describedby="impersonateAdUrlHint"
                autoFocus={adminView && Boolean(operationId)}
              />
              <p
                id="impersonateAdUrlHint"
                className={cn('text-xs', adUrlError ? 'text-destructive' : 'text-muted-foreground')}
              >
                {adUrlError ?? 'Cole a URL completa do anúncio. O ID será preenchido automaticamente quando possível.'}
              </p>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="impersonateAdId">ID do anúncio OLX</Label>
              <Input
                id="impersonateAdId"
                className={cn(adIdError && 'border-destructive')}
                value={adId}
                inputMode="numeric"
                autoComplete="off"
                spellCheck={false}
                onChange={(e) => applyAdIdInput(e.target.value)}
                onPaste={handleAdIdPaste}
                onBlur={() => setAdIdTouched(true)}
                placeholder="1513407983"
                aria-invalid={adIdError ? true : undefined}
                aria-describedby="impersonateAdIdHint"
              />
              <p
                id="impersonateAdIdHint"
                className={cn('text-xs', adIdError ? 'text-destructive' : 'text-muted-foreground')}
              >
                {adIdError
                  ? adIdError
                  : adIdFromUrl
                    ? 'ID extraído da URL.'
                    : 'Somente números (ex.: 1513407983).'}
              </p>
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="ghost" onClick={onClose} disabled={busy}>
              Cancelar
            </Button>
            <Button type="button" disabled={busy || !canSubmit} onClick={handleSubmit}>
              {busy ? 'Processando…' : 'Impersonar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {adminView ? (
        <OperationPickerDialog
          open={operationPickerOpen}
          title="Operação do anúncio"
          subtitle="Escolha a operação à qual o anúncio OLX está vinculado."
          searchOperations={searchAdministratorOperationsPicker}
          onClose={() => setOperationPickerOpen(false)}
          onSelected={(row) => {
            setOperationId(row.id);
            setOperationLabel(row.name);
          }}
        />
      ) : null}
    </>
  );
}
