import { useCallback, useEffect, useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { type ColumnDef } from '@tanstack/react-table';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { ArrowLeft, Plus, RefreshCw, Search, Users } from 'lucide-react';
import {
  createAdministratorAccount,
  disableAdministratorAccount,
  enableAdministratorAccount,
  getAdministratorAccount,
  grantAdministratorAccountPermission,
  grantAdministratorAccountRole,
  resetAdministratorAccountPassword,
  revokeAdministratorAccountPermission,
  revokeAdministratorAccountRole,
  searchAdministratorAccounts,
  type CreateAccountType,
} from '@/api/administrator/accounts';
import { useAuth } from '@/auth/AuthContext';
import type { AccountDetails } from '@/auth/types';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { PageHeader } from '@/components/layout/page-header';
import { StatusBadge } from '@/components/StatusBadge';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import {
  ACCOUNT_PERMISSION_CATALOG,
  ACCOUNT_ROLE_CATALOG,
  hasPermissionIgnoreCase,
  hasRoleIgnoreCase,
  roleLabel,
} from '@/utils/accountAccess';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;

function formatDateTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

const createSchema = z.object({
  accountType: z.enum(['usuario', 'admin']),
  username: z.string().trim().min(3, 'Mínimo 3 caracteres.'),
  password: z.string().min(8, 'Mínimo 8 caracteres.'),
  masterKey: z.string().optional(),
}).superRefine((values, ctx) => {
  if (values.accountType === 'admin' && !values.masterKey?.trim()) {
    ctx.addIssue({
      code: 'custom',
      message: 'Chave mestra obrigatória para admin.',
      path: ['masterKey'],
    });
  }
});

type CreateValues = z.infer<typeof createSchema>;

export function AccountsPage() {
  const { user } = useAuth();
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [roleFilter, setRoleFilter] = useState('all');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(20);
  const [items, setItems] = useState<AccountDetails[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<AccountDetails | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [resetPassword, setResetPassword] = useState('');
  const [createOpen, setCreateOpen] = useState(false);

  const createForm = useForm<CreateValues>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      accountType: 'usuario',
      username: '',
      password: '',
      masterKey: '',
    },
  });

  const load = useCallback(async (overrides?: {
    keyword?: string;
    status?: string;
    role?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const nextKeyword = overrides?.keyword ?? keyword;
    const nextStatus = overrides?.status ?? statusFilter;
    const nextRole = overrides?.role ?? roleFilter;
    const nextPage = overrides?.page ?? page;
    const nextPageSize = overrides?.pageSize ?? pageSize;

    setLoading(true);
    const result = await searchAdministratorAccounts({
      limit: nextPageSize,
      offset: (nextPage - 1) * nextPageSize,
      keyword: nextKeyword.trim() || undefined,
      status: nextStatus === 'all' ? undefined : nextStatus,
      role: nextRole === 'all' ? undefined : nextRole,
    });
    setLoading(false);

    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }

    setItems(result.data.items ?? []);
    setTotal(result.data.total);
  }, [keyword, page, pageSize, roleFilter, statusFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  const columns = useMemo<ColumnDef<AccountDetails>[]>(() => [
    {
      accessorKey: 'username',
      header: 'Usuário',
      cell: ({ row }) => (
        <div className="min-w-[8rem] max-w-[12rem]">
          <p className="truncate font-medium leading-tight">{row.original.username}</p>
          <p className="truncate font-mono text-[0.65rem] leading-tight text-muted-foreground">
            <span className="lg:hidden">{row.original.id.slice(0, 8)}…</span>
            <span className="hidden lg:inline">{row.original.id}</span>
          </p>
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'roles',
      header: 'Papéis',
      cell: ({ row }) => (
        <div className="flex max-w-[11rem] flex-wrap gap-0.5">
          {row.original.roles.length > 0 ? (
            row.original.roles.map((role) => (
              <Badge key={role} variant="secondary" className="h-5 px-1.5 text-[0.65rem]">
                {roleLabel(role)}
              </Badge>
            ))
          ) : (
            <span className="text-xs text-muted-foreground">—</span>
          )}
        </div>
      ),
    },
    {
      id: 'permissions',
      header: 'Perm.',
      cell: ({ row }) => (
        <span className="tabular-nums text-xs text-muted-foreground">
          {row.original.permissions.length}
        </span>
      ),
    },
    {
      accessorKey: 'lastUpdatedAt',
      header: 'Atualizado',
      cell: ({ row }) => (
        <span className="whitespace-nowrap text-xs tabular-nums text-muted-foreground">
          {formatDateTime(row.original.lastUpdatedAt)}
        </span>
      ),
    },
  ], []);

  function selectAccount(account: AccountDetails) {
    setSelected(account);
    setResetPassword('');
    void refreshSelected(account.id);
  }

  function applySearch() {
    if (page === 1) {
      void load({ page: 1 });
      return;
    }
    setPage(1);
  }

  async function refreshSelected(accountId: string) {
    const result = await getAdministratorAccount(accountId);
    if (!result.ok || !result.data?.account) {
      toast.error(result.ok ? 'Conta indisponível.' : result.error);
      return;
    }
    setSelected(result.data.account);
    setItems((current) => current.map((item) => (
      item.id === accountId ? result.data!.account : item
    )));
  }

  async function handleCreate(values: CreateValues) {
    const result = await createAdministratorAccount({
      username: values.username.trim(),
      password: values.password,
      accountType: values.accountType as CreateAccountType,
      masterKey: values.masterKey?.trim(),
    });
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    createForm.reset({ accountType: 'usuario', username: '', password: '', masterKey: '' });
    setCreateOpen(false);
    toast.success('Conta criada.');
    await load();
    if (result.data?.account) {
      setSelected(result.data.account);
    }
  }

  async function toggleRole(account: AccountDetails, role: string, enabled: boolean) {
    const key = `role:${role}`;
    setBusyKey(key);
    const result = enabled
      ? await grantAdministratorAccountRole(account.id, role)
      : await revokeAdministratorAccountRole(account.id, role);
    setBusyKey(null);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    await refreshSelected(account.id);
  }

  async function togglePermission(account: AccountDetails, permission: string, enabled: boolean) {
    const key = `perm:${permission}`;
    setBusyKey(key);
    const result = enabled
      ? await grantAdministratorAccountPermission(account.id, permission)
      : await revokeAdministratorAccountPermission(account.id, permission);
    setBusyKey(null);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    await refreshSelected(account.id);
  }

  async function handleDisableEnable(account: AccountDetails) {
    const isDisabled = account.status === 'Disabled';
    setBusyKey('status');
    const result = isDisabled
      ? await enableAdministratorAccount(account.id)
      : await disableAdministratorAccount(account.id);
    setBusyKey(null);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success(isDisabled ? 'Conta reabilitada.' : 'Conta desabilitada.');
    if (result.data?.account) {
      setSelected(result.data.account);
      setItems((current) => current.map((item) => (
        item.id === account.id ? result.data!.account : item
      )));
    }
  }

  async function handleResetPassword(account: AccountDetails) {
    if (resetPassword.trim().length < 8) {
      toast.error('A nova senha deve ter no mínimo 8 caracteres.');
      return;
    }
    setBusyKey('reset');
    const result = await resetAdministratorAccountPassword(account.id, resetPassword);
    setBusyKey(null);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    setResetPassword('');
    toast.success('Senha redefinida.');
  }

  const accountType = createForm.watch('accountType');
  const isSelf = selected?.id === user?.accountId;

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Contas"
        description="Busque, inspecione e gerencie acesso, status e senhas."
        actions={(
          <Button type="button" onClick={() => setCreateOpen(true)}>
            <Plus data-icon="inline-start" />
            Nova conta
          </Button>
        )}
      />

      <div className="grid min-w-0 gap-4 lg:grid-cols-[minmax(0,1.55fr)_minmax(18rem,0.85fr)] lg:items-start">
        <Card
          size="sm"
          className={cn(
            'min-h-0 min-w-0 border-border/60 bg-card/90 lg:h-[calc(100dvh-10.5rem)]',
            selected && 'hidden lg:flex',
          )}
        >
          <CardHeader className="gap-2.5 pb-2">
            <div className="flex items-center justify-between gap-2">
              <div className="min-w-0">
                <CardTitle className="text-sm">Registros</CardTitle>
                <CardDescription className="text-xs">
                  Clique numa linha para inspecionar.
                </CardDescription>
              </div>
              <Badge variant="secondary" className="tabular-nums">{total}</Badge>
            </div>

            <form
              className="flex flex-col gap-2"
              onSubmit={(event) => {
                event.preventDefault();
                applySearch();
              }}
            >
              <div className="relative min-w-0">
                <Search className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={keyword}
                  onChange={(event) => setKeyword(event.target.value)}
                  placeholder="Buscar usuário ou ID"
                  className="h-8 pl-8"
                />
              </div>

              <div className="flex flex-wrap items-center gap-1.5">
                <Select
                  value={statusFilter}
                  onValueChange={(value) => {
                    setStatusFilter(value);
                    setPage(1);
                  }}
                >
                  <SelectTrigger size="sm" className="min-w-[7.5rem] flex-1 sm:flex-none">
                    <SelectValue placeholder="Status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos status</SelectItem>
                    <SelectItem value="Active">Ativa</SelectItem>
                    <SelectItem value="Disabled">Desabilitada</SelectItem>
                  </SelectContent>
                </Select>

                <Select
                  value={roleFilter}
                  onValueChange={(value) => {
                    setRoleFilter(value);
                    setPage(1);
                  }}
                >
                  <SelectTrigger size="sm" className="min-w-[8rem] flex-1 sm:flex-none">
                    <SelectValue placeholder="Papel" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos papéis</SelectItem>
                    {ACCOUNT_ROLE_CATALOG.map((role) => (
                      <SelectItem key={role.id} value={role.id}>{role.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <Select
                  value={String(pageSize)}
                  onValueChange={(value) => {
                    setPageSize(Number(value));
                    setPage(1);
                  }}
                >
                  <SelectTrigger size="sm" className="w-[4.75rem]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {PAGE_SIZE_OPTIONS.map((size) => (
                      <SelectItem key={size} value={String(size)}>{size}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <Button type="submit" size="sm" variant="outline" disabled={loading}>
                  Buscar
                </Button>
                <Button
                  type="button"
                  size="icon"
                  variant="outline"
                  className="size-7"
                  disabled={loading}
                  aria-label="Atualizar lista"
                  onClick={() => void load()}
                >
                  <RefreshCw className={cn(loading && 'animate-spin')} />
                </Button>
              </div>
            </form>
          </CardHeader>

          <Separator />

          <CardContent className="min-h-0 flex-1 overflow-hidden p-0">
            <DataTable
              columns={columns}
              data={items}
              density="compact"
              loading={loading}
              selectedRowId={selected?.id}
              getRowId={(row) => row.id}
              onRowClick={selectAccount}
              emptyMessage="Nenhuma conta encontrada com esses filtros."
              className="h-full"
            />
          </CardContent>

          <Separator />
          <CardFooter className="py-2.5">
            <ListPagination
              className="w-full"
              page={page}
              pageSize={pageSize}
              total={total}
              disabled={loading}
              onPageChange={setPage}
            />
          </CardFooter>
        </Card>

        {selected ? (
          <Card className="min-w-0 border-border/60 bg-card/90 lg:h-[calc(100dvh-10.5rem)] lg:overflow-y-auto">
            <CardHeader className="gap-4">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="-ml-2 w-fit lg:hidden"
                onClick={() => setSelected(null)}
              >
                <ArrowLeft data-icon="inline-start" />
                Voltar à lista
              </Button>
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0 space-y-1.5">
                  <div className="flex flex-wrap items-center gap-2">
                    <CardTitle className="break-words text-xl">{selected.username}</CardTitle>
                    <StatusBadge status={selected.status} />
                  </div>
                  <CardDescription className="break-all font-mono text-xs">
                    {selected.id}
                  </CardDescription>
                </div>
                <Button
                  type="button"
                  size="sm"
                  className="w-full shrink-0 sm:w-auto"
                  variant={selected.status === 'Disabled' ? 'default' : 'destructive'}
                  disabled={busyKey === 'status' || isSelf}
                  onClick={() => void handleDisableEnable(selected)}
                >
                  {selected.status === 'Disabled' ? 'Reabilitar' : 'Desabilitar'}
                </Button>
              </div>
              {isSelf ? (
                <p className="rounded-lg border border-border/60 bg-muted/30 px-3 py-2 text-xs text-muted-foreground">
                  Você não pode desabilitar a própria sessão.
                </p>
              ) : null}
            </CardHeader>
            <Separator />
            <CardContent className="min-w-0 p-4 md:p-5">
              <Tabs defaultValue="access">
                <TabsList className="mb-4 grid w-full grid-cols-2">
                  <TabsTrigger value="access">Acesso</TabsTrigger>
                  <TabsTrigger value="security">Segurança</TabsTrigger>
                </TabsList>

                <TabsContent value="access" className="space-y-5">
                  <section className="space-y-2">
                    <h3 className="text-sm font-medium">Papéis</h3>
                    <div className="space-y-2">
                      {ACCOUNT_ROLE_CATALOG.map((role) => {
                        const enabled = hasRoleIgnoreCase(selected.roles, role.id);
                        const key = `role:${role.id}`;
                        return (
                          <label
                            key={role.id}
                            className="flex cursor-pointer items-start gap-3 rounded-lg border border-border/60 px-3 py-2.5 hover:bg-muted/30"
                          >
                            <Checkbox
                              checked={enabled}
                              disabled={busyKey !== null}
                              onCheckedChange={(checked) => {
                                void toggleRole(selected, role.id, checked === true);
                              }}
                              className="mt-0.5"
                            />
                            <span className="min-w-0">
                              <span className="block text-sm font-medium">{role.label}</span>
                              <span className="block text-xs text-muted-foreground">{role.description}</span>
                              {busyKey === key ? (
                                <span className="mt-1 block text-xs text-primary">Atualizando…</span>
                              ) : null}
                            </span>
                          </label>
                        );
                      })}
                    </div>
                  </section>

                  <section className="space-y-2">
                    <h3 className="text-sm font-medium">Permissões</h3>
                    <div className="space-y-2">
                      {ACCOUNT_PERMISSION_CATALOG.map((permission) => {
                        const enabled = hasPermissionIgnoreCase(selected.permissions, permission.id);
                        const key = `perm:${permission.id}`;
                        return (
                          <label
                            key={permission.id}
                            className="flex cursor-pointer items-start gap-3 rounded-lg border border-border/60 px-3 py-2.5 hover:bg-muted/30"
                          >
                            <Checkbox
                              checked={enabled}
                              disabled={busyKey !== null}
                              onCheckedChange={(checked) => {
                                void togglePermission(selected, permission.id, checked === true);
                              }}
                              className="mt-0.5"
                            />
                            <span className="min-w-0">
                              <span className="block text-sm font-medium">{permission.label}</span>
                              <span className="block text-xs text-muted-foreground">{permission.description}</span>
                              {busyKey === key ? (
                                <span className="mt-1 block text-xs text-primary">Atualizando…</span>
                              ) : null}
                            </span>
                          </label>
                        );
                      })}
                    </div>
                  </section>
                </TabsContent>

                <TabsContent value="security" className="space-y-3">
                  <div>
                    <h3 className="text-sm font-medium">Redefinir senha</h3>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Define uma nova senha administrativa para esta conta.
                    </p>
                  </div>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <Input
                      type="password"
                      value={resetPassword}
                      onChange={(event) => setResetPassword(event.target.value)}
                      placeholder="Nova senha (mín. 8)"
                      className="sm:max-w-xs"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      className="w-full sm:w-auto"
                      disabled={busyKey === 'reset'}
                      onClick={() => void handleResetPassword(selected)}
                    >
                      {busyKey === 'reset' ? 'Redefinindo…' : 'Redefinir'}
                    </Button>
                  </div>
                </TabsContent>
              </Tabs>
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden min-w-0 border-border/60 border-dashed bg-card/50 lg:flex lg:h-[calc(100dvh-10.5rem)]">
            <CardContent className="flex flex-1 flex-col items-center justify-center gap-3 px-6 py-12 text-center">
              <div className="flex size-12 items-center justify-center rounded-full bg-muted/50 text-muted-foreground">
                <Users className="size-5" />
              </div>
              <div className="space-y-1">
                <p className="font-medium">Selecione uma conta</p>
                <p className="max-w-sm text-sm text-muted-foreground">
                  Escolha um item na lista para gerenciar papéis, permissões, status e senha.
                </p>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nova conta</DialogTitle>
            <DialogDescription>
              Crie um usuário livre ou um administrador com chave mestra.
            </DialogDescription>
          </DialogHeader>
          <Form {...createForm}>
            <form className="space-y-4" onSubmit={createForm.handleSubmit(handleCreate)}>
              <FormField
                control={createForm.control}
                name="accountType"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Tipo</FormLabel>
                    <FormControl>
                      <ToggleGroup
                        type="single"
                        variant="outline"
                        value={field.value}
                        onValueChange={(value) => {
                          if (value) field.onChange(value);
                        }}
                        className="grid w-full grid-cols-2 gap-2"
                      >
                        <ToggleGroupItem value="usuario" className="w-full">Usuário</ToggleGroupItem>
                        <ToggleGroupItem value="admin" className="w-full">Admin</ToggleGroupItem>
                      </ToggleGroup>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {accountType === 'admin' ? (
                <FormField
                  control={createForm.control}
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
                control={createForm.control}
                name="username"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Usuário</FormLabel>
                    <FormControl>
                      <Input autoComplete="off" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={createForm.control}
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
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={createForm.formState.isSubmitting}>
                  {createForm.formState.isSubmitting ? 'Criando…' : 'Criar conta'}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
