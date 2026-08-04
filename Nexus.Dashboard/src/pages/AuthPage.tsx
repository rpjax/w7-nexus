import { useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { useSearchParams } from 'react-router-dom';
import type { SignUpAccountType } from '@/api/auth';
import { useAuth } from '@/auth/AuthContext';
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { toast } from 'sonner';

const signInSchema = z.object({
  username: z.string().trim().min(1, 'Usuário é obrigatório.'),
  password: z.string().min(1, 'Senha é obrigatória.'),
});

const signUpSchema = z.object({
  accountType: z.enum(['operator', 'administrator']),
  username: z.string().trim().min(1, 'Usuário é obrigatório.'),
  password: z.string().min(1, 'Senha é obrigatória.'),
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
  if (values.accountType === 'administrator' && !values.masterKey?.trim()) {
    ctx.addIssue({
      code: 'custom',
      message: 'A chave mestra é obrigatória para administrador.',
      path: ['masterKey'],
    });
  }
});

type SignInValues = z.infer<typeof signInSchema>;
type SignUpValues = z.infer<typeof signUpSchema>;

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
      accountType: 'operator',
      username: '',
      password: '',
      confirmPassword: '',
      masterKey: '',
    },
  });

  async function handleSignIn(values: SignInValues) {
    const result = await signIn(values.username.trim(), values.password.trim());
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
      password: values.password.trim(),
      masterKey: values.accountType === 'administrator' ? values.masterKey?.trim() : undefined,
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
    <div className="flex min-h-dvh items-center justify-center p-4 md:p-8">
      <div className="grid w-full max-w-4xl gap-8 lg:grid-cols-[1fr_420px] lg:items-center">
        <section className="hidden space-y-4 lg:block">
          <p className="text-xs font-semibold uppercase tracking-wider text-primary">Websete Nexus</p>
          <h1 className="text-3xl font-bold tracking-tight">Painel operacional</h1>
          <p className="max-w-md text-sm leading-relaxed text-muted-foreground">
            Entre com sua conta ou cadastre um operador. Administradores exigem a chave mestra configurada no servidor.
          </p>
        </section>

        <Card>
          <CardHeader>
            <CardTitle>Acesso</CardTitle>
            <CardDescription>Autenticação centralizada do dashboard.</CardDescription>
          </CardHeader>
          <CardContent>
            <Tabs defaultValue="sign-in">
              <TabsList className="grid w-full grid-cols-2">
                <TabsTrigger value="sign-in">Entrar</TabsTrigger>
                <TabsTrigger value="sign-up">Criar conta</TabsTrigger>
              </TabsList>

              <TabsContent value="sign-in" className="mt-4">
                <Form {...signInForm}>
                  <form className="space-y-4" onSubmit={signInForm.handleSubmit(handleSignIn)}>
                    <FormField
                      control={signInForm.control}
                      name="username"
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
                    <FormField
                      control={signInForm.control}
                      name="password"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Senha</FormLabel>
                          <FormControl>
                            <Input type="password" autoComplete="current-password" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <Button type="submit" className="w-full" disabled={signInForm.formState.isSubmitting}>
                      {signInForm.formState.isSubmitting ? 'Aguarde…' : 'Entrar'}
                    </Button>
                  </form>
                </Form>
              </TabsContent>

              <TabsContent value="sign-up" className="mt-4 space-y-4">
                <Form {...signUpForm}>
                  <form className="space-y-4" onSubmit={signUpForm.handleSubmit(handleSignUp)}>
                    <FormField
                      control={signUpForm.control}
                      name="accountType"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Tipo de conta</FormLabel>
                          <FormControl>
                            <ToggleGroup
                              type="single"
                              value={field.value}
                              onValueChange={(value) => {
                                if (value) field.onChange(value as SignUpAccountType);
                              }}
                              className="grid w-full grid-cols-2"
                            >
                              <ToggleGroupItem value="operator" className="w-full">Operador</ToggleGroupItem>
                              <ToggleGroupItem value="administrator" className="w-full">Administrador</ToggleGroupItem>
                            </ToggleGroup>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    {accountType === 'administrator' ? (
                      <FormField
                        control={signUpForm.control}
                        name="masterKey"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Chave mestra</FormLabel>
                            <FormControl>
                              <Input type="password" autoComplete="off" {...field} />
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
                            <Input autoComplete="username" {...field} />
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
                            <Input type="password" autoComplete="new-password" {...field} />
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
                            <Input type="password" autoComplete="new-password" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <Button type="submit" className="w-full" disabled={signUpForm.formState.isSubmitting}>
                      {signUpForm.formState.isSubmitting ? 'Aguarde…' : 'Criar conta'}
                    </Button>
                  </form>
                </Form>
              </TabsContent>
            </Tabs>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
