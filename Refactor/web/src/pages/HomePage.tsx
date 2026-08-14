import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  ArrowRight,
  FileText,
  Handshake,
  Layers,
  PieChart,
  Receipt,
  ScrollText,
  Shield,
  UserRound,
  Users,
  Wallet,
} from 'lucide-react';
import { getMyProfile } from '@/api/auth';
import { useAuth } from '@/auth/AuthContext';
import { useHubAccess, type HubAccess } from '@/auth/MandateContext';
import type { MyProfile } from '@/auth/types';
import { PageHeader } from '@/components/layout/page-header';
import { StatusBadge } from '@/components/StatusBadge';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { roleLabel } from '@/utils/accountAccess';
import { reportError } from '@/feedback';

type Shortcut = {
  to: string;
  title: string;
  description: string;
  action: string;
  icon: typeof UserRound;
  show?: (access: HubAccess) => boolean;
};

const SHORTCUTS: Shortcut[] = [
  {
    to: '/dashboard/profile',
    title: 'Meu perfil',
    description: 'Altere usuário e senha desta sessão.',
    action: 'Abrir perfil',
    icon: UserRound,
  },
  {
    to: '/dashboard/statement',
    title: 'Extrato',
    description: 'O que você tem a receber e o que já entrou.',
    action: 'Abrir extrato',
    icon: FileText,
  },
  {
    to: '/dashboard/carteira',
    title: 'Minha gente',
    description: 'Pessoas da sua rede de agenciamento.',
    action: 'Abrir minha gente',
    icon: Users,
    show: (a) => a.canRecruit || a.admin,
  },
  {
    to: '/dashboard/operations',
    title: 'Operações',
    description: 'Ciclos e lojas que você acompanha.',
    action: 'Abrir operações',
    icon: Layers,
    show: (a) => a.canManageOperations || a.admin,
  },
  {
    to: '/dashboard/charges',
    title: 'Cobranças',
    description: 'Pedidos de pagamento em aberto e pagos.',
    action: 'Abrir cobranças',
    icon: Receipt,
    show: (a) => a.canActAsOperator || a.canSeeFinance || a.canManageOperations || a.admin,
  },
  {
    to: '/dashboard/claims',
    title: 'Direitos',
    description: 'O que cada um tem a receber no livro.',
    action: 'Abrir direitos',
    icon: ScrollText,
    show: (a) => a.canSeeFinance || a.admin,
  },
  {
    to: '/dashboard/world-accounts',
    title: 'Livro-mundo',
    description: 'Caixas globais e exposição.',
    action: 'Abrir livro-mundo',
    icon: Wallet,
    show: (a) => a.canSeeFinance || a.canManageGateways || a.admin,
  },
  {
    to: '/dashboard/accounts',
    title: 'Membros',
    description: 'Identidades e mandatos de produto.',
    action: 'Abrir membros',
    icon: Shield,
    show: (a) => a.canGrant || a.admin,
  },
  {
    to: '/dashboard/deals',
    title: 'Agenciamento',
    description: 'Vínculos e fatias da rede.',
    action: 'Abrir agenciamento',
    icon: Handshake,
    show: (a) => a.canRecruit || a.admin,
  },
  {
    to: '/dashboard/shareholders',
    title: 'Acionistas',
    description: 'Participação e distribuição societária.',
    action: 'Abrir acionistas',
    icon: PieChart,
    show: (a) => a.admin,
  },
];

export function HomePage() {
  const { user } = useAuth();
  const access = useHubAccess();
  const [profile, setProfile] = useState<MyProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      const result = await getMyProfile();
      if (cancelled) return;

      if (!result.ok || !result.data?.profile) {
        const message = result.ok ? 'Perfil indisponível.' : result.error;
        reportError(message);
        setProfile(null);
        setError(message);
        setLoading(false);
        return;
      }

      setProfile(result.data.profile);
      setLoading(false);
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const roles = profile?.roles ?? user?.roles ?? [];
  const username = profile?.username ?? user?.username ?? 'usuário';
  const shortcuts = SHORTCUTS.filter((item) => !item.show || item.show(access));

  return (
    <div className="min-w-0 space-y-6">
      <PageHeader
        kicker="Sessão"
        title={`Olá, ${username}`}
        description={
          access.admin
            ? 'Atalhos do painel: pessoas, cobrança, livro e rede.'
            : 'Atalhos do que o seu mandato já permite ver e fazer.'
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <Card className="border-border/60 bg-card/90">
          <CardHeader className="pb-3">
            <CardDescription className="text-xs font-medium uppercase tracking-wide">
              Sessão
            </CardDescription>
            <CardTitle className="text-base">Resumo da sessão</CardTitle
          </CardHeader>
          <CardContent className="space-y-3">
            {loading ? (
              <>
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-5 w-24" />
                <Skeleton className="h-5 w-40" />
              </>
            ) : error ? (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            ) : (
              <>
                <div className="flex items-center justify-between gap-3">
                  <span className="text-sm text-muted-foreground">Status</span>
                  <StatusBadge status={profile?.status} />
                </div>
                <div className="space-y-2">
                  <span className="text-sm text-muted-foreground">Preset</span>
                  <div className="flex flex-wrap gap-1.5">
                    {roles.length > 0 ? (
                      roles.map((role) => (
                        <Badge key={role} variant="secondary">
                          {roleLabel(role)}
                        </Badge>
                      ))
                    ) : (
                      <span className="text-sm text-muted-foreground">Só identidade</span>
                    )}
                  </div>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        {shortcuts.map((item) => {
          const Icon = item.icon;
          return (
            <Card
              key={item.to}
              className="border-border/60 bg-card/90 transition-colors hover:border-primary/40"
            >
              <CardHeader className="pb-3">
                <div className="mb-1 flex size-9 items-center justify-center rounded-lg bg-primary/15 text-primary">
                  <Icon className="size-4" />
                </div>
                <CardTitle className="text-base">{item.title}</CardTitle>
                <CardDescription>{item.description}</CardDescription>
              </CardHeader>
              <CardContent>
                <Button asChild variant="outline" className="w-full justify-between">
                  <Link to={item.to}>
                    {item.action}
                    <ArrowRight data-icon="inline-end" />
                  </Link>
                </Button>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
