import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getStrawManSettings } from '../../api/strawMen/strawMan';
import { useAuth } from '../../auth/AuthContext';
import { PageHeader } from '@/components/layout/page-header';
import { paymentsPath } from '../../features/strawMen/strawManPaths';
import { formatDateTime, shortId } from '../../utils/format';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';

function formatFee(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
}

export function StrawManSelfSettingsPage() {
  const { user } = useAuth();
  const { data: settings, isLoading, error } = useQuery({
    queryKey: ['straw-man-settings'],
    queryFn: async () => {
      const result = await getStrawManSettings();
      if (!result.ok) throw new Error(result.error);
      return result.data ?? null;
    },
  });

  const fee = settings?.movementFeePercentage ?? 0;

  return (
    <div className="space-y-4">
      <PageHeader
        kicker="Laranjas"
        title="Minhas configurações"
        description="Parâmetros da sua conta laranja definidos pela administração."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Minhas configurações' },
        ]}
      />

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Não foi possível carregar as configurações</AlertTitle>
          <AlertDescription>{error instanceof Error ? error.message : 'Erro desconhecido'}</AlertDescription>
        </Alert>
      ) : null}

      <Card className="overflow-hidden border-border/60">
        <CardHeader className="flex flex-row items-start justify-between gap-4 border-b bg-muted/20 pb-4">
          <div className="space-y-2">
            <Badge variant="secondary">Conta laranja</Badge>
            <p className="text-lg font-semibold" aria-live="polite">
              @{user?.username ?? '—'}
            </p>
            <p className="text-sm text-muted-foreground">
              Taxa aplicada quando saldos são movimentados entre contas.
            </p>
          </div>
          <div
            className="flex size-12 shrink-0 items-center justify-center rounded-xl border border-border bg-muted/40 text-xs font-bold"
            aria-hidden="true"
          >
            SM
          </div>
        </CardHeader>

        <CardContent className="pt-4">
          {isLoading ? (
            <div className="space-y-4 py-2">
              <Skeleton className="h-28 w-full rounded-xl" />
              <Skeleton className="h-4 w-full" />
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
              </div>
            </div>
          ) : (
            <section className="space-y-4" id="straw-man-self-settings">
              <div className="rounded-xl border border-border bg-muted/20 p-4">
                <span className="block text-xs uppercase tracking-wide text-muted-foreground">
                  Taxa de movimentação
                </span>
                <strong className="text-3xl font-bold">{formatFee(fee)}%</strong>
                <p className="mt-1 text-sm text-muted-foreground">
                  Percentual retido em movimentações entre contas do mesmo titular ou de terceiros.
                </p>
              </div>

              <Separator />

              <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
                <div>
                  <dt className="text-muted-foreground">Identificador</dt>
                  <dd className="font-mono">{settings?.strawManId ? shortId(settings.strawManId, 16) : '—'}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground">Última atualização</dt>
                  <dd>{settings?.updatedAt ? formatDateTime(settings.updatedAt) : 'Padrão do sistema (0%)'}</dd>
                </div>
              </dl>

              <CardFooter className="px-0 pb-0">
                <Button size="sm" asChild>
                  <Link to={paymentsPath('self')}>Ver meus pagamentos</Link>
                </Button>
              </CardFooter>
            </section>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
