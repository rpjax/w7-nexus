import { useEffect } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

const createOperationSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome da operação.'),
  description: z.string(),
});

type CreateOperationValues = z.infer<typeof createOperationSchema>;

type CreateOperationModalProps = {
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSubmit: (name: string, description: string | null) => void;
};

export function CreateOperationModal({
  open,
  busy,
  onClose,
  onSubmit,
}: CreateOperationModalProps) {
  const form = useForm<CreateOperationValues>({
    resolver: zodResolver(createOperationSchema),
    defaultValues: { name: '', description: '' },
  });

  useEffect(() => {
    if (!open) form.reset({ name: '', description: '' });
  }, [open, form]);

  function handleSubmit(values: CreateOperationValues) {
    onSubmit(values.name.trim(), values.description.trim() || null);
  }

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
      <DialogContent className="sm:max-w-md" showCloseButton>
        <DialogHeader>
          <DialogTitle>Nova operação</DialogTitle>
          <DialogDescription>
            Registre uma operação no repositório central.
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form className="grid gap-4" onSubmit={form.handleSubmit(handleSubmit)}>
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel htmlFor="createOpName">Nome</FormLabel>
                  <FormControl>
                    <Input
                      id="createOpName"
                      placeholder="Ex.: Operação Atlas"
                      autoFocus
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel htmlFor="createOpDesc">Descrição</FormLabel>
                  <FormControl>
                    <Textarea
                      id="createOpDesc"
                      rows={2}
                      placeholder="Contexto e escopo da operação"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose} disabled={busy}>
                Cancelar
              </Button>
              <Button type="submit" disabled={busy}>
                {busy ? 'Registrando…' : 'Registrar operação'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
