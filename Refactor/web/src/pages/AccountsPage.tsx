import { useCallback, useEffect, useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { type ColumnDef } from '@tanstack/react-table';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { z } from 'zod';
import { ArrowLeft, Plus, RefreshCw, Search, Users } from 'lucide-react';
import {
  createAdministratorAccount,
  disableAdministratorAccount,
  enableAdministratorAccount,
  getAdministratorAccount,
  grantAdministratorAccountRole,
  resetAdministratorAccountPassword,
  revokeAdministratorAccountRole,
  searchAdministratorAccounts,
  type CreateAccountType,
} from '@/api/administrator/accounts';
import {
  getMemberMandate,
  grantMandateCapability,
  grantMandatePreset,
  MANDATE_PRESETS,
  presetLabel,
  recordMemberAttrition,
  revokeMandateCapability,
  revokeMandatePreset,
  type MemberMandate,
} from '@/api/administrator/mandates';
import { listOperations, type Operation } from '@/api/administrator/operations';
import { useAuth } from '@/auth/AuthContext';
import type { AccountDetails } from '@/auth/types';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { PageHeader } from '@/components/layout/page-header';
import { StatusBadge } from '@/components/StatusBadge';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
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
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import {
  ACCOUNT_ROLE_CATALOG,
  isAdministrator,
} from '@/utils/accountAccess';
import { cn } from '@/lib/utils';
import { reportError, reportSuccess } from '@/feedback';

const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;

const ATTRITION_STATUS_OPTIONS = [
  { id: 'burned', label: 'Queimado' },
  { id: 'left', label: 'Saiu' },
  { id: 'betrayed', label: 'Traiu' },
] as const;

const ATTRITION_CAUSE_OPTIONS = [
  { id: 'bloqueio_bancario', label: 'Bloqueio bancário' },
  { id: 'apreensao', label: 'Apreensão' },
  { id: 'traicao', label: 'Traição' },
  { id: 'saida_voluntaria', label: 'Saída voluntária' },
  { id: 'erro_operacional', label: 'Erro operacional' },
  { id: 'estorno', label: 'Estorno' },
  { id: 'desconhecido', label: 'Desconhecido' },
] as const;

const CAUSES_BY_STATUS: Record<string, readonly string[]> = {
  burned: ['bloqueio_bancario', 'apreensao', 'erro_operacional', 'estorno', 'desconhecido'],
  betrayed: ['traicao'],
  left: ['saida_voluntaria'],
};

function causesForStatus(status: string) {
  const ids = CAUSES_BY_STATUS[status] ?? CAUSES_BY_STATUS.burned;
  return ATTRITION_CAUSE_OPTIONS.filter((item) => ids.includes(item.id));
}

function capabilityLabel(capability: string): string {
  if (capability === 'gerir_operacao') return 'Gerir operação';
  if (capability === 'recrutar') return 'Recrutar';
  return capability.replaceAll('_', ' ');
}

function attritionStatusLabel(value: string): string {
  return ATTRITION_STATUS_OPTIONS.find((item) => item.id === value)?.label ?? value;
}

function attritionCauseLabel(value: string): string {
  return ATTRITION_CAUSE_OPTIONS.find((item) => item.id === value)?.label ?? value;
}

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
  const [appliedPresets, setAppliedPresets] = useState<string[]>([]);
  const [mandateGrants, setMandateGrants] = useState<MemberMandate['grants']>([]);
  const [attritionStatus, setAttritionStatus] = useState('active');
  const [attritionCause, setAttritionCause] = useState<string | null>(null);
  const [attritionPick, setAttritionPick] = useState('burned');
  const [attritionCausePick, setAttritionCausePick] = useState('apreensao');
  const [listMandates, setListMandates] = useState<Record<string, string[]>>({});
  const [specificOpIds, setSpecificOpIds] = useState('');
  const [selectedOperationIds, setSelectedOperationIds] = useState<string[]>([]);
  const [operations, setOperations] = useState<Operation[]>([]);
  const [operationsUnavailable, setOperationsUnavailable] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState<null | {
    kind: 'grant-admin' | 'revoke-admin' | 'disable' | 'enable' | 'grant-preset' | 'revoke-preset' | 'attrition' | 'reset';
    account: AccountDetails;
    presetId?: string;
  }>(null);

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
    const mandatePresetFilter = MANDATE_PRESETS.some((preset) => preset.id === nextRole);

    setLoading(true);
    const result = await searchAdministratorAccounts({
      limit: nextPageSize,
      offset: (nextPage - 1) * nextPageSize,
      keyword: nextKeyword.trim() || undefined,
      status: nextStatus === 'all' ? undefined : nextStatus,
      role: nextRole === 'all' || mandatePresetFilter ? undefined : nextRole,
    });
    setLoading(false);

    if (!result.ok || !result.data) {
      reportError(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }

    let rows = result.data.items ?? [];
    const mandateMap: Record<string, string[]> = {};
    await Promise.all(rows.map(async (account) => {
      const mandate = await getMemberMandate(account.id);
      if (mandate.ok && mandate.data) {
        mandateMap[account.id] = mandate.data.appliedPresets ?? [];
      }
    }));
    setListMandates(mandateMap);

    if (mandatePresetFilter) {
      rows = rows.filter((account) =>
        (mandateMap[account.id] ?? []).some(
          (preset) => preset.localeCompare(nextRole, undefined, { sensitivity: 'accent' }) === 0,
        ),
      );
    }

    setItems(rows);
    setTotal(mandatePresetFilter ? rows.length : result.data.total);
  }, [keyword, page, pageSize, roleFilter, statusFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    void (async () => {
      const result = await listOperations();
      if (!result.ok || !result.data) {
        reportError(result.ok ? 'Não foi possível listar operações.' : result.error);
        setOperationsUnavailable(true);
        setOperations([]);
        return;
      }
      setOperationsUnavailable(false);
      setOperations(result.data.items ?? []);
    })();
  }, []);

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
      header: 'Poder',
      cell: ({ row }) => {
        const presets = listMandates[row.original.id] ?? [];
        return (
          <div className="flex max-w-[14rem] flex-wrap gap-0.5">
            {isAdministrator(row.original.roles) ? (
              <Badge variant="secondary" className="h-5 px-1.5 text-[0.65rem]">Admin</Badge>
            ) : null}
            {presets.map((preset) => (
              <Badge key={preset} variant="outline" className="h-5 px-1.5 text-[0.65rem]">
                {presetLabel(preset)}
              </Badge>
            ))}
            {!isAdministrator(row.original.roles) && presets.length === 0 ? (
              <span className="text-xs text-muted-foreground">Só login</span>
            ) : null}
          </div>
        );
      },
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
  ], [listMandates]);

  function selectAccount(account: AccountDetails) {
    setSelected(account);
    setResetPassword('');
    setAppliedPresets([]);
    setMandateGrants([]);
    setAttritionStatus('active');
    setAttritionCause(null);
    void refreshSelected(account.id);
  }

  async function loadMandate(accountId: string) {
    const result = await getMemberMandate(accountId);
    if (!result.ok || !result.data) {
      reportError(result.ok ? 'Mandato indisponível.' : result.error);
      setAppliedPresets([]);
      setMandateGrants([]);
      setAttritionStatus('active');
      setAttritionCause(null);
      return;
    }
    setAppliedPresets(result.data.appliedPresets ?? []);
    setMandateGrants(result.data.grants ?? []);
    setAttritionStatus(result.data.attritionStatus ?? 'active');
    setAttritionCause(result.data.attritionCause ?? null);
    setListMandates((current) => ({
      ...current,
      [accountId]: result.data?.appliedPresets ?? [],
    }));
  }

  async function grantSpecificGerirOperacao() {
    if (!selected) return;
    const ids = operationsUnavailable
      ? specificOpIds.split(/[\s,;]+/).map((part) => part.trim()).filter(Boolean)
      : selectedOperationIds;
    if (ids.length === 0) {
      reportError(operationsUnavailable
        ? 'Informe ao menos um ID de operação.'
        : 'Selecione ao menos uma operação.');
      return;
    }
    setBusyKey('cap:specific');
    const result = await grantMandateCapability({
      accountId: selected.id,
      capability: 'gerir_operacao',
      scopeKind: 'OperationSpecific',
      operationIds: ids,
    });
    setBusyKey(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Poder de gerir operação concedido.');
    setSpecificOpIds('');
    setSelectedOperationIds([]);
    await loadMandate(selected.id);
  }

  async function revokeGrant(grant: MemberMandate['grants'][number]) {
    if (!selected) return;
    setBusyKey(`cap:${grant.id}`);
    const result = await revokeMandateCapability({
      accountId: selected.id,
      capability: grant.capability,
      scopeKind: grant.scopeKind,
      operationIds: grant.operationIds,
    });
    setBusyKey(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Capacidade revogada.');
    await loadMandate(selected.id);
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
      reportError(result.ok ? 'Conta indisponível.' : result.error);
      return;
    }
    setSelected(result.data.account);
    setItems((current) => current.map((item) => (
      item.id === accountId ? result.data!.account : item
    )));
    await loadMandate(accountId);
  }

  async function handleCreate(values: CreateValues) {
    const result = await createAdministratorAccount({
      username: values.username.trim(),
      password: values.password,
      accountType: values.accountType as CreateAccountType,
      masterKey: values.masterKey?.trim(),
    });
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    createForm.reset({ accountType: 'usuario', username: '', password: '', masterKey: '' });
    setCreateOpen(false);
    reportSuccess('Membro criado.');
    setKeyword('');
    setPage(1);
    await load({ keyword: '', page: 1 });
    if (result.data?.account) {
      const created = result.data.account;
      setItems((current) => (
        current.some((item) => item.id === created.id) ? current : [created, ...current]
      ));
      setSelected(created);
      void loadMandate(created.id);
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
      reportError(result.error);
      return;
    }
    reportSuccess(enabled ? 'Preset Admin concedido.' : 'Preset Admin removido.');
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
      reportError(result.error);
      return;
    }
    reportSuccess(isDisabled ? 'Login reabilitado.' : 'Login desabilitado.');
    if (result.data?.account) {
      setSelected(result.data.account);
      setItems((current) => current.map((item) => (
        item.id === account.id ? result.data!.account : item
      )));
    }
  }

  async function togglePreset(account: AccountDetails, presetId: string, enabled: boolean) {
    const key = `preset:${presetId}`;
    setBusyKey(key);
    const result = enabled
      ? await grantMandatePreset(account.id, presetId)
      : await revokeMandatePreset(account.id, presetId);
    setBusyKey(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess(enabled ? 'Preset concedido.' : 'Preset removido.');
    await loadMandate(account.id);
  }

  async function handleAttrition() {
    if (!selected) return;
    setBusyKey('attrition');
    const result = await recordMemberAttrition(selected.id, attritionPick, attritionCausePick);
    setBusyKey(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Baixa registrada. O login continua até você desabilitar o membro.');
    await loadMandate(selected.id);
  }

  async function runConfirmedAction() {
    if (!confirmAction) return;
    const { kind, account, presetId } = confirmAction;
    setConfirmAction(null);
    if (kind === 'grant-admin') {
      await toggleRole(account, 'Administrator', true);
      return;
    }
    if (kind === 'revoke-admin') {
      await toggleRole(account, 'Administrator', false);
      return;
    }
    if (kind === 'grant-preset' && presetId) {
      await togglePreset(account, presetId, true);
      return;
    }
    if (kind === 'revoke-preset' && presetId) {
      await togglePreset(account, presetId, false);
      return;
    }
    if (kind === 'attrition') {
      await handleAttrition();
      return;
    }
    if (kind === 'reset') {
      await handleResetPassword(account);
      return;
    }
    await handleDisableEnable(account);
  }

  async function handleResetPassword(account: AccountDetails) {
    if (resetPassword.trim().length < 8) {
      reportError('A nova senha deve ter no mínimo 8 caracteres.');
      return;
    }
    setBusyKey('reset');
    const result = await resetAdministratorAccountPassword(account.id, resetPassword);
    setBusyKey(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    setResetPassword('');
    reportSuccess('Senha redefinida.');
  }

  const accountType = createForm.watch('accountType');
  const isSelf = selected?.id === user?.accountId;
  const causeOptions = causesForStatus(attritionPick);
  const powerSuspended = attritionStatus !== 'active';

  useEffect(() => {
    const options = causesForStatus(attritionPick);
    if (!options.some((item) => item.id === attritionCausePick)) {
      setAttritionCausePick(options[0]?.id ?? 'apreensao');
    }
  }, [attritionPick, attritionCausePick]);

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Membros"
        description="Identidades de login. Admin é raiz; mandatos de produto (Recrutador, Operador, …) ficam no detalhe. Queimar tira o poder visível; desabilitar só corta o login."
        actions={(
          <Button type="button" onClick={() => setCreateOpen(true)}>
            <Plus data-icon="inline-start" />
            Novo membro
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
                    <SelectItem value="all">Todos os presets</SelectItem>
                    {ACCOUNT_ROLE_CATALOG.map((role) => (
                      <SelectItem key={role.id} value={role.id}>{role.label}</SelectItem>
                    ))}
                    {MANDATE_PRESETS.map((preset) => (
                      <SelectItem key={preset.id} value={preset.id}>{preset.label}</SelectItem>
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
              emptyMessage={keyword.trim()
                ? 'Nenhum membro com essa busca. Limpe o filtro ou crie um novo.'
                : 'Nenhum membro nesta lista. Crie o primeiro ou afrouxa os filtros.'}
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
              <div className="min-w-0 space-y-2">
                <div className="flex flex-wrap items-center gap-2">
                  <CardTitle className="break-words text-xl">{selected.username}</CardTitle>
                  <StatusBadge status={selected.status} />
                  {isAdministrator(selected.roles) ? (
                    <Badge variant="secondary">Admin</Badge>
                  ) : null}
                  {isSelf ? (
                    <Badge variant="outline">Você</Badge>
                  ) : null}
                </div>
                <CardDescription className="text-xs">
                  Membro desde {formatDateTime(selected.createdAt)} · atualizado {formatDateTime(selected.lastUpdatedAt)}
                </CardDescription>
                <p className="truncate font-mono text-[0.65rem] text-muted-foreground" title={selected.id}>
                  {selected.id}
                </p>
              </div>
            </CardHeader>
            <Separator />
            <CardContent className="min-w-0 space-y-6 p-4 md:p-5">
              <section className="space-y-3">
                <div className="space-y-1">
                  <h3 className="text-sm font-medium">Preset Admin</h3>
                  <p className="text-xs text-muted-foreground">
                    Acesso irrestrito no hub (raiz de atenuação). Separado dos mandatos de produto.
                  </p>
                </div>
                {isAdministrator(selected.roles) ? (
                  <div className="rounded-lg border border-border/60 bg-muted/20 px-3 py-3">
                    <p className="text-sm font-medium">Este membro é Admin.</p>
                    {isSelf ? (
                      <p className="mt-1 text-xs text-muted-foreground">
                        Você não pode remover o próprio Admin. Peça a outro administrador.
                      </p>
                    ) : (
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="mt-3"
                        disabled={busyKey !== null}
                        onClick={() => setConfirmAction({ kind: 'revoke-admin', account: selected })}
                      >
                        Remover Admin…
                      </Button>
                    )}
                  </div>
                ) : (
                  <div className="rounded-lg border border-dashed border-border/60 px-3 py-3">
                    <p className="text-sm text-muted-foreground">Só identidade de login — sem preset Admin.</p>
                    <Button
                      type="button"
                      size="sm"
                      className="mt-3"
                      disabled={busyKey !== null || selected.status === 'Disabled'}
                      onClick={() => setConfirmAction({ kind: 'grant-admin', account: selected })}
                    >
                      Conceder Admin…
                    </Button>
                  </div>
                )}
              </section>

              <Separator />

              <section className="space-y-3">
                <div className="space-y-1">
                  <h3 className="text-sm font-medium">Poder</h3>
                  <p className="text-xs text-muted-foreground">
                    Presets de produto. Operador exige um deal ativo em{' '}
                    <Link to="/dashboard/deals" className="underline underline-offset-2">Deals</Link>
                    . Queimado ou traição tira o poder da ficha; o login só cai se você desabilitar abaixo.
                  </p>
                </div>
                {powerSuspended ? (
                  <p className="rounded-md border border-border/60 bg-muted/30 px-3 py-2 text-xs">
                    Poder suspenso: {attritionStatusLabel(attritionStatus)}
                    {attritionCause ? ` · ${attritionCauseLabel(attritionCause)}` : ''}.
                    Recrutador e outros presets não aparecem ativos.
                  </p>
                ) : null}
                <div className="flex flex-wrap gap-1.5">
                  {appliedPresets.length === 0 ? (
                    <span className="text-xs text-muted-foreground">Nenhum mandato ativo.</span>
                  ) : (
                    appliedPresets.map((preset) => (
                      <Badge key={preset} variant="secondary" title={preset}>{presetLabel(preset)}</Badge>
                    ))
                  )}
                </div>
                <div className="space-y-2">
                  {MANDATE_PRESETS.map((preset) => {
                    const granted = appliedPresets.some(
                      (item) => item.localeCompare(preset.id, undefined, { sensitivity: 'accent' }) === 0,
                    );
                    return (
                      <div
                        key={preset.id}
                        className="flex items-center justify-between gap-3 rounded-lg border border-border/60 px-3 py-2"
                      >
                        <p className="text-sm font-medium" title={preset.id}>{preset.label}</p>
                        <Button
                          type="button"
                          size="sm"
                          variant={granted ? 'outline' : 'default'}
                          disabled={busyKey !== null || selected.status === 'Disabled' || (powerSuspended && !granted)}
                          onClick={() => setConfirmAction({
                            kind: granted ? 'revoke-preset' : 'grant-preset',
                            account: selected,
                            presetId: preset.id,
                          })}
                        >
                          {granted ? 'Revogar…' : 'Conceder…'}
                        </Button>
                      </div>
                    );
                  })}
                </div>

                <div className="space-y-2 rounded-lg border border-dashed border-border/60 px-3 py-3">
                  <p className="text-sm font-medium">Ajuste fino · gerir operação</p>
                  <p className="text-xs text-muted-foreground">
                    {operationsUnavailable
                      ? 'Lista de operações indisponível — cole IDs separados por vírgula. Gestor continua em todas as operações.'
                      : 'Marque as operações pelo nome. Gestor continua em todas as operações.'}
                  </p>
                  {operationsUnavailable ? (
                    <div className="flex flex-col gap-2 sm:flex-row">
                      <Input
                        value={specificOpIds}
                        onChange={(event) => setSpecificOpIds(event.target.value)}
                        placeholder="id-op-1, id-op-2"
                        className="sm:flex-1"
                      />
                      <Button
                        type="button"
                        size="sm"
                        disabled={busyKey !== null || selected.status === 'Disabled' || powerSuspended || !specificOpIds.trim()}
                        onClick={() => void grantSpecificGerirOperacao()}
                      >
                        Conceder
                      </Button>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {operations.length === 0 ? (
                        <p className="text-xs text-muted-foreground">Nenhuma operação cadastrada.</p>
                      ) : (
                        <div className="max-h-40 space-y-1.5 overflow-y-auto">
                          {operations.map((operation) => {
                            const checked = selectedOperationIds.includes(operation.operationId);
                            return (
                              <label
                                key={operation.operationId}
                                className="flex items-center gap-2 rounded-md px-1 py-0.5 text-sm"
                              >
                                <Checkbox
                                  checked={checked}
                                  onCheckedChange={(value) => {
                                    setSelectedOperationIds((current) => (
                                      value === true
                                        ? [...current, operation.operationId]
                                        : current.filter((id) => id !== operation.operationId)
                                    ));
                                  }}
                                />
                                <span className="min-w-0 truncate">{operation.name}</span>
                              </label>
                            );
                          })}
                        </div>
                      )}
                      <Button
                        type="button"
                        size="sm"
                        disabled={busyKey !== null || selected.status === 'Disabled' || powerSuspended || selectedOperationIds.length === 0}
                        onClick={() => void grantSpecificGerirOperacao()}
                      >
                        Conceder
                      </Button>
                    </div>
                  )}
                  <div className="space-y-1">
                    {mandateGrants.length === 0 ? (
                      <p className="text-xs text-muted-foreground">Nenhum poder explícito além dos presets.</p>
                    ) : (
                      mandateGrants.map((grant) => {
                        const opNames = (grant.operationIds ?? []).map((id) =>
                          operations.find((operation) => operation.operationId === id)?.name ?? id.slice(0, 8),
                        );
                        return (
                          <div
                            key={grant.id}
                            className="flex items-start justify-between gap-2 rounded border border-border/50 px-2 py-1.5 text-xs"
                          >
                            <div className="min-w-0">
                              <p className="font-medium">
                                {capabilityLabel(grant.capability)}
                                {opNames.length ? ` · ${opNames.join(', ')}` : ''}
                              </p>
                              {grant.sourcePreset ? (
                                <p className="text-muted-foreground">via {presetLabel(grant.sourcePreset)}</p>
                              ) : null}
                            </div>
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              disabled={busyKey !== null}
                              onClick={() => void revokeGrant(grant)}
                            >
                              Revogar
                            </Button>
                          </div>
                        );
                      })
                    )}
                  </div>
                </div>
              </section>

              <Separator />

              <section className="space-y-3">
                <div className="space-y-1">
                  <h3 className="text-sm font-medium">Baixa</h3>
                  <p className="text-xs text-muted-foreground">
                    Queimado e traição derrubam o mandato visível (e a cascata de quem este membro concedeu).
                    Saída voluntária sobe a gente para o concedente. Não bloqueia o login sozinho; não mexe em dinheiro.
                  </p>
                </div>
                <p className="text-xs text-muted-foreground">
                  Situação:{' '}
                  {attritionStatus === 'active'
                    ? 'Ativo'
                    : attritionStatusLabel(attritionStatus)}
                  {attritionCause ? ` · ${attritionCauseLabel(attritionCause)}` : ''}
                </p>
                <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
                  <div className="min-w-0 flex-1 space-y-1">
                    <Label className="text-xs">Como sai</Label>
                    <Select value={attritionPick} onValueChange={setAttritionPick}>
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {ATTRITION_STATUS_OPTIONS.map((option) => (
                          <SelectItem key={option.id} value={option.id}>{option.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="min-w-0 flex-1 space-y-1">
                    <Label className="text-xs">Causa</Label>
                    <Select value={attritionCausePick} onValueChange={setAttritionCausePick}>
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {causeOptions.map((option) => (
                          <SelectItem key={option.id} value={option.id}>{option.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={busyKey !== null || selected.status === 'Disabled'}
                    onClick={() => setConfirmAction({ kind: 'attrition', account: selected })}
                  >
                    Registrar baixa…
                  </Button>
                </div>
              </section>

              <Separator />

              <section className="space-y-3">
                <div>
                  <h3 className="text-sm font-medium">Senha</h3>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Define uma nova senha para esta conta.
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
                    onClick={() => setConfirmAction({ kind: 'reset', account: selected })}
                  >
                    {busyKey === 'reset' ? 'Redefinindo…' : 'Redefinir…'}
                  </Button>
                </div>
              </section>

              <Separator />

              <section className="space-y-3">
                <div>
                  <h3 className="text-sm font-medium text-destructive">Zona de risco</h3>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Desabilitar bloqueia o login. O usuário permanece reservado.
                  </p>
                </div>
                {isSelf ? (
                  <p className="rounded-lg border border-border/60 bg-muted/30 px-3 py-2 text-xs text-muted-foreground">
                    Você não pode desabilitar a própria sessão.
                  </p>
                ) : (
                  <Button
                    type="button"
                    size="sm"
                    variant={selected.status === 'Disabled' ? 'default' : 'destructive'}
                    disabled={busyKey === 'status'}
                    onClick={() => setConfirmAction({
                      kind: selected.status === 'Disabled' ? 'enable' : 'disable',
                      account: selected,
                    })}
                  >
                    {selected.status === 'Disabled' ? 'Reabilitar login…' : 'Desabilitar login…'}
                  </Button>
                )}
              </section>
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden min-w-0 border-border/60 border-dashed bg-card/50 lg:flex lg:h-[calc(100dvh-10.5rem)]">
            <CardContent className="flex flex-1 flex-col items-center justify-center gap-3 px-6 py-12 text-center">
              <div className="flex size-12 items-center justify-center rounded-full bg-muted/50 text-muted-foreground">
                <Users className="size-5" />
              </div>
              <div className="space-y-1">
                <p className="font-medium">Selecione um membro</p>
                <p className="max-w-sm text-sm text-muted-foreground">
                  {items.length === 0
                    ? 'A lista está vazia com os filtros atuais. Limpe a busca ou crie um membro.'
                    : 'Escolha um item na lista para gerenciar Admin, mandatos, senha e login.'}
                </p>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      <Dialog
        open={confirmAction !== null}
        onOpenChange={(open) => {
          if (!open) setConfirmAction(null);
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {confirmAction?.kind === 'grant-admin' && 'Tornar Admin'}
              {confirmAction?.kind === 'revoke-admin' && 'Remover Admin'}
              {confirmAction?.kind === 'grant-preset' && `Conceder ${presetLabel(confirmAction.presetId ?? '')}`}
              {confirmAction?.kind === 'revoke-preset' && `Revogar ${presetLabel(confirmAction.presetId ?? '')}`}
              {confirmAction?.kind === 'attrition' && `Registrar ${attritionStatusLabel(attritionPick).toLowerCase()}`}
              {confirmAction?.kind === 'disable' && 'Desabilitar login'}
              {confirmAction?.kind === 'enable' && 'Reabilitar login'}
              {confirmAction?.kind === 'reset' && 'Redefinir senha'}
            </DialogTitle>
            <DialogDescription>
              {confirmAction?.kind === 'grant-admin' && (
                <>
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}passará a ter acesso irrestrito no hub.
                </>
              )}
              {confirmAction?.kind === 'revoke-admin' && (
                <>
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}deixará de ser Admin. Não dá para remover o último Admin do sistema.
                </>
              )}
              {confirmAction?.kind === 'grant-preset' && (
                <>
                  Conceder o mandato{' '}
                  <span className="font-medium text-foreground">{presetLabel(confirmAction.presetId ?? '')}</span>
                  {' '}para{' '}
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {confirmAction.presetId === 'Operator' ? (
                    <>. Se falhar, abra <Link to="/dashboard/deals" className="underline">Deals</Link> e cadastre um deal ativo.</>
                  ) : '.'}
                </>
              )}
              {confirmAction?.kind === 'revoke-preset' && (
                <>
                  Remover o mandato{' '}
                  <span className="font-medium text-foreground">{presetLabel(confirmAction.presetId ?? '')}</span>
                  {' '}de{' '}
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}(pode podar sub-mandatos).
                </>
              )}
              {confirmAction?.kind === 'attrition' && (
                <>
                  Marcar{' '}
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}como {attritionStatusLabel(attritionPick).toLowerCase()}
                  {' '}({attritionCauseLabel(attritionCausePick)}).
                  {attritionPick === 'left'
                    ? ' A gente que este membro concedeu sobe para o concedente.'
                    : ' O poder visível (Recrutador e demais presets) cai, inclusive o que este membro concedeu.'}
                  {' '}Não bloqueia o login sozinho; não mexe em dinheiro. Para cortar o acesso, desabilite o login depois.
                </>
              )}
              {confirmAction?.kind === 'disable' && (
                <>
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}não conseguirá entrar até ser reabilitado. Isto não tira Recrutador sozinho — use a baixa para o mandato.
                </>
              )}
              {confirmAction?.kind === 'enable' && (
                <>
                  Liberar o login de{' '}
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}novamente.
                </>
              )}
              {confirmAction?.kind === 'reset' && (
                <>
                  A senha de{' '}
                  <span className="font-medium text-foreground">{confirmAction.account.username}</span>
                  {' '}será substituída pela que você digitou.
                </>
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirmAction(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant={confirmAction?.kind === 'disable' || confirmAction?.kind === 'revoke-admin' || confirmAction?.kind === 'revoke-preset' || confirmAction?.kind === 'attrition' ? 'destructive' : 'default'}
              disabled={busyKey !== null}
              onClick={() => void runConfirmedAction()}
            >
              {confirmAction?.kind === 'grant-admin' && 'Tornar Admin'}
              {confirmAction?.kind === 'revoke-admin' && 'Remover Admin'}
              {confirmAction?.kind === 'grant-preset' && 'Conceder'}
              {confirmAction?.kind === 'revoke-preset' && 'Revogar'}
              {confirmAction?.kind === 'attrition' && attritionStatusLabel(attritionPick)}
              {confirmAction?.kind === 'disable' && 'Desabilitar'}
              {confirmAction?.kind === 'enable' && 'Reabilitar'}
              {confirmAction?.kind === 'reset' && 'Redefinir senha'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Novo membro</DialogTitle>
            <DialogDescription>
              Cria um login. Usuário comum não recebe mandato agora; Admin exige a chave mestra.
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
                    <FormDescription>
                      {field.value === 'admin'
                        ? 'Preset raiz. O último Admin não pode ser desabilitado nem revogado.'
                        : 'Só identidade de login. Mandatos (Operador, Recrutador, …) você concede depois, na ficha.'}
                    </FormDescription>
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
                  {createForm.formState.isSubmitting ? 'Criando…' : 'Criar membro'}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
