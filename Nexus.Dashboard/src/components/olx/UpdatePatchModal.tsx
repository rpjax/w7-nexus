import { useEffect, useState } from 'react';
import type { OlxOperatorAdPatchRow } from '../../api/olx/types';
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

type UpdatePatchModalProps = {
  open: boolean;
  busy: boolean;
  row: OlxOperatorAdPatchRow | null;
  onClose: () => void;
  onSubmit: (originalPrice: number | null, promotionalPrice: number | null) => void;
};

function toInput(value?: number | null): string {
  if (value === null || value === undefined) return '';
  return String(value);
}

export function UpdatePatchModal({
  open,
  busy,
  row,
  onClose,
  onSubmit,
}: UpdatePatchModalProps) {
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
    <Dialog open={open && row !== null} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
      <DialogContent className="sm:max-w-md" showCloseButton>
        <DialogHeader>
          <DialogTitle>Editar preços patchados</DialogTitle>
          <DialogDescription>
            Anúncio <strong>{row?.adId}</strong> — informe ao menos um preço visível para a vítima.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4">
          <div className="grid gap-2">
            <Label htmlFor="patchOriginalPrice">Preço original (R$)</Label>
            <Input
              id="patchOriginalPrice"
              inputMode="decimal"
              value={originalPrice}
              onChange={(e) => setOriginalPrice(e.target.value)}
              placeholder="Ex.: 1999.90"
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="patchPromoPrice">Preço promocional (R$)</Label>
            <Input
              id="patchPromoPrice"
              inputMode="decimal"
              value={promotionalPrice}
              onChange={(e) => setPromotionalPrice(e.target.value)}
              placeholder="Ex.: 1499.90"
            />
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </Button>
          <Button type="button" disabled={busy || !hasPrice} onClick={handleSubmit}>
            {busy ? 'Salvando…' : 'Salvar preços'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
