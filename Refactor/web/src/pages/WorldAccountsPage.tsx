import { useCallback, useEffect, useMemo, useState } from 'react';
import { Wallet } from 'lucide-react';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import {
  listExposure,
  markAccountLost,
  reconcileAccount,
  type ExposureLine,
} from '@/api/administrator/ledger';
import { getMemberMandate } from '@/api/administrator/mandates';
import {
  configureWorldAccount,
  labelWorldAccount,
  listWorldAccountTransactions,
  listWorldAccounts,
  openWorldAccount,
  recordWorldAccountObservation,
  type WorldAccount,
  type WorldAccountTransaction,
} from '@/api/administrator/worldAccounts';
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { cn } from '@/lib/utils';
import { reportError, reportSuccess } from '@/feedback';

const KINDS = [
  { value: 'Gateway', label: 'Gateway' },
  { value: 'Bank', label: 'Banco' },
  { value: 'Crypto', label: 'Crypto' },
  { value: 'Payout', label: 'Pagamento' },
] as const;

const CURRENCIES = ['BRL', 'USD', 'USDT'] as const;

const ATTRITION_CAUSES = [
  { value: 'bloqueio_bancario', label: 'Bloqueio bancário' },
  { value: 'apreensao', label: 'Apreensão' },
  { value: 'traicao', label: 'Traição' },
  { value: 'saida_voluntaria', label: 'Saída voluntária' },
  { value: 'erro_operacional', label: 'Erro operacional' },
  { value: 'estorno', label: 'Estorno' },
  { value: 'desconhecido', label: 'Desconhecido' },
] as const;

type ConfirmKind = 'lost' | 'reconcile' | 'block-emission' | 'freeze';

function formatMap(map: Record<string, number> | undefined): string {
  const entries = Object.entries(map ?? {});
  if (entries.length === 0) return '—';
  return entries.map(([currency, amount]) => `${currency} ${amount}`).join(' · ');
}

function kindLabel(kind: string): string {
  return KINDS.find((item) => item.value === kind)?.label ?? kind;
}

function causeLabel(cause: string): string {
  return ATTRITION_CAUSES.find((item) => item.value === cause)?.label ?? cause;
}

function emissionLabel(status: string): string {
  if (status === 'Blocked') return 'Bloqueada';
  if (status === 'Ok') return 'Ok';
  return status;
}

function balanceLabel(status: string): string {
  if (status === 'Frozen') return 'Congelado';
  if (status === 'Lost') return 'Perdido';
  if (status === 'Accessible') return 'Acessível';
  return status;
}

function isGateway(account: WorldAccount | null): boolean {
  return account?.kind === 'Gateway';
}

function isLost(account: WorldAccount | null): boolean {
  return account?.balanceStatus === 'Lost';
}

function productError(message: string): string {
  const text = message.trim();
  if (/atuar como Laranja|precisa existir|OrangeNotEligible|Laranja inválido/i.test(text)) {
    return 'Escolha um membro que atua como Laranja. Um login comum não abre Gateway.';
  }
  if (/Sem claims ativos/i.test(text)) {
    return 'Não há direitos (claims) nesta conta para ratear a diferença. Ajuste o caixa com Crédito ou Débito.';
  }
  if (/Invariante|soma claims/i.test(text)) {
    return 'A reconciliação só fecha quando os direitos nesta conta batem com o saldo. Sem claims, use Crédito ou Débito.';
  }
  if (/Eixo de emiss/i.test(text)) {
    return 'Emissão só existe em Gateway.';
  }
  return text;
}

function isOrangeMandate(presets: string[], grants: Array<{ capability: string }>): boolean {
  return (
    presets.some((preset) => preset.toLowerCase() === 'orange') ||
    grants.some((grant) => grant.capability === 'atuar_como_laranja')
  );
}

function transactionKindLabel(kind: string): string {
  const key = kind.toLowerCase();
  if (key === 'credit' || key === 'observedcredited') return 'Crédito';
  if (key === 'debit' || key === 'observeddebited') return 'Débito';
  if (key.includes('write') || key.includes('quota')) return kind;
  return kind;
}

function transactionMemoLabel(memo: string | null): string {
  if (!memo) return '';
  const writeOff = /^write-off:(.+)$/i.exec(memo);
  if (writeOff) return `Baixa por perda · ${causeLabel(writeOff[1])}`;
  const reconcile = /^reconcile:(.+)$/i.exec(memo);
  if (reconcile) return `Reconciliação · ${causeLabel(reconcile[1])}`;
  return memo;
}

export function WorldAccountsPage() {
  const [items, setItems] = useState<WorldAccount[]>([]);
  const [selected, setSelected] = useState<WorldAccount | null>(null);
  const [transactions, setTransactions] = useState<WorldAccountTransaction[]>([]);
  const [oranges, setOranges] = useState<AccountDetails[]>([]);
  const [busy, setBusy] = useState(false);
  const [listLoading, setListLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [kind, setKind] = useState<(typeof KINDS)[number]['value']>('Gateway');
  const [label, setLabel] = useState('');
  const [orangeId, setOrangeId] = useState('');
  const [cut, setCut] = useState('10');
  const [openQuotaCurrency, setOpenQuotaCurrency] = useState<(typeof CURRENCIES)[number]>('BRL');
  const [openQuotaRemaining, setOpenQuotaRemaining] = useState('10000');
  const [quotaCurrency, setQuotaCurrency] = useState<(typeof CURRENCIES)[number]>('BRL');
  const [quotaRemaining, setQuotaRemaining] = useState('');
  const [obsCurrency, setObsCurrency] = useState<(typeof CURRENCIES)[number]>('BRL');
  const [obsAmount, setObsAmount] = useState('100');
  const [obsMemo, setObsMemo] = useState('');
  const [lostMemo, setLostMemo] = useState('');
  const [rename, setRename] = useState('');
  const [cause, setCause] = useState<(typeof ATTRITION_CAUSES)[number]['value']>('bloqueio_bancario');
  const [observed, setObserved] = useState('90');
  const [exposure, setExposure] = useState<ExposureLine[]>([]);
  const [confirm, setConfirm] = useState<ConfirmKind | null>(null);

  const labelsById = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of items) map.set(item.accountId, item.label);
    return map;
  }, [items]);

  const load = useCallback(async () => {
    setListLoading(true);
    const result = await listWorldAccounts();
    if (!result.ok || !result.data) {
      const message = productError(result.ok ? 'Resposta inválida.' : result.error);
      setListError(message);
      setItems([]);
      setListLoading(false);
      reportError(message);
      return;
    }
    setListError(null);
    setItems(result.data.items ?? []);
    setListLoading(false);
    const exposed = await listExposure();
    if (!exposed.ok) {
      reportError(productError(exposed.error));
      setExposure([]);
    } else {
      setExposure(exposed.data?.items ?? []);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    void searchAdministratorAccounts({ limit: 100, offset: 0 }).then(async (result) => {
      if (!result.ok || !result.data) {
        reportError(result.ok ? 'Não foi possível listar membros.' : productError(result.error));
        return;
      }
      const members = result.data.items ?? [];
      const eligible: AccountDetails[] = [];
      await Promise.all(
        members.map(async (member) => {
          const mandate = await getMemberMandate(member.id);
          if (!mandate.ok || !mandate.data) return;
          if (isOrangeMandate(mandate.data.appliedPresets ?? [], mandate.data.grants ?? [])) {
            eligible.push(member);
          }
        }),
      );
      eligible.sort((a, b) => a.username.localeCompare(b.username, 'pt-BR'));
      setOranges(eligible);
      setOrangeId((current) => (eligible.some((item) => item.id === current) ? current : ''));
    });
  }, []);

  async function select(accountId: string) {
    const listed = await listWorldAccounts();
    if (!listed.ok || !listed.data) {
      reportError(listed.ok ? 'Livro-mundo indisponível.' : productError(listed.error));
      return;
    }
    const current = listed.data.items?.find((item) => item.accountId === accountId);
    if (!current) {
      reportError('Livro-mundo indisponível.');
      return;
    }
    setSelected(current);
    setRename(current.label);
    const quotaEntries = Object.entries(current.quotas ?? {});
    if (quotaEntries.length > 0) {
      const [currency, remaining] = quotaEntries[0];
      if (CURRENCIES.includes(currency as (typeof CURRENCIES)[number])) {
        setQuotaCurrency(currency as (typeof CURRENCIES)[number]);
      }
      setQuotaRemaining(String(remaining));
    } else {
      setQuotaRemaining('');
    }
    const tx = await listWorldAccountTransactions(accountId);
    if (!tx.ok || !tx.data) {
      reportError(tx.ok ? 'Não foi possível listar transações.' : productError(tx.error));
      setTransactions([]);
      return;
    }
    setTransactions(tx.data.items ?? []);
  }

  const canOpen =
    Boolean(label.trim()) && (kind !== 'Gateway' || Boolean(orangeId));

  async function handleOpen() {
    if (kind === 'Gateway' && !orangeId) {
      reportError('Escolha um membro que atua como Laranja. Um login comum não abre Gateway.');
      return;
    }
    setBusy(true);
    const result = await openWorldAccount({
      kind,
      label: label.trim(),
      orangeMemberId: kind === 'Gateway' ? orangeId.trim() || undefined : undefined,
      level1CutPercent: kind === 'Gateway' ? Number(cut) : undefined,
      quotaCurrency: openQuotaCurrency,
      quotaRemaining: openQuotaRemaining.trim() === '' ? undefined : Number(openQuotaRemaining),
    });
    setBusy(false);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Livro-mundo aberto.');
    setLabel('');
    await load();
    if (result.data?.accountId) await select(result.data.accountId);
  }

  async function handleObserve(direction: 'credit' | 'debit') {
    if (!selected || isLost(selected)) return;
    setBusy(true);
    const result = await recordWorldAccountObservation(selected.accountId, {
      direction,
      currency: obsCurrency,
      amount: Number(obsAmount),
      memo: obsMemo.trim() || undefined,
    });
    setBusy(false);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess(direction === 'credit' ? 'Crédito observado.' : 'Débito observado.');
    await load();
    await select(selected.accountId);
  }

  async function handleQuota() {
    if (!selected) return;
    setBusy(true);
    const result = await configureWorldAccount(selected.accountId, {
      quotaCurrency,
      quotaRemaining: Number(quotaRemaining),
    });
    setBusy(false);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Quota restante atualizada.');
    await load();
    await select(selected.accountId);
  }

  async function handleFreeze() {
    if (!selected || isLost(selected)) return;
    setBusy(true);
    const result = await configureWorldAccount(selected.accountId, { balanceStatus: 'Frozen' });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Saldo congelado.');
    await load();
    await select(selected.accountId);
  }

  async function handleUnfreeze() {
    if (!selected || isLost(selected)) return;
    setBusy(true);
    const result = await configureWorldAccount(selected.accountId, { balanceStatus: 'Accessible' });
    setBusy(false);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Saldo acessível.');
    await load();
    await select(selected.accountId);
  }

  async function handleEmissionOk() {
    if (!selected || !isGateway(selected) || isLost(selected)) return;
    setBusy(true);
    const result = await configureWorldAccount(selected.accountId, { emissionStatus: 'Ok' });
    setBusy(false);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Emissão liberada.');
    await load();
    await select(selected.accountId);
  }

  async function handleBlockEmission() {
    if (!selected || !isGateway(selected) || isLost(selected)) return;
    setBusy(true);
    const result = await configureWorldAccount(selected.accountId, { emissionStatus: 'Blocked' });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Emissão bloqueada.');
    await load();
    await select(selected.accountId);
  }

  async function handleLabel() {
    if (!selected) return;
    setBusy(true);
    const result = await labelWorldAccount(selected.accountId, rename.trim());
    setBusy(false);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Rótulo atualizado.');
    await load();
    await select(selected.accountId);
  }

  async function handleLost() {
    if (!selected || isLost(selected)) return;
    setBusy(true);
    const result = await markAccountLost(selected.accountId, cause);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Caixa marcada como perdida.');
    await load();
    await select(selected.accountId);
  }

  async function handleReconcile() {
    if (!selected || isLost(selected)) return;
    setBusy(true);
    const result = await reconcileAccount({
      accountId: selected.accountId,
      currency: obsCurrency,
      observedBalance: Number(observed),
      cause,
    });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(productError(result.error));
      return;
    }
    reportSuccess('Reconciliação aplicada.');
    await load();
    await select(selected.accountId);
  }

  const lost = isLost(selected);
  const gateway = isGateway(selected);

  const confirmCopy: Record<ConfirmKind, { title: string; description: string; action: string; run: () => void }> = {
    lost: {
      title: 'Marcar caixa como perdida',
      description: `A caixa «${selected?.label ?? 'esta conta'}» deixa de se mover. Causa: ${causeLabel(cause)}.${
        lostMemo.trim() ? ` Nota: ${lostMemo.trim()}` : ''
      } Esta ação não se desfaz daqui.`,
      action: 'Marcar perdida',
      run: () => void handleLost(),
    },
    reconcile: {
      title: 'Reconciliar saldo',
      description:
        'Reconciliação rateia a diferença nos direitos (claims) desta conta. Sem claims, use Crédito ou Débito na observação — o número sozinho não altera o caixa.',
      action: 'Reconciliar',
      run: () => void handleReconcile(),
    },
    'block-emission': {
      title: 'Bloquear emissão',
      description: `Impedir novas emissões em ${selected?.label ?? 'este Gateway'} até reabrir.`,
      action: 'Bloquear',
      run: () => void handleBlockEmission(),
    },
    freeze: {
      title: 'Congelar saldo',
      description: `Congelar ${selected?.label ?? 'esta conta'} trava hops e entra em exposição. Confirme para continuar.`,
      action: 'Congelar',
      run: () => void handleFreeze(),
    },
  };

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Livro-mundo"
        description="Contas do livro-mundo: Gateway, banco, crypto e pagamento. Saldo só muda por observação; quota é o teto restante neste ciclo."
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.2fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Wallet className="size-4" />
              Contas do livro-mundo
            </CardTitle>
            <CardDescription>
              Abrir um livro-mundo. Login de membro não é Conta — Conta aqui é o caixa no mundo.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-1">
                <Label>Tipo</Label>
                <Select value={kind} onValueChange={(value) => setKind(value as typeof kind)}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {KINDS.map((item) => (
                      <SelectItem key={item.value} value={item.value}>
                        {item.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label>Rótulo</Label>
                <Input value={label} onChange={(e) => setLabel(e.target.value)} />
              </div>
              {kind === 'Gateway' ? (
                <>
                  <div className="space-y-1 sm:col-span-2">
                    <Label>Laranja</Label>
                    {oranges.length > 0 ? (
                      <Select value={orangeId || undefined} onValueChange={setOrangeId}>
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="Escolher membro Laranja" />
                        </SelectTrigger>
                        <SelectContent>
                          {oranges.map((member) => (
                            <SelectItem key={member.id} value={member.id}>
                              {member.username}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    ) : (
                      <p className="text-sm text-muted-foreground">
                        Nenhum membro atua como Laranja. Conceda o preset Laranja antes de abrir Gateway.
                      </p>
                    )}
                  </div>
                  <div className="space-y-1">
                    <Label>Corte nível 1 (%)</Label>
                    <Input value={cut} onChange={(e) => setCut(e.target.value)} />
                  </div>
                </>
              ) : null}
              <div className="space-y-1">
                <Label>Moeda da quota</Label>
                <Select
                  value={openQuotaCurrency}
                  onValueChange={(value) => setOpenQuotaCurrency(value as typeof openQuotaCurrency)}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CURRENCIES.map((currency) => (
                      <SelectItem key={currency} value={currency}>
                        {currency}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label>Quota restante neste ciclo</Label>
                <Input value={openQuotaRemaining} onChange={(e) => setOpenQuotaRemaining(e.target.value)} />
              </div>
            </div>
            <Button type="button" disabled={busy || !canOpen} onClick={() => void handleOpen()}>
              Abrir
            </Button>
            <Separator />
            {listLoading ? (
              <p className="text-sm text-muted-foreground">Carregando contas do livro-mundo…</p>
            ) : listError ? (
              <div className="space-y-2 rounded-lg border border-destructive/40 bg-destructive/5 px-3 py-2">
                <p className="text-sm text-destructive">{listError}</p>
                <Button type="button" size="sm" variant="outline" onClick={() => void load()}>
                  Tentar de novo
                </Button>
              </div>
            ) : items.length === 0 ? (
              <p className="text-sm text-muted-foreground">Nenhuma conta no livro-mundo.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Rótulo</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead>Emissão</TableHead>
                    <TableHead>Saldo</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((item) => (
                    <TableRow
                      key={item.accountId}
                      className={cn('cursor-pointer', selected?.accountId === item.accountId && 'bg-muted/40')}
                      onClick={() => void select(item.accountId)}
                    >
                      <TableCell>
                        <div className="font-medium">{item.label}</div>
                        <div className="text-xs text-muted-foreground">{formatMap(item.balances)}</div>
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{kindLabel(item.kind)}</Badge>
                      </TableCell>
                      <TableCell>
                        {item.kind === 'Gateway' && item.balanceStatus !== 'Lost'
                          ? emissionLabel(item.emissionStatus)
                          : '—'}
                      </TableCell>
                      <TableCell>{balanceLabel(item.balanceStatus)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        {selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardHeader>
              <CardTitle className="text-base">{selected.label}</CardTitle>
              <CardDescription>
                {kindLabel(selected.kind)}
                {gateway && !lost ? ` · emissão ${emissionLabel(selected.emissionStatus)}` : ''}
                {' · '}
                saldo {balanceLabel(selected.balanceStatus)}
                {lost ? ' — caixa perdida, sem emissão.' : ''}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <Input value={rename} onChange={(e) => setRename(e.target.value)} placeholder="Rótulo" />
                <Button type="button" size="sm" disabled={busy || !rename.trim()} onClick={() => void handleLabel()}>
                  Renomear
                </Button>
              </div>
              <p className="text-sm">
                Quota restante {formatMap(selected.quotas)} · saldos observados {formatMap(selected.balances)}
              </p>
              <div className="grid gap-3 sm:grid-cols-2">
                <section className="space-y-2">
                  <h3 className="text-sm font-medium">Quota restante neste ciclo</h3>
                  <Select value={quotaCurrency} onValueChange={(value) => setQuotaCurrency(value as typeof quotaCurrency)}>
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {CURRENCIES.map((currency) => (
                        <SelectItem key={currency} value={currency}>
                          {currency}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <div className="flex gap-2">
                    <Input value={quotaRemaining} onChange={(e) => setQuotaRemaining(e.target.value)} />
                    <Button type="button" size="sm" disabled={busy} onClick={() => void handleQuota()}>
                      Salvar
                    </Button>
                  </div>
                </section>
                <section className="space-y-2">
                  <h3 className="text-sm font-medium">Observação (crédito / débito)</h3>
                  <Select
                    value={obsCurrency}
                    onValueChange={(value) => setObsCurrency(value as typeof obsCurrency)}
                    disabled={lost}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {CURRENCIES.map((currency) => (
                        <SelectItem key={currency} value={currency}>
                          {currency}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Input
                    value={obsAmount}
                    onChange={(e) => setObsAmount(e.target.value)}
                    placeholder="Montante"
                    disabled={lost}
                  />
                  <Input
                    value={obsMemo}
                    onChange={(e) => setObsMemo(e.target.value)}
                    placeholder="Memo da observação"
                    disabled={lost}
                  />
                  <div className="flex gap-2">
                    <Button type="button" size="sm" disabled={busy || lost} onClick={() => void handleObserve('credit')}>
                      Crédito
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={busy || lost}
                      onClick={() => void handleObserve('debit')}
                    >
                      Débito
                    </Button>
                  </div>
                </section>
              </div>
              <Separator />
              <section className="space-y-2">
                <h3 className="text-sm font-medium">{gateway && !lost ? 'Emissão e saldo' : 'Saldo'}</h3>
                <div className="flex flex-wrap gap-2">
                  {gateway && !lost ? (
                    <>
                      <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleEmissionOk()}>
                        Liberar emissão
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        disabled={busy}
                        onClick={() => setConfirm('block-emission')}
                      >
                        Bloquear emissão
                      </Button>
                    </>
                  ) : null}
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={busy || lost}
                    onClick={() => setConfirm('freeze')}
                  >
                    Congelar saldo
                  </Button>
                  <Button type="button" size="sm" variant="outline" disabled={busy || lost} onClick={() => void handleUnfreeze()}>
                    Descongelar
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    disabled={busy || lost}
                    onClick={() => setConfirm('lost')}
                  >
                    Perdido
                  </Button>
                </div>
                {lost ? (
                  <p className="text-sm text-muted-foreground">
                    Esta caixa já está perdida. Perdido, observação e congelar ficam encerrados.
                  </p>
                ) : (
                  <div className="grid gap-2 sm:grid-cols-2">
                    <div className="space-y-1">
                      <Label>Causa da perda</Label>
                      <Select value={cause} onValueChange={(value) => setCause(value as typeof cause)}>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {ATTRITION_CAUSES.map((item) => (
                            <SelectItem key={item.value} value={item.value}>
                              {item.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <Input
                        value={lostMemo}
                        onChange={(e) => setLostMemo(e.target.value)}
                        placeholder="Nota da perda (só neste diálogo)"
                      />
                    </div>
                    <div className="space-y-1">
                      <Label>Saldo observado (reconciliação)</Label>
                      <p className="text-xs text-muted-foreground">
                        Só rateia direitos (claims). Sem claims, use Crédito ou Débito.
                      </p>
                      <div className="flex gap-2">
                        <Input value={observed} onChange={(e) => setObserved(e.target.value)} />
                        <Button type="button" size="sm" disabled={busy} onClick={() => setConfirm('reconcile')}>
                          Reconciliar
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </section>
              <section className="space-y-1">
                <h3 className="text-sm font-medium">Exposição (direitos presos)</h3>
                {exposure.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    Nenhuma exposição registrada. Exposição são direitos (claims) presos em caixa congelada ou
                    perdida — o saldo observado sozinho não entra aqui.
                  </p>
                ) : (
                  exposure.map((line) => (
                    <p key={`${line.accountId}-${line.currency}`} className="text-sm text-muted-foreground">
                      {labelsById.get(line.accountId) ?? line.accountId.slice(0, 8)} · {line.currency} {line.amount} ·{' '}
                      {balanceLabel(line.balanceStatus)}
                    </p>
                  ))
                )}
              </section>
              <Separator />
              <div className="space-y-1">
                {transactions.length === 0 ? (
                  <p className="text-sm text-muted-foreground">Nenhuma transação.</p>
                ) : (
                  transactions.map((tx, index) => {
                    const memo = transactionMemoLabel(tx.memo);
                    return (
                      <p key={`${tx.occurredAt}-${index}`} className="text-xs text-muted-foreground">
                        {transactionKindLabel(tx.kind)} {tx.currency} {tx.amount}
                        {memo ? ` · ${memo}` : ''}
                      </p>
                    );
                  })
                )}
              </div>
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden border-dashed border-border/60 bg-card/50 lg:flex">
            <CardContent className="flex flex-1 items-center justify-center p-8 text-sm text-muted-foreground">
              Selecione uma conta do livro-mundo.
            </CardContent>
          </Card>
        )}
      </div>

      <Dialog
        open={confirm !== null}
        onOpenChange={(open) => {
          if (!open) setConfirm(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{confirm ? confirmCopy[confirm].title : ''}</DialogTitle>
            <DialogDescription>{confirm ? confirmCopy[confirm].description : ''}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirm(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant={confirm === 'lost' || confirm === 'block-emission' ? 'destructive' : 'default'}
              disabled={busy}
              onClick={() => confirm && confirmCopy[confirm].run()}
            >
              {confirm ? confirmCopy[confirm].action : 'Confirmar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
