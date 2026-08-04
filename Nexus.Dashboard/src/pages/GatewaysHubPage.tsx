import { Link } from 'react-router-dom';
import { PageHeader } from '@/components/layout/page-header';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';

const GATEWAYS = [
  {
    to: '/dashboard/gateways/frendz',
    name: 'Frendz',
    caption: 'Credenciais e gestão do integrador ativo',
    active: true,
  },
  {
    to: '/dashboard/gateways/sigilopay',
    name: 'SigiloPay',
    caption: 'Credenciais (chave pública e secreta) e integração PIX',
    active: false,
  },
  {
    to: '/dashboard/gateways/wintech',
    name: 'Wintech',
    caption: 'API Wintech Pagamentos — chaves públicas/secretas e PIX',
    active: false,
  },
] as const;

export function GatewaysHubPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        kicker="Integração"
        title="Gateways"
        description="Escolha um gateway para acessar credenciais e regras de integração específicas."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Gateways' },
        ]}
      />

      <Card className="border-border/60 bg-card/80">
        <CardHeader>
          <CardTitle>Seleção de gateway</CardTitle>
          <CardDescription>
            Cada gateway possui contexto e dados próprios. O menu acima mantém a navegação escalável.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {GATEWAYS.map((gateway) => (
              <Card
                key={gateway.to}
                className={cn(
                  'transition-colors hover:border-primary/40',
                  gateway.active && 'border-primary/50 bg-primary/5',
                )}
              >
                <Link to={gateway.to} className="flex flex-col gap-1 p-4">
                  <span className="font-medium text-foreground">{gateway.name}</span>
                  <span className="text-sm text-muted-foreground">{gateway.caption}</span>
                </Link>
              </Card>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
