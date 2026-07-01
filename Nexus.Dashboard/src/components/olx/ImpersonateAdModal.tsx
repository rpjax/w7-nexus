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
import { OperationPickerModal } from '../OperationPickerModal';
import { searchAdministratorOperationsPicker } from '../../api/operationPickerSources';

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

  if (!open) return null;

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
      <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
        <div className="dialog-card olx-modal" onClick={(e) => e.stopPropagation()}>
          <div className="modal-stack-header">
            <div>
              <h3>Impersonar anúncio</h3>
              <p className="muted small">
                Reserve o anúncio para patch. Apenas anúncios livres podem ser assumidos.
              </p>
            </div>
            <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
              <span aria-hidden="true">×</span>
            </button>
          </div>

          <div className="form-grid">
            <div className="field">
              <label>Operação</label>
              {adminView ? (
                <button
                  type="button"
                  className="olx-picker-field__button olx-picker-field__button--full"
                  onClick={() => setOperationPickerOpen(true)}
                >
                  <span className={operationLabel ? 'olx-picker-field__value' : 'olx-picker-field__placeholder muted'}>
                    {operationLabel ?? 'Selecionar operação…'}
                  </span>
                  <span className="olx-picker-field__chevron" aria-hidden="true">▾</span>
                </button>
              ) : (
                <>
                  <input
                    id="impersonateOpId"
                    className="nexus-input"
                    value={operationId}
                    onChange={(e) => setOperationId(e.target.value)}
                    placeholder="ID da operação vinculada ao anúncio"
                    autoFocus
                  />
                  <p className="form-hint muted small">Informe a operação à qual o anúncio pertence.</p>
                </>
              )}
            </div>
            <div className="field">
              <label htmlFor="impersonateAdUrl">URL do anúncio OLX</label>
              <input
                id="impersonateAdUrl"
                className={`nexus-input${adUrlError ? ' nexus-input--invalid' : ''}`}
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
                className={`form-hint small${adUrlError ? ' form-hint--error' : ' muted'}`}
              >
                {adUrlError ?? 'Cole a URL completa do anúncio. O ID será preenchido automaticamente quando possível.'}
              </p>
            </div>
            <div className="field">
              <label htmlFor="impersonateAdId">ID do anúncio OLX</label>
              <input
                id="impersonateAdId"
                className={`nexus-input${adIdError ? ' nexus-input--invalid' : ''}`}
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
                className={`form-hint small${adIdError ? ' form-hint--error' : ' muted'}`}
              >
                {adIdError
                  ? adIdError
                  : adIdFromUrl
                    ? 'ID extraído da URL.'
                    : 'Somente números (ex.: 1513407983).'}
              </p>
            </div>
          </div>

          <div className="dialog-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>
              Cancelar
            </button>
            <button
              type="button"
              className="btn btn-primary"
              disabled={busy || !canSubmit}
              onClick={handleSubmit}
            >
              {busy ? 'Processando…' : 'Impersonar'}
            </button>
          </div>
        </div>
      </div>

      {adminView ? (
        <OperationPickerModal
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
