import { useNavigate, useSearchParams } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { KeyRound, Shield, UserRound } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import type { SignUpAccountType } from '@/api/auth';
import { useAuth } from '@/auth/AuthContext';
import { BrandLockup } from '@/components/brand/BrandMark';
import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

const signInSchema = z.object({
  username: z.string().trim().min(1, 'Usuário é obrigatório.'),
  password: z.string().min(1, 'Senha é obrigatória.'),
});

const signUpSchema = z.object({
  accountType: z.enum(['usuario', 'admin']),
  username: z.string().trim().min(1, 'Usuário é obrigatório.'),
  password: z.string().min(8, 'A senha deve ter no mínimo 8 caracteres.'),
  confirmPassword: z.string().min(1, 'Confirme a senha.'),
  masterKey: z.string().optional(),
}).superRefine((values, ctx) => {
  if (values.password !== values.confirmPassword) {
    ctx.addIssue({
      code: 'custom',
      message: 'As senhas não coincidem.',
      path: ['confirmPassword'],
    });
  }
  if (values.accountType === 'admin' && !values.masterKey?.trim()) {
    ctx.addIssue({
      code: 'custom',
      message: 'A chave mestra é obrigatória para administrador.',
      path: ['masterKey'],
    });
  }
});

type SignInValues = z.infer<typeof signInSchema>;
type SignUpValues = z.infer<typeof signUpSchema>;

const fieldClass = 'h-10 rounded-lg bg-background/45 px-3 text-sm';

export function AuthPage() {
  const { signIn, signUp } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const signInForm = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { username: '', password: '' },
  });

  const signUpForm = useForm<SignUpValues>({
    resolver: zodResolver(signUpSchema),
    defaultValues: {
      accountType: 'usuario',
      username: '',
      password: '',
      confirmPassword: '',
      masterKey: '',
    },
  });

  async function handleSignIn(values: SignInValues) {
    const result = await signIn(values.username.trim(), values.password);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    const redirect = searchParams.get('redirect');
    navigate(redirect?.startsWith('/') ? redirect : '/dashboard', { replace: true });
  }

  async function handleSignUp(values: SignUpValues) {
    const result = await signUp({
      accountType: values.accountType as SignUpAccountType,
      username: values.username.trim(),
      password: values.password,
      masterKey: values.accountType === 'admin' ? values.masterKey?.trim() : undefined,
    });
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    const redirect = searchParams.get('redirect');
    navigate(redirect?.startsWith('/') ? redirect : '/dashboard', { replace: true });
  }

  const accountType = signUpForm.watch('accountType');

  return (
    <div
      id="main-content"
      className="relative flex min-h-dvh items-center justify-center px-4 py-10 md:px-8"
    >
      <div className="auth-stage grid w-full max-w-5xl overflow-hidden rounded-2xl border border-border/50 bg-card/55 shadow-[0_24px_80px_rgba(0,0,0,0.35)] backdrop-blur-xl min-[720px]:grid-cols-[1.05fr_0.95fr]">
        <section className="relative flex flex-col justify-start gap-8 border-b border-border/40 px-8 py-9 min-[720px]:border-b-0 min-[720px]:border-r min-[720px]:justify-between min-[720px]:gap-10 min-[720px]:px-10 min-[720px]:py-12">
          <div
            className="pointer-events-none absolute inset-0 opacity-90"
            aria-hidden="true"
            style={{
              background:
                'radial-gradient(ellipse 90% 70% at 10% 20%, rgba(74,134,255,0.2), transparent 55%), radial-gradient(ellipse 60% 50% at 85% 90%, rgba(120,80,255,0.14), transparent 50%)',
            }}
          />
          <BrandLockup
            size="hero"
            className="relative"
            subtitle="Painel operacional da Websete. Entre para continuar — sem ruído, só o caminho principal."
          />
          <ul className="relative hidden space-y-3 text-sm text-muted-foreground min-[720px]:block">
            <li className="flex items-start gap-3">
              <span className="mt-1.5 size-1.5 shrink-0 rounded-full bg-primary" />
              Sessão com token de acesso.
            </li>
            <li className="flex items-start gap-3">
              <span className="mt-1.5 size-1.5 shrink-0 rounded-full bg-primary/70" />
              Usuário livre; administrador exige chave mestra.
            </li>
          </ul>
        </section>

        <section className="relative flex flex-col justify-center px-6 py-8 sm:px-8 min-[720px]:px-10 min-[720px]:py-12">
          <Tabs defaultValue="sign-in" className="w-full gap-5">
            <TabsList className="grid h-11 w-full grid-cols-2 rounded-xl bg-muted/70 p-1">
              <TabsTrigger
                value="sign-in"
                className="rounded-lg data-active:bg-background data-active:text-foreground data-active:shadow-sm"
              >
                Entrar
              </TabsTrigger>
              <TabsTrigger
                value="sign-up"
                className="rounded-lg data-active:bg-background data-active:text-foreground data-active:shadow-sm"
              >
                Criar conta
              </TabsTrigger>
            </TabsList>

            <TabsContent value="sign-in" className="mt-0 outline-none">
              <Form {...signInForm}>
                <form className="space-y-5" onSubmit={signInForm.handleSubmit(handleSignIn)}>
                  <FormField
                    control={signInForm.control}
                    name="username"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Usuário</FormLabel>
                        <FormControl>
                          <Input
                            className={fieldClass}
                            autoComplete="username"
                            placeholder="seu.usuario"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={signInForm.control}
                    name="password"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Senha</FormLabel>
                        <FormControl>
                          <Input
                            className={fieldClass}
                            type="password"
                            autoComplete="current-password"
                            placeholder="••••••••"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <Button
                    type="submit"
                    size="lg"
                    className="mt-1 h-11 w-full text-sm font-semibold"
                    disabled={signInForm.formState.isSubmitting}
                  >
                    <KeyRound data-icon="inline-start" />
                    {signInForm.formState.isSubmitting ? 'Aguarde…' : 'Entrar'}
                  </Button>
                </form>
              </Form>
            </TabsContent>

            <TabsContent value="sign-up" className="mt-0 outline-none">
              <Form {...signUpForm}>
                <form className="space-y-5" onSubmit={signUpForm.handleSubmit(handleSignUp)}>
                  <FormField
                    control={signUpForm.control}
                    name="accountType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Tipo de conta</FormLabel>
                        <FormControl>
                          <ToggleGroup
                            type="single"
                            variant="outline"
                            value={field.value}
                            onValueChange={(value) => {
                              if (value) field.onChange(value as SignUpAccountType);
                            }}
                            className="grid w-full grid-cols-2 gap-2"
                          >
                            <ToggleGroupItem
                              value="usuario"
                              className={cn(
                                'h-auto w-full flex-col gap-1 rounded-lg px-3 py-3 data-[state=on]:border-primary/50 data-[state=on]:bg-primary/15 data-[state=on]:text-foreground',
                              )}
                            >
                              <UserRound className="size-4 text-primary" />
                              <span className="text-xs font-medium">Usuário</span>
                            </ToggleGroupItem>
                            <ToggleGroupItem
                              value="admin"
                              className={cn(
                                'h-auto w-full flex-col gap-1 rounded-lg px-3 py-3 data-[state=on]:border-primary/50 data-[state=on]:bg-primary/15 data-[state=on]:text-foreground',
                              )}
                            >
                              <Shield className="size-4 text-primary" />
                              <span className="text-xs font-medium">Admin</span>
                            </ToggleGroupItem>
                          </ToggleGroup>
                        </FormControl>
                        <FormDescription>
                          {accountType === 'admin'
                            ? 'Administrador requer a chave mestra do servidor.'
                            : 'Conta padrão, sem papel administrativo.'}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  {accountType === 'admin' ? (
                    <FormField
                      control={signUpForm.control}
                      name="masterKey"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel className="inline-flex items-center gap-1.5">
                            <KeyRound className="size-3.5 text-muted-foreground" />
                            Chave mestra
                          </FormLabel>
                          <FormControl>
                            <Input
                              className={fieldClass}
                              type="password"
                              autoComplete="off"
                              placeholder="Token de criação"
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  ) : null}

                  <FormField
                    control={signUpForm.control}
                    name="username"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Usuário</FormLabel>
                        <FormControl>
                          <Input
                            className={fieldClass}
                            autoComplete="username"
                            placeholder="seu.usuario"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={signUpForm.control}
                    name="password"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Senha</FormLabel>
                        <FormControl>
                          <Input
                            className={fieldClass}
                            type="password"
                            autoComplete="new-password"
                            placeholder="Mínimo 8 caracteres"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={signUpForm.control}
                    name="confirmPassword"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Confirmar senha</FormLabel>
                        <FormControl>
                          <Input
                            className={fieldClass}
                            type="password"
                            autoComplete="new-password"
                            placeholder="Repita a senha"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <Button
                    type="submit"
                    size="lg"
                    className="mt-1 h-11 w-full text-sm font-semibold"
                    disabled={signUpForm.formState.isSubmitting}
                  >
                    {signUpForm.formState.isSubmitting ? 'Aguarde…' : 'Criar conta'}
                  </Button>
                </form>
              </Form>
            </TabsContent>
          </Tabs>
        </section>
      </div>
    </div>
  );
}
