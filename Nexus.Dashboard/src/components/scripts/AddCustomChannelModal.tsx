import { useEffect, useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

type AddCustomChannelModalProps = {
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSubmit: (customName: string) => void;
};

export function AddCustomChannelModal({ open, busy, onClose, onSubmit }: AddCustomChannelModalProps) {
  const [name, setName] = useState('');

  useEffect(() => {
    if (!open) setName('');
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Canal customizado</DialogTitle>
          <DialogDescription>Ex.: beta, canary, qa</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <Label htmlFor="customChannelName">Nome</Label>
          <Input
            id="customChannelName"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="beta"
            autoFocus
          />
        </div>

        <DialogFooter>
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy}>
            Cancelar
          </Button>
          <Button
            type="button"
            disabled={busy || !name.trim()}
            onClick={() => onSubmit(name.trim())}
          >
            {busy ? 'Adicionando…' : 'Adicionar canal'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
