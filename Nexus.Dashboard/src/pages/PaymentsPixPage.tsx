import { useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { searchAdministratorAccountsPicker } from '../api/accountPickerSources';
import { searchAdministratorOperationsPicker } from '../api/operationPickerSources';
import { generatePix } from '../api/payments';
import type { PixChargeResult } from '../api/types';
import { AccountPickerDialog } from '@/components/data/entity-picker-dialog';
import { OperationPickerDialog } from '@/components/data/entity-picker-dialog';
import { Copy, Link2 } from 'lucide-react';
import { PageHeader } from '@/components/layout/page-header';
import { formatMoney } from '../utils/financeLabels';
import { shortId } from '../utils/format';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { Textarea } from '@/components/ui/textarea';
import { EntityCombobox } from '@/components/data/entity-combobox';
import { cn } from '@/lib/utils';

function parseAmountInput(raw: string): number {
  const normalized = raw.replace(',', '.').trim();
  if (!normalized) return 0;
  const value = Number(normalized);
  return Number.isFinite(value) ? value : 0;
}

const pixFormSchema = z.object({
  operationId: z.string().trim().min(1, 'Selecione uma operação.'),
  operationName: z.string().nullable(),
  amountInput: z.string().trim().min(1, 'Informe um valor maior que zero.').refine(
    (value) => parseAmountInput(value) > 0,
    'Informe um valor maior que zero.',
  ),
  operatorId: z.string().nullable(),
  operatorName: z.string().nullable(),
});

type PixFormValues = z.infer<typeof pixFormSchema>;

export function PaymentsPixPage() {
  const [submitError, setSubmitError] = useState('');
  const [lastResult, setLastResult] = useState<PixChargeResult | null>(null);
  const [pixCopied, setPixCopied] = useState(false);
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);
  const [operationOptions, setOperationOptions] = useState<{ id: string; label: string; description?: string }[]>([]);
  const [operatorOptions, setOperatorOptions] = useState<{ id: string; label: string; description?: string }[]>([]);

  const form = useForm<PixFormValues>({
    resolver: zodResolver(pixFormSchema),
    defaultValues: {
      operationId: '',
      operationName: null,
      amountInput: '',
      operatorId: null,
      operatorName: null,
    },
  });

  const amountInput = form.watch('amountInput');
  const operationId = form.watch('operationId');
  const operationName = form.watch('operationName');
  const amount = useMemo(() => parseAmountInput(amountInput), [amountInput]);

  async function handleGenerate(values: PixFormValues) {
    setSubmitError('');
    setLastResult(null);
    setPixCopied(false);
    const parsedAmount = parseAmountInput(values.amountInput);
    try {
      const result = await generatePix({
        operationId: values.operationId.trim(),
        amount: parsedAmount,
        operatorId: values.operatorId,
      });
      if (!result.ok) {
        setSubmitError(result.error);
        return;
      }
      setLastResult(result.data);
    } catch (ex) {
      setSubmitError(ex instanceof Error ? ex.message : 'Ocorreu um erro inesperado. Tente novamente.');
    }
  }

  async function copyPixCode() {
    if (!lastResult?.pixCode) return;
    await navigator.clipboard.writeText(lastResult.pixCode);
    setPixCopied(true);
  }

  return (
    <div className="space-y-6">
      <PageHeader
        kicker="Financeiro"
        title="Pagamentos PIX"
        description="Gere cobrança PIX vinculada a uma operação. O laranja é definido automaticamente pela credencial selecionada."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Registros de pagamentos', href: '/dashboard/payments' },
          { label: 'Gerar PIX' },
        ]}
      />

      <Form {...form}>
        <form onSubmit={form.handleSubmit(handleGenerate)}>
          <Card className="border-border/60 bg-card/80" aria-labelledby="pix-form-title">
            <CardHeader>
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-2">
                  <Badge variant="info">Cobrança instantânea</Badge>
                  <p className="text-3xl font-bold tracking-tight text-foreground" aria-live="polite">
                    {amount > 0 ? formatMoney(amount) : 'R$ 0,00'}
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {operationName
                      ? `Operação ${operationName}`
                      : 'Selecione a operação e o valor para gerar o PIX.'}
                  </p>
                </div>
                <div
                  className="flex size-14 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-xs font-bold tracking-wider text-primary"
                  aria-hidden="true"
                >
                  PIX
                </div>
              </div>
            </CardHeader>

            <Separator />

            <CardContent className="space-y-6 pt-6">
              {submitError ? (
                <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3" role="alert">
                  <p className="text-sm font-medium text-destructive">Falha ao gerar cobrança</p>
                  <p className="mt-1 text-sm text-destructive/90">{submitError}</p>
                </div>
              ) : null}

              <section className="space-y-3">
                <div className="space-y-1">
                  <Badge variant="outline">Passo 1</Badge>
                  <h2 id="pix-form-title" className="text-base font-semibold text-foreground">Contexto</h2>
                  <p className="text-sm text-muted-foreground">
                    A cobrança será registrada no contexto da operação escolhida.
                  </p>
                </div>
                <FormField
                  control={form.control}
                  name="operationId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Operação</FormLabel>
                      <FormControl>
                        <EntityCombobox
                          value={field.value || null}
                          onChange={(value) => {
                            field.onChange(value ?? '');
                            if (!value) form.setValue('operationName', null);
                          }}
                          options={operationOptions}
                          placeholder="Selecionar operação"
                          searchPlaceholder="Buscar operação…"
                          emptyLabel="Nenhuma operação. Use o botão abaixo para buscar."
                        />
                      </FormControl>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setOperationPickerOpen(true)}
                      >
                        Buscar operação…
                      </Button>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </section>

              <section className="space-y-3">
                <div className="space-y-1">
                  <Badge variant="outline">Passo 2</Badge>
                  <h2 className="text-base font-semibold text-foreground">Valor</h2>
                  <p className="text-sm text-muted-foreground">
                    Informe o montante da cobrança em reais.
                  </p>
                </div>
                <FormField
                  control={form.control}
                  name="amountInput"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel htmlFor="pixAmount">Valor</FormLabel>
                      <FormControl>
                        <label
                          className={cn(
                            'flex items-center gap-2 rounded-lg border border-border/60 bg-background/40 px-3 py-2',
                          )}
                          htmlFor="pixAmount"
                        >
                          <span className="text-sm font-medium text-muted-foreground">R$</span>
                          <Input
                            id="pixAmount"
                            type="text"
                            inputMode="decimal"
                            className="border-0 bg-transparent shadow-none focus-visible:ring-0"
                            placeholder="0,00"
                            autoComplete="off"
                            {...field}
                          />
                        </label>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </section>

              <section className="space-y-3">
                <div className="space-y-1">
                  <Badge variant="outline">Passo 3</Badge>
                  <h2 className="text-base font-semibold text-foreground">Operador</h2>
                  <p className="text-sm text-muted-foreground">
                    Opcional. Com operador, o repasse segue a equipe e a estratégia de credenciais dela.
                    Sem operador, usa a estratégia da operação e divide entre administradores.
                  </p>
                </div>
                <FormField
                  control={form.control}
                  name="operatorId"
                  render={({ field }) => (
                    <FormItem>
                      <div className="flex items-center gap-2">
                        <FormLabel>Operador</FormLabel>
                        <Badge variant="outline">opcional</Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        Filtra repasse e credenciais pela equipe do operador.
                      </p>
                      <FormControl>
                        <EntityCombobox
                          value={field.value}
                          onChange={(value) => {
                            field.onChange(value);
                            if (!value) form.setValue('operatorName', null);
                          }}
                          options={operatorOptions}
                          placeholder="Nenhum operador"
                          searchPlaceholder="Buscar operador…"
                          emptyLabel="Nenhum operador. Use o botão abaixo para buscar."
                        />
                      </FormControl>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setOperatorPickerOpen(true)}
                      >
                        Buscar operador…
                      </Button>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </section>
            </CardContent>

            <CardFooter className="justify-between gap-3">
              <span className="text-sm text-muted-foreground">POST /api/charges/administrator/pix</span>
              <Button
                type="submit"
                disabled={form.formState.isSubmitting || !operationId.trim() || amount <= 0}
              >
                <Link2 className="size-4" />
                {form.formState.isSubmitting ? 'Gerando…' : 'Gerar PIX'}
              </Button>
            </CardFooter>
          </Card>
        </form>
      </Form>

      {lastResult ? (
        <Card className="border-border/60 bg-card/80" aria-live="polite">
          <CardHeader>
            <div className="flex items-start justify-between gap-3">
              <div className="space-y-1">
                <Badge variant="success">Cobrança pronta</Badge>
                <CardTitle>Código PIX gerado</CardTitle>
                <p className="text-sm text-muted-foreground">
                  ID do pagamento:{' '}
                  <span className="font-mono" title={lastResult.id}>{shortId(lastResult.id, 28)}</span>
                </p>
              </div>
              <Badge variant="warning">Pendente</Badge>
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <Textarea
              readOnly
              rows={5}
              value={lastResult.pixCode}
              aria-label="Código PIX copia e cola"
              className="font-mono text-xs"
            />
            <Button type="button" onClick={() => void copyPixCode()}>
              <Copy className="size-4" />
              {pixCopied ? 'Copiado' : 'Copiar código'}
            </Button>
            {pixCopied ? (
              <p className="text-sm text-success">Código PIX copiado para a área de transferência.</p>
            ) : null}
          </CardContent>
        </Card>
      ) : null}

      <OperationPickerDialog
        open={operationPickerOpen}
        onClose={() => setOperationPickerOpen(false)}
        searchOperations={searchAdministratorOperationsPicker}
        title="Selecionar operação"
        subtitle="Todas as operações do sistema — a cobrança PIX será criada no contexto da operação escolhida."
        onSelected={(row) => {
          form.setValue('operationId', row.id, { shouldValidate: true });
          form.setValue('operationName', row.name);
          setOperationOptions([{ id: row.id, label: row.name, description: row.id }]);
        }}
      />

      <AccountPickerDialog
        open={operatorPickerOpen}
        onClose={() => setOperatorPickerOpen(false)}
        searchAccounts={searchAdministratorAccountsPicker}
        title="Conta do operador"
        subtitle="Opcional — define equipe, repasse e credenciais da cobrança."
        onSelected={(row) => {
          form.setValue('operatorId', row.id);
          form.setValue('operatorName', row.username);
          setOperatorOptions([{ id: row.id, label: row.username, description: row.id }]);
        }}
      />
    </div>
  );
}
