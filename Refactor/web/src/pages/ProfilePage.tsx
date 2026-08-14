import { useEffect, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { KeyRound, UserRound } from 'lucide-react';
import { changeMyPassword, changeMyUsername, getMyProfile } from '@/api/auth';
import { useAuth } from '@/auth/AuthContext';
import type { MyProfile } from '@/auth/types';
import { PageHeader } from '@/components/layout/page-header';
import { StatusBadge } from '@/components/StatusBadge';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
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
import { Skeleton } from '@/components/ui/skeleton';
import { roleLabel } from '@/utils/accountAccess';
import { toast } from 'sonner';

const usernameSchema = z.object({
  newUsername: z.string().trim().min(3, 'Mínimo 3 caracteres.').max(64, 'Máximo 64 caracteres.'),
});

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Informe a senha atual.'),
  newPassword: z.string().min(8, 'A nova senha deve ter no mínimo 8 caracteres.'),
  confirmPassword: z.string().min(1, 'Confirme a nova senha.'),
}).superRefine((values, ctx) => {
  if (values.newPassword !== values.confirmPassword) {
    ctx.addIssue({
      code: 'custom',
      message: 'As senhas não coincidem.',
      path: ['confirmPassword'],
    });
  }
});

type UsernameValues = z.infer<typeof usernameSchema>;
type PasswordValues = z.infer<typeof passwordSchema>;

function userInitial(username: string | undefined): string {
  const letter = username?.trim().charAt(0);
  return letter ? letter.toUpperCase() : '?';
}

export function ProfilePage() {
  const { user, applyTokens, patchUser } = useAuth();
  const [profile, setProfile] = useState<MyProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const usernameForm = useForm<UsernameValues>({
    resolver: zodResolver(usernameSchema),
    defaultValues: { newUsername: '' },
  });

  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      const result = await getMyProfile();
      if (cancelled) return;

      if (!result.ok || !result.data?.profile) {
        toast.error(result.ok ? 'Perfil indisponível.' : result.error);
        setLoading(false);
        return;
      }

      setProfile(result.data.profile);
      usernameForm.reset({ newUsername: result.data.profile.username });
      setLoading(false);
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [usernameForm]);

  async function handleUsername(values: UsernameValues) {
    const result = await changeMyUsername(values.newUsername.trim());
    if (!result.ok) {
      toast.error(result.error);
      return;
    }

    toast.success('Usuário atualizado. O nome anterior não pode ser reutilizado.');
    const nextUsername = result.data?.username ?? values.newUsername.trim();
    setProfile((current) => (current ? { ...current, username: nextUsername } : current));
    patchUser({ username: nextUsername });
  }

  async function handlePassword(values: PasswordValues) {
    const result = await changeMyPassword(values.currentPassword, values.newPassword);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }

    const tokens = result.data?.tokens;
    if (!tokens?.accessToken) {
      toast.error('Senha alterada, mas o servidor não retornou novos tokens.');
      return;
    }

    const applied = applyTokens(tokens);
    if (!applied.ok) {
      toast.error(applied.error);
      return;
    }

    passwordForm.reset();
    toast.success('Senha alterada.');
  }

  const roles = profile?.roles ?? user?.roles ?? [];
  const username = profile?.username ?? user?.username;

  return (
    <div className="min-w-0 space-y-6">
      <PageHeader
        kicker="Conta"
        title="Meu perfil"
        description="Atualize o usuário e a senha da sessão atual. Um nome já usado não volta a ficar disponível."
      />

      <Card className="border-border/60 bg-card/90">
        <CardContent className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between">
          {loading ? (
            <div className="flex w-full items-center gap-3">
              <Skeleton className="size-12 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-5 w-40" />
                <Skeleton className="h-4 w-56" />
              </div>
            </div>
          ) : (
            <>
              <div className="flex items-center gap-3">
                <Avatar className="size-12">
                  <AvatarFallback className="text-base">{userInitial(username)}</AvatarFallback>
                </Avatar>
                <div className="min-w-0 space-y-1.5">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="truncate text-lg font-semibold">{username}</p>
                    <StatusBadge status={profile?.status} />
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {roles.length > 0 ? (
                      roles.map((role) => (
                        <Badge key={role} variant="secondary">{roleLabel(role)}</Badge>
                      ))
                    ) : (
                      <span className="text-sm text-muted-foreground">Sem mandato</span>
                    )}
                  </div>
                </div>
              </div>
              <p className="max-w-xs text-xs leading-relaxed text-muted-foreground sm:text-right">
                Alterações de usuário e senha valem nesta sessão. O nome antigo não pode ser reutilizado.
              </p>
            </>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <div className="mb-1 flex size-8 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <UserRound className="size-4" />
            </div>
            <CardTitle className="text-base">Identidade</CardTitle>
            <CardDescription>O usuário é o login único. Trocar o nome reserva o anterior para sempre.</CardDescription>
          </CardHeader>
          <CardContent>
            <Form {...usernameForm}>
              <form className="space-y-4" onSubmit={usernameForm.handleSubmit(handleUsername)}>
                <FormField
                  control={usernameForm.control}
                  name="newUsername"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Usuário</FormLabel>
                      <FormControl>
                        <Input autoComplete="username" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <Button type="submit" disabled={usernameForm.formState.isSubmitting}>
                  {usernameForm.formState.isSubmitting ? 'Salvando…' : 'Salvar usuário'}
                </Button>
              </form>
            </Form>
          </CardContent>
        </Card>

        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <div className="mb-1 flex size-8 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <KeyRound className="size-4" />
            </div>
            <CardTitle className="text-base">Segurança</CardTitle>
            <CardDescription>Troque a senha e renove os tokens da sessão.</CardDescription>
          </CardHeader>
          <CardContent>
            <Form {...passwordForm}>
              <form className="space-y-4" onSubmit={passwordForm.handleSubmit(handlePassword)}>
                <FormField
                  control={passwordForm.control}
                  name="currentPassword"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Senha atual</FormLabel>
                      <FormControl>
                        <Input type="password" autoComplete="current-password" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <Separator />
                <FormField
                  control={passwordForm.control}
                  name="newPassword"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Nova senha</FormLabel>
                      <FormControl>
                        <Input type="password" autoComplete="new-password" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={passwordForm.control}
                  name="confirmPassword"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Confirmar nova senha</FormLabel>
                      <FormControl>
                        <Input type="password" autoComplete="new-password" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <Button type="submit" disabled={passwordForm.formState.isSubmitting}>
                  {passwordForm.formState.isSubmitting ? 'Salvando…' : 'Salvar senha'}
                </Button>
              </form>
            </Form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
