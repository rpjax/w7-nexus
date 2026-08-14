import { useCallback, useEffect, useMemo, useState } from 'react';
import { Copy, Layers } from 'lucide-react';
import { Link } from 'react-router-dom';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import {
  bindEmissionRail,
  listEmissionRails,
  listOperationRails,
  unbindEmissionRail,
  type EmissionRail,
} from '@/api/administrator/charging';
import { listAgencyDeals, type AgencyDeal } from '@/api/administrator/mandates';
import {
  assignOperator,
  configureOperationCut,
  createOperation,
  deleteStoreObject,
  getOperation,
  listOperations,
  listStoreObjects,
  registerScript,
  resolveScript,
  transitionOperation,
  unassignOperator,
  upsertStoreObject,
  type Operation,
  type StoreObject,
} from '@/api/administrator/operations';
import { listWorldAccounts, type WorldAccount } from '@/api/administrator/worldAccounts';
import type { AccountDetails } from '@/auth/types';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
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
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { reportError, reportSuccess } from '@/feedback';

const TRANSITIONS: Record<string, string[]> = {
  Draft: ['Active', 'Closed'],
  Active: ['Paused', 'Closed', 'Draft'],
  Paused: ['Active', 'Closed'],
  Closed: [],
};

const STATUS_LABEL: Record<string, string> = {
  Draft: 'Rascunho',
  Active: 'Ativa',
  Paused: 'Pausada',
  Closed: 'Encerrada',
};

const TRANSITION_LABEL: Record<string, string> = {
  Draft: 'Voltar a rascunho',
  Active: 'Ativar',
  Paused: 'Pausar',
  Closed: 'Encerrar',
};

type ConfirmKind =
  | { kind: 'unbind'; railId: string }
  | { kind: 'store'; objectId: string }
  | { kind: 'close' };

function statusLabel(status: string): string {
  return STATUS_LABEL[status] ?? status;
}

function statusBadgeVariant(status: string): 'secondary' | 'success' | 'warning' | 'destructive' | 'outline' {
  if (status === 'Active') return 'success';
  if (status === 'Paused') return 'warning';
  if (status === 'Closed') return 'destructive';
  return 'secondary';
}

function humanizeOpsError(message: string): string {
  if (/AgencyDeal/i.test(message) || /precisa ser Operator/i.test(message)) {
    return 'Só dá para associar quem já é Operador com deal de agenciamento ativo. Abra Deals ou Membros para preparar isso.';
  }
  if (/Cut de gest/i.test(message) || /gestao deve/i.test(message)) {
    return 'Percentual de gestão deve ficar vazio ou entre 0 e 100.';
  }
  if (/ponta x gest/i.test(message) || /nao pode gerir e atuar/i.test(message)) {
    return 'Esta pessoa já gere a operação; não pode atuar como operador na mesma frente.';
  }
  return message;
}

function parseCut(raw: string): { ok: true; value: number | null } | { ok: false; message: string } {
  const trimmed = raw.trim();
  if (trimmed === '') return { ok: true, value: null };
  const value = Number(trimmed.replace(',', '.'));
  if (!Number.isFinite(value) || value < 0 || value > 100) {
    return { ok: false, message: 'Percentual de gestão deve ficar vazio ou entre 0 e 100.' };
  }
  return { ok: true, value };
}

export function OperationsPage() {
  const [items, setItems] = useState<Operation[]>([]);
  const [selected, setSelected] = useState<Operation | null>(null);
  const [storeItems, setStoreItems] = useState<StoreObject[]>([]);
  const [storeListError, setStoreListError] = useState<string | null>(null);
  const [accounts, setAccounts] = useState<AccountDetails[]>([]);
  const [deals, setDeals] = useState<AgencyDeal[]>([]);
  const [worldAccounts, setWorldAccounts] = useState<WorldAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [name, setName] = useState('');
  const [memberId, setMemberId] = useState('');
  const [cut, setCut] = useState('');
  const [cutFieldError, setCutFieldError] = useState<string | null>(null);
  const [scriptName, setScriptName] = useState('default');
  const [scriptBody, setScriptBody] = useState('// script');
  const [resolvedScript, setResolvedScript] = useState('');
  const [objectType, setObjectType] = useState('note');
  const [payloadJson, setPayloadJson] = useState('{"ok":true}');
  const [rails, setRails] = useState<EmissionRail[]>([]);
  const [boundRailIds, setBoundRailIds] = useState<string[]>([]);
  const [bindRailId, setBindRailId] = useState('');
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmKind | null>(null);

  const accountById = useMemo(() => new Map(accounts.map((account) => [account.id, account])), [accounts]);
  const worldById = useMemo(
    () => new Map(worldAccounts.map((account) => [account.accountId, account])),
    [worldAccounts],
  );

  const load = useCallback(async () => {
    setLoading(true);
    setListError(null);
    const [ops, members, worlds, dealList] = await Promise.all([
      listOperations(),
      searchAdministratorAccounts({ limit: 100, offset: 0 }),
      listWorldAccounts(),
      listAgencyDeals(),
    ]);
    setLoading(false);

    if (!ops.ok || !ops.data) {
      const message = ops.ok ? 'Resposta inválida.' : humanizeOpsError(ops.error);
      setListError(message);
      reportError(message);
      return;
    }
    setItems(ops.data.items ?? []);
    if (!members.ok || !members.data) {
      reportError(members.ok ? 'Não foi possível listar membros.' : humanizeOpsError(members.error));
    } else {
      setAccounts(members.data.items ?? []);
    }
    if (!worlds.ok || !worlds.data) {
      reportError(worlds.ok ? 'Não foi possível listar contas do livro-mundo.' : humanizeOpsError(worlds.error));
    } else {
      setWorldAccounts(worlds.data.items ?? []);
    }
    if (dealList.ok && dealList.data) {
      setDeals(dealList.data.items ?? []);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function select(operationId: string) {
    setDetailLoading(true);
    const result = await getOperation(operationId);
    if (!result.ok || !result.data) {
      setDetailLoading(false);
      reportError(result.ok ? 'Operação indisponível.' : humanizeOpsError(result.error));
      return;
    }
    setSelected(result.data);
    setCut(result.data.managementCutPercent?.toString() ?? '');
    setCutFieldError(null);
    setResolvedScript('');
    const [store, allRails, bound] = await Promise.all([
      listStoreObjects(operationId),
      listEmissionRails(),
      listOperationRails(operationId),
    ]);
    if (!store.ok || !store.data) {
      const message = store.ok ? 'Não foi possível listar o Store.' : humanizeOpsError(store.error);
      reportError(message);
      setStoreListError(message);
    } else {
      setStoreListError(null);
      setStoreItems(store.data.items ?? []);
    }
    if (!allRails.ok || !allRails.data) {
      reportError(allRails.ok ? 'Não foi possível listar trilhos.' : humanizeOpsError(allRails.error));
      setRails([]);
    } else {
      setRails(allRails.data.items ?? []);
    }
    if (!bound.ok || !bound.data) {
      reportError(bound.ok ? 'Não foi possível listar trilhos da operação.' : humanizeOpsError(bound.error));
      setBoundRailIds([]);
    } else {
      setBoundRailIds(bound.data.railIds ?? []);
    }
    setBindRailId('');
    setDetailLoading(false);
  }

  function closeDetail() {
    setSelected(null);
    setStoreItems([]);
    setStoreListError(null);
    setShowAdvanced(false);
  }

  const unboundRails = useMemo(
    () => rails.filter((rail) => !boundRailIds.includes(rail.railId)),
    [boundRailIds, rails],
  );
  const boundRails = useMemo(
    () => rails.filter((rail) => boundRailIds.includes(rail.railId)),
    [boundRailIds, rails],
  );

  function railLabel(rail: EmissionRail): string {
    const world = worldById.get(rail.railId);
    const orange = accountById.get(rail.orangeMemberId);
    const name = world?.label ?? orange?.username ?? 'Trilho';
    return `${name} · quota ${rail.quotaRemaining} ${rail.currency}`;
  }

  function memberLabel(id: string): string {
    return accountById.get(id)?.username ?? 'Membro';
  }

  const eligibleOperators = useMemo(() => {
    const assigned = new Set(selected?.assignedOperatorIds ?? []);
    const activeOperatorIds = new Set(
      deals.filter((deal) => deal.status === 'Active').map((deal) => deal.operatorAccountId),
    );
    return accounts.filter((account) => activeOperatorIds.has(account.id) && !assigned.has(account.id));
  }, [accounts, deals, selected]);

  async function copyText(value: string, success: string) {
    try {
      await navigator.clipboard.writeText(value);
      reportSuccess(success);
    } catch {
      reportError('Não foi possível copiar.');
    }
  }

  async function handleCreate() {
    setBusy(true);
    const result = await createOperation(name.trim());
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Operação criada.');
    setName('');
    await load();
    if (result.data?.operationId) await select(result.data.operationId);
  }

  async function handleTransition(target: string) {
    if (!selected) return;
    setBusy(true);
    const result = await transitionOperation(selected.operationId, target);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess(`Status → ${statusLabel(target)}`);
    await load();
    await select(selected.operationId);
  }

  async function handleCut() {
    if (!selected) return;
    const parsed = parseCut(cut);
    if (!parsed.ok) {
      setCutFieldError(parsed.message);
      return;
    }
    setCutFieldError(null);
    setBusy(true);
    const result = await configureOperationCut(selected.operationId, parsed.value);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess(parsed.value == null ? 'Percentual de gestão removido.' : 'Percentual de gestão salvo.');
    await select(selected.operationId);
  }

  async function handleAssign() {
    if (!selected || !memberId) return;
    setBusy(true);
    const result = await assignOperator(selected.operationId, memberId);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Operador associado.');
    setMemberId('');
    await select(selected.operationId);
  }

  async function handleUnassign(id: string) {
    if (!selected) return;
    setBusy(true);
    const result = await unassignOperator(selected.operationId, id);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Operador removido.');
    await select(selected.operationId);
  }

  async function handleBind() {
    if (!selected || !bindRailId) return;
    setBusy(true);
    const result = await bindEmissionRail(selected.operationId, bindRailId);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Trilho ligado.');
    setBindRailId('');
    await select(selected.operationId);
  }

  async function handleUnbind(railId: string) {
    if (!selected) return;
    setBusy(true);
    const result = await unbindEmissionRail(selected.operationId, railId);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Trilho desligado.');
    await select(selected.operationId);
  }

  async function handleScript() {
    if (!selected) return;
    setBusy(true);
    const result = await registerScript(selected.operationId, scriptName.trim(), scriptBody);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Script registrado.');
  }

  async function handleResolve() {
    if (!selected) return;
    setBusy(true);
    const result = await resolveScript(selected.operationKey);
    setBusy(false);
    if (!result.ok || !result.data) {
      reportError(result.ok ? 'Nenhum script habilitado nesta frente.' : humanizeOpsError(result.error));
      setResolvedScript('');
      return;
    }
    setResolvedScript(result.data.body);
    reportSuccess('Corpo do script carregado para conferência.');
  }

  async function handleStoreUpsert() {
    if (!selected) return;
    setBusy(true);
    const result = await upsertStoreObject(selected.operationId, {
      objectType: objectType.trim(),
      payloadJson,
    });
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Objeto salvo.');
    await select(selected.operationId);
  }

  async function handleStoreDelete(objectId: string) {
    if (!selected) return;
    setBusy(true);
    const result = await deleteStoreObject(selected.operationId, objectId);
    setBusy(false);
    if (!result.ok) {
      reportError(humanizeOpsError(result.error));
      return;
    }
    reportSuccess('Objeto removido.');
    await select(selected.operationId);
  }

  const nextStatuses = selected ? TRANSITIONS[selected.status] ?? [] : [];

  const confirmCopy =
    confirm?.kind === 'unbind'
      ? {
          title: 'Desligar trilho',
          description: 'A operação deixa de emitir por este trilho. Pode ligar de novo depois.',
        }
      : confirm?.kind === 'store'
        ? {
            title: 'Remover objeto do Store',
            description: 'O objeto será apagado do Store desta operação.',
          }
        : {
            title: 'Encerrar a frente',
            description:
              'Encerrar é irreversível: a operação não volta a Ativa. Cobranças novas nesta frente deixam de ser aceitas.',
          };

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Operações"
        description="Ciclo de vida, operadores e trilhos de emissão. Script e Store ficam em avançado."
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.2fr)]">
        <Card className={cn('border-border/60 bg-card/90', selected && 'hidden lg:block')}>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Layers className="size-4" />
              Lista
            </CardTitle>
            <CardDescription>Crie e selecione uma frente.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex gap-2">
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome da operação" />
              <Button type="button" disabled={busy || !name.trim()} onClick={() => void handleCreate()}>
                Criar
              </Button>
            </div>
            <Separator />
            {loading ? (
              <div className="space-y-2">
                <Skeleton className="h-14 w-full" />
                <Skeleton className="h-14 w-full" />
                <Skeleton className="h-14 w-full" />
              </div>
            ) : listError ? (
              <div className="space-y-2 rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-3">
                <p className="text-sm text-destructive">{listError}</p>
                <Button type="button" size="sm" variant="outline" onClick={() => void load()}>
                  Tentar de novo
                </Button>
              </div>
            ) : items.length === 0 ? (
              <p className="rounded-lg border border-dashed border-border/60 px-3 py-6 text-center text-sm text-muted-foreground">
                Nenhuma operação ainda. Crie a primeira frente acima.
              </p>
            ) : (
              <div className="space-y-2">
                {items.map((item) => (
                  <button
                    key={item.operationId}
                    type="button"
                    className={cn(
                      'w-full rounded-lg border border-border/60 px-3 py-2 text-left text-sm transition-colors hover:bg-muted/40',
                      selected?.operationId === item.operationId && 'border-primary/40 bg-muted/30',
                    )}
                    onClick={() => void select(item.operationId)}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-medium">{item.name}</span>
                      <Badge variant={statusBadgeVariant(item.status)}>{statusLabel(item.status)}</Badge>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {detailLoading && !selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardContent className="space-y-3 p-6">
              <Skeleton className="h-6 w-40" />
              <Skeleton className="h-4 w-64" />
              <Skeleton className="h-24 w-full" />
            </CardContent>
          </Card>
        ) : selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardHeader>
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <CardTitle className="text-base">{selected.name}</CardTitle>
                  <CardDescription>Frente em {statusLabel(selected.status).toLowerCase()}.</CardDescription>
                </div>
                <Badge variant={statusBadgeVariant(selected.status)}>{statusLabel(selected.status)}</Badge>
              </div>
              <Button type="button" size="sm" variant="ghost" className="mt-1 w-fit px-0 lg:hidden" onClick={closeDetail}>
                Voltar à lista
              </Button>
            </CardHeader>
            <CardContent className="space-y-5">
              {detailLoading ? <Skeleton className="h-8 w-full" /> : null}

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Ciclo</h3>
                {nextStatuses.length === 0 ? (
                  <p className="text-sm text-muted-foreground">Encerrada — sem transições.</p>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    {nextStatuses.map((target) => (
                      <Button
                        key={target}
                        type="button"
                        size="sm"
                        variant={target === 'Closed' ? 'destructive' : 'outline'}
                        disabled={busy}
                        onClick={() => {
                          if (target === 'Closed') setConfirm({ kind: 'close' });
                          else void handleTransition(target);
                        }}
                      >
                        {TRANSITION_LABEL[target] ?? statusLabel(target)}
                      </Button>
                    ))}
                  </div>
                )}
              </section>

              <Separator />

              <section className="space-y-2">
                <div className="space-y-1">
                  <Label htmlFor="ops-cut">Percentual de gestão (%)</Label>
                  <p className="text-xs text-muted-foreground">Vazio = sem percentual. Aceita 0 a 100.</p>
                </div>
                <div className="flex gap-2">
                  <Input
                    id="ops-cut"
                    type="number"
                    min={0}
                    max={100}
                    step="0.01"
                    inputMode="decimal"
                    value={cut}
                    onChange={(e) => {
                      setCut(e.target.value);
                      setCutFieldError(null);
                    }}
                    placeholder="Vazio"
                    aria-invalid={cutFieldError ? true : undefined}
                  />
                  <Button type="button" size="sm" disabled={busy} onClick={() => void handleCut()}>
                    Salvar
                  </Button>
                </div>
                {cutFieldError ? <p className="text-xs text-destructive">{cutFieldError}</p> : null}
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Operadores</h3>
                {eligibleOperators.length === 0 ? (
                  <p className="rounded-lg border border-dashed border-border/60 px-3 py-3 text-xs text-muted-foreground">
                    Ninguém elegível agora. Primeiro crie um deal de agenciamento e conceda o preset Operador.{' '}
                    <Link className="text-foreground underline underline-offset-2" to="/dashboard/deals">
                      Deals
                    </Link>
                    {' · '}
                    <Link className="text-foreground underline underline-offset-2" to="/dashboard/accounts">
                      Membros
                    </Link>
                  </p>
                ) : (
                  <div className="flex gap-2">
                    <Select value={memberId || undefined} onValueChange={setMemberId}>
                      <SelectTrigger className="min-w-0 flex-1">
                        <SelectValue placeholder="Escolher operador" />
                      </SelectTrigger>
                      <SelectContent>
                        {eligibleOperators.map((account) => (
                          <SelectItem key={account.id} value={account.id}>
                            {account.username}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button type="button" size="sm" disabled={busy || !memberId} onClick={() => void handleAssign()}>
                      Associar
                    </Button>
                  </div>
                )}
                {selected.assignedOperatorIds.length === 0 ? (
                  <p className="text-xs text-muted-foreground">Nenhum operador associado.</p>
                ) : (
                  <div className="space-y-1">
                    {selected.assignedOperatorIds.map((id) => (
                      <div key={id} className="flex items-center justify-between gap-2 rounded border border-border/60 px-2 py-1.5 text-xs">
                        <span className="truncate font-medium">{memberLabel(id)}</span>
                        <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleUnassign(id)}>
                          Remover
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Trilhos de emissão</h3>
                {rails.length === 0 ? (
                  <p className="rounded-lg border border-dashed border-border/60 px-3 py-3 text-xs text-muted-foreground">
                    Nenhum trilho no mundo ainda. Abra uma conta no livro-mundo e emita o trilho lá; depois volte para ligar nesta frente.{' '}
                    <Link className="text-foreground underline underline-offset-2" to="/dashboard/world-accounts">
                      Livro-mundo
                    </Link>
                  </p>
                ) : (
                  <div className="flex gap-2">
                    <Select value={bindRailId || undefined} onValueChange={setBindRailId}>
                      <SelectTrigger className="min-w-0 flex-1">
                        <SelectValue
                          placeholder={unboundRails.length === 0 ? 'Todos os trilhos já ligados' : 'Escolher trilho'}
                        />
                      </SelectTrigger>
                      <SelectContent>
                        {unboundRails.map((rail) => (
                          <SelectItem key={rail.railId} value={rail.railId}>
                            {railLabel(rail)}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button type="button" size="sm" disabled={busy || !bindRailId} onClick={() => void handleBind()}>
                      Ligar
                    </Button>
                  </div>
                )}
                {boundRails.length === 0 ? (
                  <p className="text-xs text-muted-foreground">Nenhum trilho ligado a esta operação.</p>
                ) : (
                  <div className="space-y-1">
                    {boundRails.map((rail) => (
                      <div key={rail.railId} className="flex items-center justify-between gap-2 rounded border border-border/60 px-2 py-1.5 text-xs">
                        <span className="truncate">{railLabel(rail)}</span>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={busy}
                          onClick={() => setConfirm({ kind: 'unbind', railId: rail.railId })}
                        >
                          Desligar
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </section>

              <Separator />

              <div>
                {showAdvanced ? (
                  <Tabs defaultValue="script">
                    <div className="mb-3 flex items-center justify-between gap-2">
                      <TabsList>
                        <TabsTrigger value="script">Script</TabsTrigger>
                        <TabsTrigger value="store">Store</TabsTrigger>
                      </TabsList>
                      <Button type="button" size="sm" variant="ghost" onClick={() => setShowAdvanced(false)}>
                        Ocultar avançado
                      </Button>
                    </div>
                    <TabsContent value="script" className="space-y-2">
                      <p className="text-xs text-muted-foreground">
                        Registrar grava o texto na frente. Resolver busca o script habilitado desta operação para o canal
                        externo (integração) — não é um “edge” de rede.
                      </p>
                      <div className="flex flex-wrap items-center gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() => void copyText(selected.operationKey, 'Chave copiada.')}
                        >
                          <Copy className="mr-1 size-3.5" />
                          Copiar chave
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          onClick={() => void copyText(selected.operationId, 'ID copiado.')}
                        >
                          Copiar ID
                        </Button>
                      </div>
                      <div className="space-y-1">
                        <Label htmlFor="ops-script-name">Nome</Label>
                        <Input id="ops-script-name" value={scriptName} onChange={(e) => setScriptName(e.target.value)} />
                      </div>
                      <Textarea value={scriptBody} onChange={(e) => setScriptBody(e.target.value)} rows={4} />
                      <div className="flex gap-2">
                        <Button type="button" size="sm" disabled={busy} onClick={() => void handleScript()}>
                          Registrar
                        </Button>
                        <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleResolve()}>
                          Resolver script
                        </Button>
                      </div>
                      {resolvedScript ? (
                        <pre className="overflow-auto rounded bg-muted/40 p-2 text-xs">{resolvedScript}</pre>
                      ) : null}
                    </TabsContent>
                    <TabsContent value="store" className="space-y-2">
                      <p className="text-xs text-muted-foreground">Payload em JSON. Sem Store SQL rico.</p>
                      <div className="space-y-1">
                        <Label htmlFor="ops-store-type">Tipo</Label>
                        <Input id="ops-store-type" value={objectType} onChange={(e) => setObjectType(e.target.value)} />
                      </div>
                      <Textarea value={payloadJson} onChange={(e) => setPayloadJson(e.target.value)} rows={3} />
                      <Button type="button" size="sm" disabled={busy} onClick={() => void handleStoreUpsert()}>
                        Salvar objeto
                      </Button>
                      {storeListError ? (
                        <div className="space-y-2 rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2">
                          <p className="text-xs text-destructive">{storeListError}</p>
                          <Button type="button" size="sm" variant="outline" onClick={() => void select(selected.operationId)}>
                            Tentar listar de novo
                          </Button>
                        </div>
                      ) : storeItems.length === 0 ? (
                        <p className="text-xs text-muted-foreground">Nenhum objeto no Store.</p>
                      ) : (
                        <div className="space-y-1">
                          {storeItems.map((item) => (
                            <div key={item.objectId} className="rounded border border-border/60 px-2 py-1.5 text-xs">
                              <div className="flex items-center justify-between gap-2">
                                <span className="font-medium">{item.objectType}</span>
                                <Button
                                  type="button"
                                  size="sm"
                                  variant="outline"
                                  disabled={busy}
                                  onClick={() => setConfirm({ kind: 'store', objectId: item.objectId })}
                                >
                                  Remover
                                </Button>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </TabsContent>
                  </Tabs>
                ) : (
                  <Button type="button" size="sm" variant="ghost" className="px-0 text-muted-foreground" onClick={() => setShowAdvanced(true)}>
                    Mostrar avançado (Script / Store)
                  </Button>
                )}
              </div>
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden border-dashed border-border/60 bg-card/50 lg:flex">
            <CardContent className="flex flex-1 items-center justify-center p-8 text-sm text-muted-foreground">
              Selecione uma operação na lista.
            </CardContent>
          </Card>
        )}
      </div>

      <Dialog open={confirm !== null} onOpenChange={(open) => { if (!open) setConfirm(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{confirmCopy.title}</DialogTitle>
            <DialogDescription>{confirmCopy.description}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirm(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={busy}
              onClick={() => {
                const current = confirm;
                setConfirm(null);
                if (current?.kind === 'unbind') void handleUnbind(current.railId);
                if (current?.kind === 'store') void handleStoreDelete(current.objectId);
                if (current?.kind === 'close') void handleTransition('Closed');
              }}
            >
              Confirmar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
