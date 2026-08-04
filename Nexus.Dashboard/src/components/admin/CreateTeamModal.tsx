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

const createTeamSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome da equipe.'),
});

type CreateTeamValues = z.infer<typeof createTeamSchema>;

type CreateTeamModalProps = {
  open: boolean;
  busy: boolean;
  operationName?: string;
  onClose: () => void;
  onSubmit: (name: string) => void;
};

export function CreateTeamModal({
  open,
  busy,
  operationName,
  onClose,
  onSubmit,
}: CreateTeamModalProps) {
  const form = useForm<CreateTeamValues>({
    resolver: zodResolver(createTeamSchema),
    defaultValues: { name: '' },
  });

  useEffect(() => {
    if (!open) form.reset({ name: '' });
  }, [open, form]);

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
      <DialogContent className="sm:max-w-md" showCloseButton>
        <DialogHeader>
          <DialogTitle>Nova equipe</DialogTitle>
          <DialogDescription>
            {operationName
              ? `Operação: ${operationName}`
              : 'Crie uma equipe para esta operação.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form
            className="space-y-4"
            onSubmit={form.handleSubmit((values) => onSubmit(values.name.trim()))}
          >
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel htmlFor="createTeamName">Nome da equipe</FormLabel>
                  <FormControl>
                    <Input
                      id="createTeamName"
                      placeholder="Ex.: Equipe Alpha"
                      autoFocus
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
                {busy ? 'Criando…' : 'Criar equipe'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
