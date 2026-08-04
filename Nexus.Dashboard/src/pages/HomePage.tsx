import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { canUseOperatorPanel, canUseOlxPanel, canUseStrawManPanel, isAdministrator, isOlxOperator } from '../auth/roles';
import { PageHeader } from '@/components/layout/page-header';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { useHomeMetrics, type CredentialStat } from '@/hooks/use-home-metrics';
import { cn } from '@/lib/utils';

const toneColor: Record<CredentialStat['tone'], string> = {
  info: 'bg-primary',
  success: 'bg-success',
  danger: 'bg-destructive',
  warn: 'bg-warning',
};

type MetricCardProps = {
  label: string;
  value: string;
  caption?: string;
  tone?: CredentialStat['tone'];
  loading?: boolean;
};

function MetricCard({ label, value, caption, tone, loading }: MetricCardProps) {
  return (
    <Card className="border-border/60 bg-card/80 backdrop-blur-sm">
      <CardHeader className="pb-2">
        <CardDescription className="text-xs font-medium uppercase tracking-wide">{label}</CardDescription>
        {loading ? (
          <Skeleton className="mt-1 h-8 w-24" />
        ) : (
          <CardTitle className="flex items-center gap-2 text-2xl font-bold">
            {tone ? (
              <span className={cn('size-2 shrink-0 rounded-full', toneColor[tone])} aria-hidden="true" />
            ) : null}
            {value}
          </CardTitle>
        )}
      </CardHeader>
      {caption ? (
        <CardContent className="pt-0">
          <p className="text-xs text-muted-foreground">{caption}</p>
        </CardContent>
      ) : null}
    </Card>
  );
}

function QuickAction({ to, admin, children }: { to: string; admin?: boolean; children: React.ReactNode }) {
  return (
    <Button variant="outline" asChild className={cn(admin && 'border-warning/40 text-warning hover:bg-warning/10')}>
      <Link to={to}>{children}</Link>
    </Button>
  );
}

export function HomePage() {
  const { user } = useAuth();
  const operatorPanel = canUseOperatorPanel(user);
  const adminView = isAdministrator(user);
  const strawManPanel = canUseStrawManPanel(user);
  const olxPanel = canUseOlxPanel(user);
  const olxOperator = isOlxOperator(user);

  const { data: metrics, isLoading: metricsLoading } = useHomeMetrics({
    operatorPanel,
    adminView,
    olxPanel,
    olxOperator,
  });

  const homeKicker = adminView && operatorPanel
    ? 'Administração e operação'
    : adminView
      ? 'Administração'
      : olxOperator
        ? 'Painel OLX'
        : operatorPanel
          ? 'Painel do operador'
          : strawManPanel
            ? 'Painel laranja'
            : 'Dashboard';

  const homeSubtitle = adminView && operatorPanel
    ? 'Visão operacional das suas alocações e gestão global do sistema.'
    : adminView
      ? 'Gerencie o catálogo global de operações e contas do sistema.'
      : olxOperator
        ? 'Impersonação de anúncios OLX, patch de preços e liberação de slots.'
        : operatorPanel
          ? 'Acompanhe suas operações alocadas, pagamentos e credenciais de gateway.'
          : strawManPanel
            ? 'Acompanhe pagamentos e configurações da sua conta laranja.'
            : 'Nenhum papel operacional ou administrativo associado à sua sessão.';

  return (
    <div className="space-y-6">
      <PageHeader
        kicker={homeKicker}
        kickerVariant={adminView ? 'admin' : 'default'}
        title="Visão geral"
        description={homeSubtitle}
        breadcrumbs={[{ label: 'Dashboard' }]}
      />

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {operatorPanel ? (
          <MetricCard
            label="Minhas operações"
            value={metrics?.myOperationsTotal?.toString() ?? '—'}
            caption="Operações em que você está alocado"
            tone="info"
            loading={metricsLoading}
          />
        ) : null}
        {adminView ? (
          <MetricCard
            label="Operações no sistema"
            value={metrics?.systemOperationsTotal?.toString() ?? '—'}
            caption="Total no repositório global"
            tone="info"
            loading={metricsLoading}
          />
        ) : null}
        {operatorPanel ? (
          <>
            <MetricCard
              label="Credencial Frendz"
              value={metrics?.frendz.status ?? '—'}
              caption={metrics?.frendz.caption}
              tone={metrics?.frendz.tone}
              loading={metricsLoading}
            />
            <MetricCard
              label="Credencial SigiloPay"
              value={metrics?.sigiloPay.status ?? '—'}
              caption={metrics?.sigiloPay.caption}
              tone={metrics?.sigiloPay.tone}
              loading={metricsLoading}
            />
            <MetricCard
              label="Credencial Wintech"
              value={metrics?.wintech.status ?? '—'}
              caption={metrics?.wintech.caption}
              tone={metrics?.wintech.tone}
              loading={metricsLoading}
            />
          </>
        ) : null}
        {olxPanel ? (
          <MetricCard
            label="Anúncios OLX"
            value={metrics?.olxPatchesTotal?.toString() ?? '—'}
            caption={olxOperator ? 'Patches sob seu controle' : 'Registros globais de patch'}
            tone="success"
            loading={metricsLoading}
          />
        ) : null}
        <MetricCard
          label="Sessão"
          value={user?.username ?? '—'}
          caption={user?.roles.length ? user.roles.join(', ') : 'Autenticado'}
          tone="success"
        />
      </section>

      <Card className="border-border/60 bg-card/80">
        <CardHeader>
          <CardTitle>Atalhos</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {operatorPanel ? (
              <>
                <QuickAction to="/dashboard/operations">Minhas operações</QuickAction>
                <QuickAction to="/dashboard/payments">Meus pagamentos</QuickAction>
                <QuickAction to="/dashboard/payments/pix">Gerar cobrança PIX</QuickAction>
                <QuickAction to="/dashboard/gateways/frendz">Credenciais Frendz</QuickAction>
                <QuickAction to="/dashboard/gateways/sigilopay">Credenciais SigiloPay</QuickAction>
                <QuickAction to="/dashboard/gateways/wintech">Credenciais Wintech</QuickAction>
              </>
            ) : null}
            {adminView ? (
              <>
                <QuickAction to="/dashboard/admin/operations" admin>Todas as operações</QuickAction>
                <QuickAction to="/dashboard/admin/payments" admin>Todos os pagamentos</QuickAction>
                <QuickAction to="/dashboard/accounts" admin>Contas</QuickAction>
                <QuickAction to="/dashboard/admin/straw-men" admin>Gestão de laranjas</QuickAction>
              </>
            ) : null}
            {strawManPanel ? (
              <>
                <QuickAction to="/dashboard/straw-man/payments">Meus pagamentos</QuickAction>
                <QuickAction to="/dashboard/straw-man/settings">Minhas configurações</QuickAction>
              </>
            ) : null}
            {olxPanel ? (
              <>
                {olxOperator ? (
                  <QuickAction to="/dashboard/olx/ads">OLX — Meus anúncios</QuickAction>
                ) : null}
                {adminView ? (
                  <QuickAction to="/dashboard/olx/admin/ads" admin>OLX — Gestão global</QuickAction>
                ) : null}
              </>
            ) : null}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
