import { useNavigate, useSearchParams } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { KeyRound, Shield } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
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
import { toast } from 'sonner';

const signInSchema = z.object({
  username: z.string().trim().min(1, 'Usuário é obrigatório.'),
  password: z.string().min(1, 'Senha é obrigatória.'),
});

const signUpSchema = z.object({
  username: z.string().trim().min(1, 'Usuário é obrigatório.'),
  password: z.string().min(8, 'A senha deve ter no mínimo 8 caracteres.'),
  confirmPassword: z.string().min(1, 'Confirme a senha.'),
  masterKey: z.string().trim().min(1, 'A chave mestra é obrigatória.'),
}).superRefine((values, ctx) => {
  if (values.password !== values.confirmPassword) {
    ctx.addIssue({
      code: 'custom',
      message: 'As senhas não coincidem.',
      path: ['confirmPassword'],
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
      username: values.username.trim(),
      password: values.password,
      masterKey: values.masterKey.trim(),
    });
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    const redirect = searchParams.get('redirect');
    navigate(redirect?.startsWith('/') ? redirect : '/dashboard', { replace: true });
  }

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
              Cadastro público fechado; contas comuns só via Admin.
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
                Bootstrap admin
              </TabsTrigger>
            </TabsList>

            <TabsContent value="sign-in" className="mt-0 outline-none">
              <Form {...signInForm}>
                <form className="space-y-5" onSubmit={signInForm.handleSubmit(handleSignIn)}>
                  <FormDescription>
                    Em deploy local o Admin já nasce no seed: usuário <span className="font-medium text-foreground">admin</span>, senha <span className="font-medium text-foreground">adminadmin</span>.
                  </FormDescription>
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
                            placeholder="admin"
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
                  <FormDescription>
                    Só use se o seed falhou. Chave mestra local: <span className="font-medium text-foreground">local-dev-master-key</span>. Se o Admin já existe, use a aba Entrar.
                  </FormDescription>
                  <FormField
                    control={signUpForm.control}
                    name="masterKey"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="inline-flex items-center gap-1.5">
                          <Shield className="size-3.5 text-muted-foreground" />
                          Chave mestra
                        </FormLabel>
                        <FormControl>
                          <Input
                            className={fieldClass}
                            type="password"
                            autoComplete="off"
                            placeholder="local-dev-master-key"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
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
                            placeholder="admin"
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
                    {signUpForm.formState.isSubmitting ? 'Aguarde…' : 'Criar administrador'}
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
