import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { API_FLOWS, API_GROUPS, API_ENDPOINTS } from '../../features/api-docs/catalog';
import type { ApiDocsView } from '../../features/api-docs/types';

type ApiDocsOverviewProps = {
  onNavigate: (view: ApiDocsView) => void;
};

const QUICK_START = [
  {
    step: '1',
    title: 'Autentique-se',
    text: 'Obtenha um JWT via sign-in e envie em Authorization: Bearer em todas as rotas protegidas.',
    flowId: 'auth-jwt',
  },
  {
    step: '2',
    title: 'Estruture a operação',
    text: 'Crie operação, equipes e atribua operadores antes de qualquer cobrança.',
    flowId: 'operations-setup',
  },
  {
    step: '3',
    title: 'Configure o gateway',
    text: 'Cadastre credenciais Frendz, Wintech ou SigiloPay e vincule à operação.',
    flowId: 'gateways-setup',
  },
  {
    step: '4',
    title: 'Gere e acompanhe PIX',
    text: 'Crie cobranças e use SignalR para status em tempo real.',
    flowId: 'payments-pix',
  },
];

const accentDot: Record<string, string> = {
  blue: 'bg-primary',
  green: 'bg-success',
  amber: 'bg-warning',
  violet: 'bg-purple-400',
  rose: 'bg-rose-400',
};

const httpStatusItems = [
  { code: '200', label: 'Sucesso', variant: 'success' as const },
  { code: '304', label: 'Cache (ETag)', variant: 'info' as const },
  { code: '401', label: 'Sem token', variant: 'warning' as const },
  { code: '403', label: 'Sem permissão', variant: 'warning' as const },
  { code: '422', label: 'Validação', variant: 'destructive' as const },
];

export function ApiDocsOverview({ onNavigate }: ApiDocsOverviewProps) {
  return (
    <div className="space-y-6">
      <Card className="border-primary/20 bg-gradient-to-br from-primary/8 to-card/50">
        <CardHeader>
          <p className="text-[0.72rem] font-semibold uppercase tracking-wider text-primary">
            Nexus API · v1
          </p>
          <CardTitle className="text-[clamp(1.35rem,4vw,1.75rem)]">
            Guia completo de integração
          </CardTitle>
          <CardDescription className="max-w-[60ch] text-[0.92rem] leading-relaxed">
            Esta documentação explica <em>como</em> usar a API — não apenas lista endpoints.
            Comece pelos fluxos guiados para entender o contexto de cada chamada, ou navegue
            pela referência técnica quando já souber o que precisa.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-5">
            <div>
              <span className="block text-2xl font-bold">{API_FLOWS.length}</span>
              <span className="text-xs text-muted-foreground">Fluxos explicativos</span>
            </div>
            <div>
              <span className="block text-2xl font-bold">{API_ENDPOINTS.length}</span>
              <span className="text-xs text-muted-foreground">Endpoints</span>
            </div>
            <div>
              <span className="block text-2xl font-bold">{API_GROUPS.length}</span>
              <span className="text-xs text-muted-foreground">Domínios</span>
            </div>
          </div>
        </CardContent>
      </Card>

      <section>
        <h3 className="mb-1 text-[1.05rem] font-semibold">Por onde começar?</h3>
        <p className="mb-3.5 text-[0.88rem] text-muted-foreground">
          Sequência recomendada para colocar um ambiente Nexus em produção.
        </p>
        <ol className="m-0 flex list-none flex-col gap-3 p-0">
          {QUICK_START.map((item) => (
            <li key={item.step}>
              <Card className="bg-card/45">
                <CardContent className="flex gap-3.5 pt-4">
                  <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-primary/15 text-[0.8rem] font-bold text-primary">
                    {item.step}
                  </span>
                  <div>
                    <strong>{item.title}</strong>
                    <p className="my-1 mb-2 text-[0.86rem] leading-normal text-muted-foreground">{item.text}</p>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="h-auto px-0 text-[0.82rem] text-primary"
                      onClick={() => onNavigate({ kind: 'flow', id: item.flowId })}
                    >
                      Ver fluxo guiado →
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </li>
          ))}
        </ol>
      </section>

      <section>
        <h3 className="mb-1 text-[1.05rem] font-semibold">Fluxos guiados</h3>
        <p className="mb-3.5 text-[0.88rem] text-muted-foreground">
          Cada fluxo explica o contexto, armadilhas comuns e referência técnica sob demanda.
        </p>
        <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2 xl:grid-cols-3">
          {API_FLOWS.map((flow) => (
            <Button
              key={flow.id}
              type="button"
              variant="ghost"
              className="flex h-auto cursor-pointer flex-col items-start gap-1.5 rounded-xl border border-border bg-card/45 p-4 text-left transition-[border-color,transform] hover:-translate-y-px hover:border-primary/35"
              onClick={() => onNavigate({ kind: 'flow', id: flow.id })}
            >
              <span
                className={cn('size-2 shrink-0 rounded-full', accentDot[flow.accent] ?? accentDot.blue)}
                aria-hidden="true"
              />
              <span className="text-[0.9rem] font-semibold">{flow.title}</span>
              <span className="text-[0.8rem] leading-snug text-muted-foreground">{flow.description}</span>
              <span className="mt-auto text-[0.72rem] text-primary">
                {flow.steps.length} passos · ~{flow.estimatedMinutes} min
              </span>
            </Button>
          ))}
        </div>
      </section>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-[1.05rem]">Autenticação</CardTitle>
            <CardDescription>
              A API é stateless: cada requisição protegida precisa do JWT no header.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <pre className="block overflow-x-auto rounded-lg border border-border bg-background/80 p-2.5 text-[0.8rem]">
              Authorization: Bearer {'{accessToken}'}
            </pre>
            <ul className="list-disc pl-5 text-[0.86rem] leading-relaxed text-muted-foreground">
              <li><strong>Público</strong> — checkout PIX, resolução de scripts, OLX victim</li>
              <li><strong>JWT Bearer</strong> — painéis operador, admin e laranja</li>
              <li><strong>Token mestre</strong> — bootstrap do primeiro administrador</li>
            </ul>
            <Button type="button" size="sm" className="self-start" onClick={() => onNavigate({ kind: 'flow', id: 'auth-jwt' })}>
              Entender autenticação →
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-[1.05rem]">Códigos HTTP</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-1.5">
              {httpStatusItems.map((item) => (
                <Badge
                  key={item.code}
                  variant={item.variant}
                  className="gap-1.5 rounded-lg px-2 py-1.5 text-xs"
                >
                  <span className="font-bold">{item.code}</span>
                  <span className="font-normal opacity-80">{item.label}</span>
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      <section>
        <h3 className="mb-1 text-[1.05rem] font-semibold">Referência por domínio</h3>
        <div className="grid grid-cols-1 gap-2.5 md:grid-cols-2">
          {API_GROUPS.map((group) => (
            <Button
              key={group.id}
              type="button"
              variant="ghost"
              className="flex h-auto cursor-pointer flex-col items-start gap-1.5 rounded-xl border border-border bg-card/45 p-4 text-left transition-[border-color,transform] hover:-translate-y-px hover:border-primary/35"
              onClick={() => onNavigate({ kind: 'group', id: group.id })}
            >
              <span className="text-[0.9rem] font-semibold">{group.title}</span>
              <span className="text-[0.8rem] leading-snug text-muted-foreground">{group.description}</span>
            </Button>
          ))}
        </div>
      </section>
    </div>
  );
}
