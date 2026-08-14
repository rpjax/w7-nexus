import { useCallback, useEffect, useMemo, useState } from 'react';
import { Receipt } from 'lucide-react';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import {
  createCharge,
  getCharge,
  listCharges,
  listEmissionRails,
  markChargePaid,
  transitionCharge,
  type Charge,
  type EmissionRail,
} from '@/api/administrator/charging';
import { getMyCharge, listMyCharges } from '@/api/authenticated/charging';
import { materializeCharge, reverseCharge } from '@/api/administrator/ledger';
import { listOperations, type Operation } from '@/api/administrator/operations';
import { listWorldAccounts, type WorldAccount } from '@/api/administrator/worldAccounts';
import { useHubAccess } from '@/auth/MandateContext';
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
import { cn } from '@/lib/utils';
import { reportError, reportSuccess } from '@/feedback';

type ConfirmKind = 'paid' | 'materialize' | 'reverse' | 'Cancelled' | 'Expired' | 'Failed' | null;

const CHARGE_STATUS_LABEL: Record<string, string> = {
  Open: 'Aberta',
  Paid: 'Paga',
  Materialized: 'Materializada',
  Cancelled: 'Cancelada',
  Expired: 'Expirada',
  Failed: 'Falhou',
  Reversed: 'Estornada',
};

const OPERATION_STATUS_LABEL: Record<string, string> = {
  Draft: 'Rascunho',
  Active: 'Ativa',
  Paused: 'Pausada',
  Closed: 'Encerrada',
};

const WORLD_KIND_LABEL: Record<string, string> = {
  Gateway: 'Gateway',
  Bank: 'Banco',
  Crypto: 'Crypto',
  Payout: 'Payout',
};

const SPLIT_KIND_LABEL: Record<string, string> = {
  Orange: 'Laranja',
  Shareholders: 'Acionistas',
  OperationManagement: 'Gestão da operação',
  Agency: 'Agência',
  ResidualOrg: 'Residual da Org',
};

const REVERSE_CAUSES = [
  { value: 'estorno', label: 'Estorno' },
  { value: 'erro_operacional', label: 'Erro operacional' },
  { value: 'bloqueio_bancario', label: 'Bloqueio bancário' },
  { value: 'apreensao', label: 'Apreensão' },
  { value: 'traicao', label: 'Traição' },
  { value: 'saida_voluntaria', label: 'Saída voluntária' },
  { value: 'desconhecido', label: 'Desconhecido' },
] as const;

function chargeStatusLabel(status: string): string {
  return CHARGE_STATUS_LABEL[status] ?? status;
}

function chargeStatusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning' {
  if (status === 'Failed') return 'destructive';
  if (status === 'Materialized') return 'success';
  if (status === 'Paid') return 'warning';
  if (status === 'Open') return 'warning';
  if (status === 'Cancelled' || status === 'Expired' || status === 'Reversed') return 'outline';
  return 'secondary';
}

function operationStatusLabel(status: string): string {
  return OPERATION_STATUS_LABEL[status] ?? status;
}

function worldKindLabel(kind: string): string {
  return WORLD_KIND_LABEL[kind] ?? kind;
}

function splitKindLabel(kind: string): string {
  return SPLIT_KIND_LABEL[kind] ?? kind;
}

function formatMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).format(amount);
  } catch {
    return `${amount} ${currency}`;
  }
}

function shortId(id: string): string {
  return id.length > 8 ? `${id.slice(0, 8)}…` : id;
}

function memberName(accounts: AccountDetails[], id: string | null | undefined): string {
  if (!id) return '—';
  return accounts.find((item) => item.id === id)?.username ?? shortId(id);
}

function estimatedSplit(gross: number, lines: Array<{ order: number; kind: string; percentOfRemainder: number }>) {
  const rows: Array<{ order: number; kind: string; percent: number; amount: number }> = [];
  let remainder = gross;
  for (const line of [...lines].sort((a, b) => a.order - b.order)) {
    const amount = remainder * (line.percentOfRemainder / 100);
    rows.push({ order: line.order, kind: line.kind, percent: line.percentOfRemainder, amount });
    remainder -= amount;
  }
  return rows;
}

export function ChargesPage() {
  const access = useHubAccess();
  const wide = access.admin || access.canSeeFinance || access.canManageOperations;
  const [items, setItems] = useState<Charge[]>([]);
  const [operations, setOperations] = useState<Operation[]>([]);
  const [worldAccounts, setWorldAccounts] = useState<WorldAccount[]>([]);
  const [rails, setRails] = useState<EmissionRail[]>([]);
  const [members, setMembers] = useState<AccountDetails[]>([]);
  const [selected, setSelected] = useState<Charge | null>(null);
  const [busy, setBusy] = useState(false);
  const [listLoading, setListLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [operationId, setOperationId] = useState('');
  const [operatorId, setOperatorId] = useState('');
  const [amount, setAmount] = useState('100');
  const [netAmount, setNetAmount] = useState('');
  const [landingAccountId, setLandingAccountId] = useState('');
  const [confirm, setConfirm] = useState<ConfirmKind>(null);
  const [reverseCause, setReverseCause] = useState('estorno');

  const operationsById = useMemo(
    () => new Map(operations.map((op) => [op.operationId, op])),
    [operations],
  );

  const selectedOperation = operationsById.get(operationId);
  const assignedOperators = selectedOperation?.assignedOperatorIds ?? [];
  const canGenerate = Boolean(operationId.trim() && (!wide || operatorId.trim()));
  const generateHint = !operationId.trim()
    ? 'Selecione uma operação Ativa para gerar.'
    : wide && assignedOperators.length === 0
      ? 'Associe um operador à operação (em Operações) e escolha-o aqui.'
      : wide && !operatorId.trim()
        ? 'O operador é obrigatório. Escolha quem emite esta cobrança.'
        : selectedOperation && selectedOperation.status !== 'Active'
          ? `Só operação Ativa aceita nova cobrança. Esta está ${operationStatusLabel(selectedOperation.status).toLowerCase()}.`
          : null;

  const loadLookups = useCallback(async () => {
    if (!wide) return;
    const [ops, accounts, railsResult, membersResult] = await Promise.all([
      listOperations(),
      listWorldAccounts(),
      listEmissionRails(),
      searchAdministratorAccounts({ limit: 200, offset: 0 }),
    ]);
    if (!ops.ok || !ops.data) {
      reportError(ops.ok ? 'Não foi possível listar operações.' : ops.error);
    } else {
      setOperations(ops.data.items ?? []);
    }
    if (!accounts.ok || !accounts.data) {
      reportError(accounts.ok ? 'Não foi possível listar contas mundo.' : accounts.error);
    } else {
      setWorldAccounts(accounts.data.items ?? []);
    }
    if (railsResult.ok && railsResult.data) setRails(railsResult.data.items ?? []);
    if (membersResult.ok && membersResult.data) setMembers(membersResult.data.items ?? []);
  }, [wide]);

  const load = useCallback(async () => {
    setListLoading(true);
    setListError(null);
    const result = wide ? await listCharges() : await listMyCharges();
    setListLoading(false);
    if (!result.ok || !result.data) {
      const message = result.ok ? 'Resposta inválida.' : result.error;
      setListError(message);
      reportError(message);
      return;
    }
    setItems(result.data.items ?? []);
  }, [wide]);

  useEffect(() => {
    void load();
    void loadLookups();
  }, [load, loadLookups]);

  function applyLandingDefaults(charge: Charge) {
    if (charge.netAmount != null) {
      setNetAmount(String(charge.netAmount));
    } else {
      setNetAmount(String(charge.grossAmount));
    }
    if (charge.landingWorldAccountId) {
      setLandingAccountId(charge.landingWorldAccountId);
      return;
    }
    const railExists = worldAccounts.some((item) => item.accountId === charge.emissionRailId)
      || rails.some((item) => item.railId === charge.emissionRailId);
    setLandingAccountId(railExists ? charge.emissionRailId : '');
  }

  async function select(id: string) {
    const result = wide ? await getCharge(id) : await getMyCharge(id);
    if (!result.ok || !result.data) {
      reportError(result.ok ? 'Cobrança indisponível.' : result.error);
      return;
    }
    setSelected(result.data);
    applyLandingDefaults(result.data);
  }

  async function handleCreate() {
    const opId = operationId.trim();
    if (!opId) {
      reportError('Selecione uma operação.');
      return;
    }
    if (wide && !operatorId.trim()) {
      reportError('Associe um operador à operação e escolha-o aqui.');
      return;
    }
    setBusy(true);
    const result = await createCharge({
      operationId: opId,
      operatorMemberId: operatorId.trim() || undefined,
      grossAmount: Number(amount),
    });
    setBusy(false);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Cobrança gerada.');
    await load();
    if (result.data?.chargeId) await select(result.data.chargeId);
  }

  async function handlePaid() {
    if (!selected) return;
    setBusy(true);
    const result = await markChargePaid(selected.chargeId);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Marcada como paga.');
    await load();
    await select(selected.chargeId);
  }

  async function handleMaterialize() {
    if (!selected) return;
    const landing = landingAccountId.trim();
    if (!landing) {
      reportError('Selecione onde o líquido chegou de fato.');
      return;
    }
    const net = Number(netAmount);
    if (!Number.isFinite(net) || net <= 0) {
      reportError('Informe o líquido recebido (maior que zero).');
      return;
    }
    if (net > selected.grossAmount) {
      reportError(`O líquido não pode ser maior que o bruto (${formatMoney(selected.grossAmount, selected.currency)}).`);
      return;
    }
    setBusy(true);
    const result = await materializeCharge({
      chargeId: selected.chargeId,
      netAmount: net,
      landingWorldAccountId: landing,
    });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Materializada.');
    await load();
    await select(selected.chargeId);
  }

  async function handleReverse() {
    if (!selected) return;
    setBusy(true);
    const result = await reverseCharge(selected.chargeId);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Estorno aplicado.');
    await load();
    await select(selected.chargeId);
  }

  async function handleTerminal(target: string) {
    if (!selected) return;
    setBusy(true);
    const result = await transitionCharge(selected.chargeId, target);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess(chargeStatusLabel(target));
    await load();
    await select(selected.chargeId);
  }

  function operationName(id: string): string {
    return operationsById.get(id)?.name ?? shortId(id);
  }

  function landingLabel(id: string | null): string {
    if (!id) return '—';
    const account = worldAccounts.find((item) => item.accountId === id);
    if (account) return `${account.label} · ${worldKindLabel(account.kind)}`;
    const rail = rails.find((item) => item.railId === id);
    return rail ? shortId(rail.railId) : shortId(id);
  }

  const confirmCopy = {
    paid: {
      title: 'Marcar como paga?',
      description:
        'Registra o pagamento como fato externo. Ainda não cria direitos/claims e ainda não materializa no ledger. Só continue se o dinheiro já entrou.',
      action: 'Confirmar paga',
      variant: 'default' as const,
      run: handlePaid,
    },
    materialize: {
      title: 'Materializar no ledger?',
      description: 'Cria claims a partir do snapshot de split. Esta ação não desfaz o pagamento.',
      action: 'Materializar',
      variant: 'default' as const,
      run: handleMaterialize,
    },
    reverse: {
      title: 'Estornar esta cobrança?',
      description: '',
      action: 'Estornar',
      variant: 'destructive' as const,
      run: handleReverse,
    },
    Cancelled: {
      title: 'Cancelar esta cobrança?',
      description: 'A cobrança Aberta deixa de poder ser marcada como paga. Indisponível depois de Paga.',
      action: 'Cancelar cobrança',
      variant: 'outline' as const,
      run: () => handleTerminal('Cancelled'),
    },
    Expired: {
      title: 'Expirar esta cobrança?',
      description: 'Encerra a Aberta como expirada. Indisponível depois de Paga.',
      action: 'Expirar',
      variant: 'outline' as const,
      run: () => handleTerminal('Expired'),
    },
    Failed: {
      title: 'Marcar como falhou?',
      description: 'Encerra a Aberta como falha de pagamento. Indisponível depois de Paga.',
      action: 'Confirmar falha',
      variant: 'destructive' as const,
      run: () => handleTerminal('Failed'),
    },
  };

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Cobranças"
        description={
          wide
            ? 'Paga = dinheiro entrou (fato externo). Materializada = claims no ledger. Cores diferentes de propósito.'
            : 'Suas cobranças. Split e Laranja não entram nesta visão.'
        }
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.2fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Receipt className="size-4" />
              Lista
            </CardTitle>
            <CardDescription>
              {wide
                ? 'Operação Ativa, operador associado e trilho com quota.'
                : 'Cobranças emitidas por você.'}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label>Operação</Label>
              {wide ? (
                <Select
                  value={operationId || undefined}
                  onValueChange={(value) => {
                    setOperationId(value);
                    setOperatorId('');
                  }}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Escolha a operação" />
                  </SelectTrigger>
                  <SelectContent>
                    {operations.map((op) => (
                      <SelectItem key={op.operationId} value={op.operationId}>
                        {op.name}
                        {op.status !== 'Active' ? ` (${operationStatusLabel(op.status)})` : ''}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              ) : (
                <Input
                  value={operationId}
                  onChange={(e) => setOperationId(e.target.value)}
                  placeholder="Identificador da operação"
                />
              )}
              {wide && operations.length === 0 ? (
                <p className="text-xs text-muted-foreground">Nenhuma operação listável no momento.</p>
              ) : null}
            </div>
            {wide ? (
              <div className="space-y-1.5">
                <Label>Operador</Label>
                {assignedOperators.length > 0 ? (
                  <Select value={operatorId || undefined} onValueChange={setOperatorId}>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Escolha o operador associado" />
                    </SelectTrigger>
                    <SelectContent>
                      {assignedOperators.map((id) => (
                        <SelectItem key={id} value={id}>
                          {memberName(members, id)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    {operationId
                      ? 'Esta operação não tem operadores associados. Associe um em Operações — o campo é obrigatório.'
                      : 'Escolha a operação para ver operadores associados.'}
                  </p>
                )}
              </div>
            ) : null}
            <div className="flex items-end gap-2">
              <div className="min-w-0 flex-1 space-y-1.5">
                <Label htmlFor="charge-gross">Valor bruto</Label>
                <Input id="charge-gross" value={amount} onChange={(e) => setAmount(e.target.value)} />
              </div>
              <Button type="button" disabled={busy || !canGenerate} onClick={() => void handleCreate()}>
                Gerar
              </Button>
            </div>
            {generateHint ? <p className="text-xs text-muted-foreground">{generateHint}</p> : null}
            <Separator />
            <div className="space-y-2">
              {listLoading ? (
                <>
                  <Skeleton className="h-14 w-full" />
                  <Skeleton className="h-14 w-full" />
                  <Skeleton className="h-14 w-full" />
                </>
              ) : listError ? (
                <div className="space-y-2 rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-4 text-sm">
                  <p className="text-destructive">Não foi possível carregar as cobranças.</p>
                  <p className="text-xs text-muted-foreground">{listError}</p>
                  <Button type="button" size="sm" variant="outline" onClick={() => void load()}>
                    Tentar de novo
                  </Button>
                </div>
              ) : items.length === 0 ? (
                <p className="rounded-lg border border-dashed px-3 py-6 text-center text-sm text-muted-foreground">
                  Nenhuma cobrança
                </p>
              ) : (
                items.map((item) => (
                  <button
                    key={item.chargeId}
                    type="button"
                    className={cn(
                      'w-full rounded-lg border border-border/60 px-3 py-2 text-left text-sm hover:bg-muted/40',
                      selected?.chargeId === item.chargeId && 'border-primary/40 bg-muted/30',
                    )}
                    onClick={() => void select(item.chargeId)}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="truncate font-medium">{operationName(item.operationId)}</span>
                      <Badge variant={chargeStatusVariant(item.status)}>{chargeStatusLabel(item.status)}</Badge>
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {formatMoney(item.grossAmount, item.currency)}
                      {item.status === 'Paid' ? ' · aguarda materializar' : null}
                      {item.status === 'Materialized' ? ' · claims no ledger' : null}
                    </p>
                  </button>
                ))
              )}
            </div>
          </CardContent>
        </Card>

        {selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardHeader>
              <CardTitle className="text-base">Ficha</CardTitle>
              <CardDescription>
                {operationName(selected.operationId)} · {chargeStatusLabel(selected.status)}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <dl className="grid gap-1 text-sm">
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Operação</dt>
                  <dd className="text-right">{operationName(selected.operationId)}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Operador</dt>
                  <dd className="text-right">{memberName(members, selected.operatorMemberId)}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Valor</dt>
                  <dd>{formatMoney(selected.grossAmount, selected.currency)}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted-foreground">Referência</dt>
                  <dd className="truncate text-xs text-right">
                    {selected.externalReference
                      ? selected.externalReference.startsWith('noop-')
                        ? `Teste · ${selected.externalReference}`
                        : selected.externalReference
                      : '—'}
                  </dd>
                </div>
                {wide ? (
                  <div className="flex justify-between gap-3">
                    <dt className="text-muted-foreground">Trilho</dt>
                    <dd className="text-right text-xs">{landingLabel(selected.emissionRailId)}</dd>
                  </div>
                ) : null}
                {wide && selected.orangeMemberId ? (
                  <div className="flex justify-between gap-3">
                    <dt className="text-muted-foreground">Laranja</dt>
                    <dd className="text-right">{memberName(members, selected.orangeMemberId)}</dd>
                  </div>
                ) : null}
                {wide && selected.landingWorldAccountId ? (
                  <div className="flex justify-between gap-3">
                    <dt className="text-muted-foreground">Aterrissagem</dt>
                    <dd className="text-right text-xs">{landingLabel(selected.landingWorldAccountId)}</dd>
                  </div>
                ) : null}
              </dl>
              {wide && selected.splitIntent ? (
                <>
                  <Separator />
                  <h3 className="text-sm font-medium">Split (estimativa sobre o bruto)</h3>
                  <ul className="space-y-1 text-xs">
                    {estimatedSplit(selected.grossAmount, selected.splitIntent.lines).map((line) => (
                      <li key={line.order}>
                        {line.order}. {splitKindLabel(line.kind)} · {line.percent}% da base restante ·{' '}
                        {formatMoney(line.amount, selected.currency)}
                      </li>
                    ))}
                  </ul>
                </>
              ) : null}

              {wide && selected.status === 'Open' ? (
                <div className="space-y-2">
                  <div className="flex flex-wrap gap-2">
                    <Button type="button" size="sm" disabled={busy} onClick={() => setConfirm('paid')}>
                      Marcar paga
                    </Button>
                    <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => setConfirm('Cancelled')}>
                      Cancelar
                    </Button>
                    <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => setConfirm('Expired')}>
                      Expirar
                    </Button>
                    <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => setConfirm('Failed')}>
                      Falhou
                    </Button>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Cancelar, expirar e falhou só existem enquanto a cobrança está Aberta. Depois de Paga esse trio some.
                  </p>
                </div>
              ) : null}

              {wide && selected.status === 'Paid' ? (
                <div className="space-y-2">
                  <Separator />
                  <p className="text-sm font-medium">Materializar</p>
                  <p className="text-xs text-muted-foreground">
                    Paga ainda não criou direitos. Cancelar / Expirar / Falhou ficam indisponíveis depois de Paga.
                  </p>
                  <div className="space-y-1.5">
                    <Label htmlFor="charge-net">Líquido</Label>
                    <Input id="charge-net" value={netAmount} onChange={(e) => setNetAmount(e.target.value)} />
                    <p className="text-xs text-muted-foreground">
                      Bruto {formatMoney(selected.grossAmount, selected.currency)}. O líquido é o que chegou de fato (≤ bruto,
                      depois de taxas).
                    </p>
                  </div>
                  <div className="space-y-1.5">
                    <Label>Conta de aterrissagem</Label>
                    <Select value={landingAccountId || undefined} onValueChange={setLandingAccountId}>
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Onde o líquido chegou de fato" />
                      </SelectTrigger>
                      <SelectContent>
                        {worldAccounts.map((account) => (
                          <SelectItem key={account.accountId} value={account.accountId}>
                            {account.label} · {worldKindLabel(account.kind)}
                            {account.accountId === selected.emissionRailId ? ' · trilho desta cobrança' : ''}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <Button
                    type="button"
                    size="sm"
                    disabled={busy || !landingAccountId.trim()}
                    onClick={() => setConfirm('materialize')}
                  >
                    Materializar
                  </Button>
                </div>
              ) : null}

              {wide && (selected.status === 'Paid' || selected.status === 'Materialized') ? (
                <div className="pt-1">
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    disabled={busy}
                    onClick={() => {
                      setReverseCause('estorno');
                      setConfirm('reverse');
                    }}
                  >
                    Estornar
                  </Button>
                </div>
              ) : null}
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden border-dashed lg:flex">
            <CardContent className="flex flex-1 items-center justify-center p-8 text-sm text-muted-foreground">
              Selecione uma cobrança na lista.
            </CardContent>
          </Card>
        )}
      </div>

      <Dialog open={confirm !== null} onOpenChange={(open) => !open && setConfirm(null)}>
        <DialogContent showCloseButton={!busy}>
          {confirm ? (
            <>
              <DialogHeader>
                <DialogTitle>{confirmCopy[confirm].title}</DialogTitle>
                <DialogDescription>
                  {confirm === 'reverse' && selected
                    ? `Estorna ${formatMoney(selected.netAmount ?? selected.grossAmount, selected.currency)} desta cobrança. Reverte os claims no ledger e o saldo no livro-mundo.`
                    : confirmCopy[confirm].description}
                </DialogDescription>
              </DialogHeader>
              {confirm === 'reverse' ? (
                <div className="space-y-1.5">
                  <Label>Causa (glossário)</Label>
                  <Select value={reverseCause} onValueChange={setReverseCause}>
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {REVERSE_CAUSES.map((cause) => (
                        <SelectItem key={cause.value} value={cause.value}>
                          {cause.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <p className="text-xs text-muted-foreground">
                    No livro, o estorno de cobrança registra a causa canónica <span className="font-medium">estorno</span>.
                  </p>
                </div>
              ) : null}
              <DialogFooter>
                <Button type="button" variant="outline" disabled={busy} onClick={() => setConfirm(null)}>
                  Voltar
                </Button>
                <Button
                  type="button"
                  variant={confirmCopy[confirm].variant}
                  disabled={busy}
                  onClick={() => void confirmCopy[confirm].run()}
                >
                  {confirmCopy[confirm].action}
                </Button>
              </DialogFooter>
            </>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  );
}
