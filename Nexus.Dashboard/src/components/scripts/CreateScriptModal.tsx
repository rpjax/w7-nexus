import { useEffect } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
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
import { HostPatternEditor } from './HostPatternEditor';
import { ResolutionModeBadge } from './ResolutionModeBadge';

const createScriptSchema = z.object({
  name: z.string().trim().min(1, 'Informe o nome do script.'),
  description: z.string(),
  priority: z.number().int().min(0, 'Prioridade deve ser zero ou maior.'),
  hostPatterns: z.array(z.string()),
});

type CreateScriptValues = z.infer<typeof createScriptSchema>;

type CreateScriptModalProps = {
  open: boolean;
  busy: boolean;
  onClose: () => void;
  onSubmit: (payload: {
    name: string;
    hostPatterns: string[];
    priority: number;
    description: string | null;
  }) => void;
};

export function CreateScriptModal({ open, busy, onClose, onSubmit }: CreateScriptModalProps) {
  const form = useForm<CreateScriptValues>({
    resolver: zodResolver(createScriptSchema),
    defaultValues: {
      name: '',
      description: '',
      priority: 0,
      hostPatterns: [],
    },
  });

  const hostPatterns = form.watch('hostPatterns');

  useEffect(() => {
    if (!open) {
      form.reset({
        name: '',
        description: '',
        priority: 0,
        hostPatterns: [],
      });
    }
  }, [open, form]);

  function handleSubmit(values: CreateScriptValues) {
    onSubmit({
      name: values.name.trim(),
      hostPatterns: values.hostPatterns,
      priority: values.priority,
      description: values.description.trim() || null,
    });
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <p className="text-xs uppercase tracking-wide text-muted-foreground">Admin · Scripts</p>
          <DialogTitle>Novo script</DialogTitle>
          <DialogDescription>
            Registre um patch de runtime no inventário central.
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form className="space-y-4" onSubmit={form.handleSubmit(handleSubmit)}>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem className="sm:col-span-1">
                    <FormLabel htmlFor="scriptName">Nome</FormLabel>
                    <FormControl>
                      <Input
                        id="scriptName"
                        placeholder="runtime, olx, …"
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
                name="priority"
                render={({ field }) => (
                  <FormItem className="sm:col-span-1">
                    <FormLabel htmlFor="scriptPriority">Prioridade</FormLabel>
                    <FormControl>
                      <div className="flex items-center gap-1">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          disabled={busy}
                          onClick={() => field.onChange(Math.max(0, field.value - 1))}
                          aria-label="Diminuir prioridade"
                        >
                          −
                        </Button>
                        <Input
                          id="scriptPriority"
                          type="number"
                          className="text-center"
                          min={0}
                          title="Menor valor injeta primeiro em lookups por host"
                          {...field}
                          onChange={(event) => field.onChange(Number(event.target.value))}
                        />
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          disabled={busy}
                          onClick={() => field.onChange(field.value + 1)}
                          aria-label="Aumentar prioridade"
                        >
                          +
                        </Button>
                      </div>
                    </FormControl>
                    <p className="text-xs text-muted-foreground">Menor valor = injeta primeiro.</p>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem className="sm:col-span-2">
                    <FormLabel htmlFor="scriptDesc">Descrição</FormLabel>
                    <FormControl>
                      <Textarea
                        id="scriptDesc"
                        rows={2}
                        placeholder="Opcional — contexto operacional do patch"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <section className="flex flex-col gap-3" aria-labelledby="script-hosts-label">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <h4 id="script-hosts-label" className="text-sm font-medium">
                    Host patterns
                    <span className="ml-2 text-xs font-normal text-muted-foreground">opcional</span>
                  </h4>
                  <p className="text-xs text-muted-foreground">
                    Define em quais hosts o script entra no resolve por URL.
                  </p>
                </div>
                <ResolutionModeBadge hostPatterns={hostPatterns} />
              </div>
              <FormField
                control={form.control}
                name="hostPatterns"
                render={({ field }) => (
                  <FormItem>
                    <FormControl>
                      <HostPatternEditor
                        patterns={field.value}
                        onChange={field.onChange}
                        disabled={busy}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </section>

            <DialogFooter>
              <Button type="button" variant="ghost" onClick={onClose} disabled={busy}>
                Cancelar
              </Button>
              <Button type="submit" disabled={busy}>
                {busy ? 'Criando…' : 'Criar script'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
