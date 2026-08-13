import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, Shield, UserRound } from 'lucide-react';
import { getMyProfile } from '@/api/auth';
import { useAuth } from '@/auth/AuthContext';
import type { MyProfile } from '@/auth/types';
import { PageHeader } from '@/components/layout/page-header';
import { StatusBadge } from '@/components/StatusBadge';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { isAdministrator, roleLabel } from '@/utils/accountAccess';
import { toast } from 'sonner';

export function HomePage() {
  const { user } = useAuth();
  const [profile, setProfile] = useState<MyProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      const result = await getMyProfile();
      if (cancelled) return;

      if (!result.ok || !result.data?.profile) {
        toast.error(result.ok ? 'Perfil indisponível.' : result.error);
        setProfile(null);
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
  const admin = isAdministrator(roles);
  const username = profile?.username ?? user?.username ?? 'usuário';

  return (
    <div className="min-w-0 space-y-6">
      <PageHeader
        kicker={admin ? 'Administração' : 'Conta'}
        kickerVariant={admin ? 'admin' : 'default'}
        title={`Olá, ${username}`}
        description={
          admin
            ? 'Gerencie identidades e o preset Admin. Mandatos de produto entram na próxima etapa.'
            : 'Acompanhe sua identidade e atualize handle e senha quando precisar.'
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <Card className="border-border/60 bg-card/90">
          <CardHeader className="pb-3">
            <CardDescription className="text-xs font-medium uppercase tracking-wide">
              Sessão
            </CardDescription>
            <CardTitle className="text-base">Resumo da conta</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {loading ? (
              <>
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-5 w-24" />
                <Skeleton className="h-5 w-40" />
              </>
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

        <Card className="border-border/60 bg-card/90 transition-colors hover:border-primary/40">
          <CardHeader className="pb-3">
            <div className="mb-1 flex size-9 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <UserRound className="size-4" />
            </div>
            <CardTitle className="text-base">Meu perfil</CardTitle>
            <CardDescription>
              Altere handle e senha da conta autenticada.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button asChild variant="outline" className="w-full justify-between">
              <Link to="/dashboard/profile">
                Abrir perfil
                <ArrowRight data-icon="inline-end" />
              </Link>
            </Button>
          </CardContent>
        </Card>

        {admin ? (
          <Card className="border-border/60 bg-card/90 transition-colors hover:border-warning/40 sm:col-span-2 xl:col-span-1">
            <CardHeader className="pb-3">
              <div className="mb-1 flex size-9 items-center justify-center rounded-lg bg-warning/15 text-warning">
                <Shield className="size-4" />
              </div>
              <CardTitle className="text-base">Contas</CardTitle>
              <CardDescription>
                Busque identidades, conceda Admin e controle status.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button asChild className="w-full justify-between">
                <Link to="/dashboard/accounts">
                  Abrir contas
                  <ArrowRight data-icon="inline-end" />
                </Link>
              </Button>
            </CardContent>
          </Card>
        ) : null}
      </div>

      <Card className="border-border/50 bg-transparent">
        <CardContent className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium">Próxima etapa</p>
            <p className="text-xs text-muted-foreground">
              Mandato (capacidade × escopo): Operador, Recrutador, Laranja, Gateways, Contador, Gestor de Operações.
            </p>
          </div>
          <Badge variant="outline">01 · Identidade</Badge>
        </CardContent>
      </Card>
    </div>
  );
}
