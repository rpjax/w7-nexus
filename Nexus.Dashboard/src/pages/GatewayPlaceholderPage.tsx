import { PageHeader } from '@/components/layout/page-header';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Card, CardContent } from '@/components/ui/card';

type GatewayPlaceholderPageProps = {
  title: string;
};

export function GatewayPlaceholderPage({ title }: GatewayPlaceholderPageProps) {
  return (
    <div className="space-y-6">
      <PageHeader
        title={title}
        description="Espaço reservado para o próximo módulo de integração de pagamento."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Gateways', href: '/dashboard/gateways' },
          { label: title },
        ]}
      />

      <Card className="border-border/60 bg-card/80">
        <CardContent className="pt-6">
          <Alert>
            <AlertTitle>Gateway em planejamento</AlertTitle>
            <AlertDescription>
              A estrutura de navegação já está pronta. Quando o integrador for implementado, os casos de uso entram aqui sem redesenhar o dashboard.
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    </div>
  );
}
