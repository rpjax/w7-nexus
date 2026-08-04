import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { getAdministratorStrawManSettings, upsertStrawManSettings } from '../../api/strawMen/administrator';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import type { StrawManSettings } from '../../api/types';
import { AccountPickerDialog } from '@/components/data/entity-picker-dialog';
import { EntityCombobox } from '@/components/data/entity-combobox';
import { PageHeader } from '@/components/layout/page-header';
import { paymentsPath } from '../../features/strawMen/strawManPaths';
import { formatDateTime, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';
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
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';

function formatFee(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
}

const strawManSettingsSchema = z.object({
  strawManId: z.string().trim().min(1, 'Selecione o laranja.'),
  strawLabel: z.string().nullable(),
  movementFeePercentage: z.number().min(0, 'Informe uma taxa de movimentação válida.'),
});

type StrawManSettingsValues = z.infer<typeof strawManSettingsSchema>;

export function AdminStrawManManagementPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const queryClient = useQueryClient();
  const [pickerOpen, setPickerOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [strawOptions, setStrawOptions] = useState<{ id: string; label: string; description?: string }[]>([]);

  const form = useForm<StrawManSettingsValues>({
    resolver: zodResolver(strawManSettingsSchema),
    defaultValues: {
      strawManId: '',
      strawLabel: null,
      movementFeePercentage: 0,
    },
  });

  const strawManId = form.watch('strawManId');
  const strawLabel = form.watch('strawLabel');
  const feeInput = form.watch('movementFeePercentage');
  const trimmedStrawManId = strawManId.trim();

  const {
    data: settings,
    isLoading: settingsLoading,
    error: settingsError,
  } = useQuery({
    queryKey: ['admin-straw-man-settings', trimmedStrawManId],
    enabled: Boolean(trimmedStrawManId),
    queryFn: async (): Promise<StrawManSettings | null> => {
      const result = await getAdministratorStrawManSettings(trimmedStrawManId);
      if (!result.ok) throw new Error(result.error);
      return result.data ?? null;
    },
  });

  useEffect(() => {
    if (!trimmedStrawManId) {
      form.setValue('movementFeePercentage', 0, { shouldDirty: false });
      return;
    }
    if (settings !== undefined) {
      form.setValue('movementFeePercentage', settings?.movementFeePercentage ?? 0, { shouldDirty: false });
    }
  }, [trimmedStrawManId, settings, form]);

  async function handleSave(values: StrawManSettingsValues) {
    setBusy(true);
    try {
      const result = await upsertStrawManSettings(values.strawManId.trim(), values.movementFeePercentage);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      queryClient.setQueryData(['admin-straw-man-settings', values.strawManId.trim()], result.data ?? null);
      form.setValue('movementFeePercentage', result.data?.movementFeePercentage ?? values.movementFeePercentage, {
        shouldDirty: false,
      });
      notifySuccess('Configurações do laranja atualizadas.');
    } finally {
      setBusy(false);
    }
  }

  function clearSelection() {
    form.reset({
      strawManId: '',
      strawLabel: null,
      movementFeePercentage: 0,
    });
    setStrawOptions([]);
  }

  const dirty = form.formState.isDirty;

  return (
    <div className="space-y-4">
      <PageHeader
        kicker="Laranjas"
        kickerVariant="admin"
        title="Gestão de laranjas"
        description="Configure taxas e parâmetros de qualquer titular laranja do sistema."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Gestão de laranjas' },
        ]}
      />

      <Card className="overflow-hidden border-border/60">
        <CardHeader className="flex flex-row items-start justify-between gap-4 border-b bg-muted/20 pb-4">
          <div className="space-y-2">
            <Badge variant="warning">Administração</Badge>
            <p className="text-lg font-semibold" id="straw-man-admin-title" aria-live="polite">
              {strawLabel ? `@${strawLabel.split(' ')[0]?.replace('@', '')}` : 'Selecione um laranja'}
            </p>
            <p className="text-sm text-muted-foreground">
              Escolha o titular e ajuste a taxa de movimentação aplicada nas transferências.
            </p>
          </div>
          <div
            className="flex size-12 shrink-0 items-center justify-center rounded-xl border border-warning/30 bg-warning/10 text-xs font-bold text-warning"
            aria-hidden="true"
          >
            ADM
          </div>
        </CardHeader>

        <CardContent className="pt-4">
          <Form {...form}>
            <form onSubmit={form.handleSubmit(handleSave)}>
              <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(240px,320px)_1fr]">
                <Card size="sm" className="h-fit border-border/60 bg-card/60">
                  <CardHeader>
                    <CardTitle className="text-base">Titular</CardTitle>
                    <p className="text-sm text-muted-foreground">Busque e selecione a conta laranja.</p>
                  </CardHeader>
                  <CardContent className="space-y-2">
                    <FormField
                      control={form.control}
                      name="strawManId"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Laranja</FormLabel>
                          <FormControl>
                            <EntityCombobox
                              value={field.value || null}
                              onChange={(value) => {
                                field.onChange(value ?? '');
                                if (!value) form.setValue('strawLabel', null);
                              }}
                              options={strawOptions}
                              placeholder="Selecionar laranja"
                              searchPlaceholder="Buscar laranja…"
                            />
                          </FormControl>
                          <div className="flex flex-wrap gap-2">
                            <Button type="button" variant="outline" size="sm" onClick={() => setPickerOpen(true)}>
                              Buscar laranja…
                            </Button>
                            {field.value ? (
                              <Button type="button" variant="ghost" size="sm" onClick={clearSelection}>
                                Limpar
                              </Button>
                            ) : null}
                          </div>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    {strawManId ? (
                      <p className="font-mono text-xs text-muted-foreground" title={strawManId}>
                        {shortId(strawManId, 18)}
                      </p>
                    ) : null}
                  </CardContent>
                </Card>

                <Card size="sm" className="border-border/60 bg-card/60">
                  {!strawManId ? (
                    <CardContent className="py-10 text-center text-muted-foreground">
                      <p>Selecione um laranja para visualizar e editar as configurações.</p>
                    </CardContent>
                  ) : settingsLoading ? (
                    <CardContent className="space-y-4 py-6">
                      <Skeleton className="h-10 w-full" />
                      <Skeleton className="h-24 w-full" />
                      <Skeleton className="h-4 w-2/3" />
                    </CardContent>
                  ) : settingsError ? (
                    <CardContent className="py-6">
                      <Alert variant="destructive">
                        <AlertTitle>Não foi possível carregar as configurações</AlertTitle>
                        <AlertDescription>
                          {settingsError instanceof Error ? settingsError.message : 'Erro desconhecido'}
                        </AlertDescription>
                      </Alert>
                    </CardContent>
                  ) : (
                    <>
                      <CardHeader>
                        <CardTitle className="text-base">Taxa de movimentação</CardTitle>
                        <p className="text-sm text-muted-foreground">
                          Valor atual: <strong>{formatFee(settings?.movementFeePercentage ?? 0)}%</strong>
                        </p>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                          <FormField
                            control={form.control}
                            name="movementFeePercentage"
                            render={({ field }) => (
                              <FormItem className="grid flex-1 gap-2">
                                <FormLabel htmlFor="adminMovementFee">Nova taxa (%)</FormLabel>
                                <FormControl>
                                  <Input
                                    id="adminMovementFee"
                                    type="number"
                                    min={0}
                                    step="0.01"
                                    {...field}
                                    onChange={(event) => field.onChange(Number(event.target.value))}
                                  />
                                </FormControl>
                                <FormMessage />
                              </FormItem>
                            )}
                          />
                          <div className="rounded-lg border border-border bg-muted/30 px-4 py-3">
                            <span className="block text-xs uppercase tracking-wide text-muted-foreground">Prévia</span>
                            <strong className="text-lg">{formatFee(feeInput || 0)}%</strong>
                          </div>
                        </div>

                        <Separator />

                        <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
                          <div>
                            <dt className="text-muted-foreground">Última atualização</dt>
                            <dd>{settings?.updatedAt ? formatDateTime(settings.updatedAt) : 'Nunca configurado'}</dd>
                          </div>
                          {settings?.updatedByAdminId ? (
                            <div>
                              <dt className="text-muted-foreground">Atualizado por</dt>
                              <dd className="font-mono">{shortId(settings.updatedByAdminId, 12)}</dd>
                            </div>
                          ) : null}
                        </dl>
                      </CardContent>
                      <CardFooter className="flex flex-wrap justify-between gap-2 border-t bg-muted/20">
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={paymentsPath('global-admin')}>Ver pagamentos (admin)</Link>
                        </Button>
                        <Button type="submit" disabled={busy || !dirty}>
                          {busy ? 'Salvando…' : 'Salvar configurações'}
                        </Button>
                      </CardFooter>
                    </>
                  )}
                </Card>
              </div>
            </form>
          </Form>
        </CardContent>
      </Card>

      <AccountPickerDialog
        open={pickerOpen}
        onClose={() => setPickerOpen(false)}
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        subtitle="Titular cujas configurações serão gerenciadas."
        onSelected={(row) => {
          const label = `${row.username} · ${shortId(row.id, 8)}`;
          form.setValue('strawManId', row.id, { shouldValidate: true, shouldDirty: true });
          form.setValue('strawLabel', label);
          setStrawOptions([{ id: row.id, label: row.username, description: shortId(row.id, 8) }]);
        }}
      />
    </div>
  );
}
