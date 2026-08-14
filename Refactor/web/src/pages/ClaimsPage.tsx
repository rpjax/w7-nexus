import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDown, Plus, ScrollText, Trash2 } from 'lucide-react';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import { listCharges, type Charge } from '@/api/administrator/charging';
import {
  getClaim,
  listClaims,
  listHops,
  registerHop,
  repassClaims,
  revealClaim,
  reverseCharge,
  type LedgerClaim,
  type LedgerHop,
} from '@/api/administrator/ledger';
import { listWorldAccounts, type WorldAccount } from '@/api/administrator/worldAccounts';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
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
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { reportError, reportSuccess } from '@/feedback';

const NONE = '__none__';
const CURRENCIES = ['BRL', 'USD', 'EUR'] as const;
const EMPTY_DESTINATION = { accountId: '', amount: '', currency: 'BRL' };

const ATTRITION_CAUSES = [
  { value: 'bloqueio_bancario', label: 'Bloqueio bancário' },
  { value: 'apreensao', label: 'Apreensão' },
  { value: 'traicao', label: 'Traição' },
  { value: 'saida_voluntaria', label: 'Saída voluntária' },
  { value: 'erro_operacional', label: 'Erro operacional' },
  { value: 'estorno', label: 'Estorno' },
  { value: 'desconhecido', label: 'Desconhecido' },
] as const;

const WORLD_KIND_LABEL: Record<string, string> = {
  Gateway: 'Gateway',
  Bank: 'Banco',
  Crypto: 'Crypto',
  Payout: 'Payout',
};

const CHARGE_STATUS_LABEL: Record<string, string> = {
  Open: 'Aberta',
  Paid: 'Paga',
  Materialized: 'Materializada',
  Cancelled: 'Cancelada',
  Expired: 'Expirada',
  Failed: 'Falhou',
  Reversed: 'Estornada',
};

const CLAIM_STATUS_LABEL: Record<string, string> = {
  Active: 'Ativo',
  Repassed: 'Repassado',
  Lost: 'Perdido',
  Reversed: 'Estornado',
  Archived: 'Arquivado',
};

const KIND_LABEL: Record<string, string> = {
  ResidualOrg: 'Residual da organização',
  PathCut: 'Cut proporcional',
  Orange: 'Laranja',
  Shareholders: 'Acionistas',
  OperationManagement: 'Gestão da operação',
  Agency: 'Agência',
};

type ConfirmKind = 'hop' | 'repass' | 'reveal' | 'reverse';

function shortId(id: string) {
  if (!id) return '—';
  return id.length > 8 ? `${id.slice(0, 8)}…` : id;
}

function worldKindLabel(kind: string) {
  return WORLD_KIND_LABEL[kind] ?? kind;
}

function worldLabel(accounts: WorldAccount[], id: string) {
  const found = accounts.find((item) => item.accountId === id);
  return found ? `${found.label} · ${worldKindLabel(found.kind)}` : shortId(id);
}

function memberLabel(members: { id: string; username: string }[], id: string) {
  const found = members.find((item) => item.id === id);
  return found ? found.username : shortId(id);
}

function claimStatusLabel(status: string) {
  return CLAIM_STATUS_LABEL[status] ?? status;
}

function chargeStatusLabel(status: string) {
  return CHARGE_STATUS_LABEL[status] ?? status;
}

function kindLabel(kind: string) {
  return KIND_LABEL[kind] ?? kind;
}

function chargeOptionLabel(item: Charge | undefined, fallbackId?: string) {
  if (!item) return fallbackId ? shortId(fallbackId) : '—';
  return `${chargeStatusLabel(item.status)} · ${item.grossAmount} ${item.currency}`;
}

export function ClaimsPage() {
  const [items, setItems] = useState<LedgerClaim[]>([]);
  const [hops, setHops] = useState<LedgerHop[]>([]);
  const [worldAccounts, setWorldAccounts] = useState<WorldAccount[]>([]);
  const [charges, setCharges] = useState<Charge[]>([]);
  const [members, setMembers] = useState<{ id: string; username: string }[]>([]);
  const [chargeId, setChargeId] = useState('');
  const [accountId, setAccountId] = useState('');
  const [beneficiaryId, setBeneficiaryId] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [originAccountId, setOriginAccountId] = useState('');
  const [currency, setCurrency] = useState('BRL');
  const [destinations, setDestinations] = useState([{ ...EMPTY_DESTINATION }]);
  const [cutOpen, setCutOpen] = useState(false);
  const [cutPercent, setCutPercent] = useState('');
  const [cutOrangeId, setCutOrangeId] = useState('');
  const [cutOrangeAccountId, setCutOrangeAccountId] = useState('');
  const [cutInPlace, setCutInPlace] = useState(false);
  const [keepRemainder, setKeepRemainder] = useState(true);
  const [lossCause, setLossCause] = useState('');
  const [payoutAccountId, setPayoutAccountId] = useState('');
  const [repassOriginId, setRepassOriginId] = useState('');
  const [revealSummary, setRevealSummary] = useState('');
  const [reverseChargeId, setReverseChargeId] = useState('');
  const [reverseCause, setReverseCause] = useState('estorno');
  const [ficha, setFicha] = useState<LedgerClaim | null>(null);
  const [hopFieldError, setHopFieldError] = useState('');
  const [busy, setBusy] = useState(false);
  const [loadingClaims, setLoadingClaims] = useState(true);
  const [loadingCatalog, setLoadingCatalog] = useState(true);
  const [confirm, setConfirm] = useState<ConfirmKind | null>(null);

  const loadCatalog = useCallback(async () => {
    setLoadingCatalog(true);
    const [worldResult, chargeResult, memberResult] = await Promise.all([
      listWorldAccounts(),
      listCharges(),
      searchAdministratorAccounts({ limit: 100, offset: 0 }),
    ]);
    if (!worldResult.ok || !worldResult.data) {
      reportError(worldResult.ok ? 'Não foi possível listar contas do livro-mundo.' : worldResult.error);
    } else {
      setWorldAccounts(worldResult.data.items ?? []);
    }
    if (!chargeResult.ok || !chargeResult.data) {
      reportError(chargeResult.ok ? 'Não foi possível listar cobranças.' : chargeResult.error);
    } else {
      setCharges(chargeResult.data.items ?? []);
    }
    if (!memberResult.ok || !memberResult.data) {
      reportError(memberResult.ok ? 'Não foi possível listar membros.' : memberResult.error);
    } else {
      setMembers((memberResult.data.items ?? []).map((item) => ({ id: item.id, username: item.username })));
    }
    setLoadingCatalog(false);
  }, []);

  const loadClaims = useCallback(async () => {
    setLoadingClaims(true);
    const result = await listClaims({
      chargeId: chargeId || undefined,
      accountId: accountId || undefined,
      beneficiaryId: beneficiaryId || undefined,
    });
    if (!result.ok || !result.data) {
      reportError(result.ok ? 'Resposta inválida.' : result.error);
      setItems([]);
      setLoadingClaims(false);
      return;
    }
    setItems(result.data.items ?? []);
    setLoadingClaims(false);
  }, [accountId, beneficiaryId, chargeId]);

  const loadHops = useCallback(async () => {
    const hopResult = await listHops(originAccountId || accountId || undefined);
    if (!hopResult.ok || !hopResult.data) {
      reportError(hopResult.ok ? 'Não foi possível listar hops.' : hopResult.error);
      setHops([]);
      return;
    }
    setHops(hopResult.data.items ?? []);
  }, [accountId, originAccountId]);

  useEffect(() => {
    void loadCatalog();
  }, [loadCatalog]);

  useEffect(() => {
    void loadClaims();
  }, [loadClaims]);

  useEffect(() => {
    void loadHops();
  }, [loadHops]);

  const visibleItems = useMemo(
    () => (statusFilter ? items.filter((item) => item.status === statusFilter) : items),
    [items, statusFilter],
  );

  const selectedCount = selected.length;
  const selectedClaims = useMemo(
    () => items.filter((item) => selected.includes(item.claimId)),
    [items, selected],
  );

  const bundleClaims = useMemo(() => {
    if (selectedClaims.length > 0) {
      return selectedClaims.filter(
        (item) =>
          item.status === 'Active' &&
          (!originAccountId || item.locationAccountId === originAccountId) &&
          item.currency === currency,
      );
    }
    return items.filter(
      (item) =>
        item.status === 'Active' &&
        item.locationAccountId === originAccountId &&
        item.currency === currency,
    );
  }, [currency, items, originAccountId, selectedClaims]);

  const bundleTotal = useMemo(
    () => bundleClaims.reduce((sum, item) => sum + item.amount, 0),
    [bundleClaims],
  );

  const cutAmount = useMemo(() => {
    const percent = Number(cutPercent);
    if (!cutPercent.trim() || Number.isNaN(percent) || percent <= 0) return 0;
    return Math.round(bundleTotal * percent) / 100;
  }, [bundleTotal, cutPercent]);

  const afterCut = Math.max(0, bundleTotal - cutAmount);

  const destRows = useMemo(
    () => destinations.filter((row) => row.accountId.trim()),
    [destinations],
  );

  const destSum = useMemo(
    () => destRows.reduce((sum, row) => sum + (Number(row.amount) || 0), 0),
    [destRows],
  );

  const hopLoss = useMemo(() => {
    if (!originAccountId || destRows.length === 0) return 0;
    return Math.round((afterCut - destSum) * 100) / 100;
  }, [afterCut, destRows.length, destSum, originAccountId]);

  const originEqualsDest = destRows.some((row) => row.accountId === originAccountId);
  const hopBlocked =
    !originAccountId ||
    destRows.length === 0 ||
    originEqualsDest ||
    destRows.some((row) => !row.amount || Number(row.amount) <= 0) ||
    (hopLoss > 0 && !keepRemainder && !lossCause);

  const payoutAccounts = useMemo(
    () => worldAccounts.filter((item) => item.kind === 'Payout' || item.label.toLowerCase().includes('payout')),
    [worldAccounts],
  );

  const reverseOptions = useMemo(() => {
    const fromSelection = selectedClaims.map((item) => item.originChargeId);
    const unique = [...new Set(fromSelection.filter(Boolean))];
    if (unique.length > 0) {
      return charges.filter((item) => unique.includes(item.chargeId));
    }
    return charges.filter((item) => item.status === 'Materialized' || item.status === 'Paid');
  }, [charges, selectedClaims]);

  function toggle(id: string) {
    setSelected((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]));
  }

  function updateDestination(index: number, patch: Partial<{ accountId: string; amount: string; currency: string }>) {
    setDestinations((current) => current.map((row, i) => (i === index ? { ...row, ...patch } : row)));
  }

  function hopValidationMessage() {
    if (!originAccountId) return 'Escolha a conta de origem.';
    if (destRows.length === 0) return 'Inclua pelo menos um destino.';
    if (originEqualsDest) return 'Origem e destino não podem ser a mesma conta.';
    if (destRows.some((row) => !row.amount || Number(row.amount) <= 0)) return 'Cada destino precisa de um valor.';
    if (hopLoss > 0 && !keepRemainder && !lossCause) return 'Perda maior que zero: informe a causa ou mantenha o resto na origem.';
    return '';
  }

  async function handleHop() {
    const message = hopValidationMessage();
    if (message) {
      setHopFieldError(message);
      reportError(message);
      return;
    }
    setBusy(true);
    const result = await registerHop({
      originAccountId: originAccountId.trim(),
      currency: currency.trim() || 'BRL',
      claimIds: selected.length > 0 ? selected : undefined,
      destinations: destRows.map((row) => ({
        accountId: row.accountId.trim(),
        amount: Number(row.amount),
        currency: row.currency.trim() || 'BRL',
      })),
      cut: cutPercent.trim()
        ? {
            orangeMemberId: cutOrangeId.trim(),
            percent: Number(cutPercent),
            inPlace: cutInPlace,
            orangeAccountId: cutInPlace ? undefined : cutOrangeAccountId.trim() || undefined,
          }
        : undefined,
      keepRemainderAtOrigin: hopLoss > 0 && keepRemainder,
      lossCause: hopLoss > 0 && !keepRemainder ? lossCause : undefined,
    });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    const loss = result.data?.lossAmount ?? 0;
    reportSuccess(loss > 0 ? `Hop registrado. Perda ${loss} ${currency} com causa.` : 'Hop registrado.');
    setSelected([]);
    await Promise.all([loadClaims(), loadHops()]);
  }

  async function handleRepass() {
    setBusy(true);
    const result = await repassClaims({
      originAccountId: (repassOriginId || originAccountId).trim(),
      claimIds: selected.length > 0 ? selected : undefined,
      payoutAccountId: payoutAccountId.trim(),
    });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Repasse registrado. Status dos claims: repassado.');
    setSelected([]);
    await Promise.all([loadClaims(), loadHops()]);
  }

  async function handleReveal() {
    if (selected.length !== 1) {
      reportError('Selecione um claim para revelar.');
      setConfirm(null);
      return;
    }
    if (!revealSummary.trim()) {
      reportError('O relatório controlado precisa de um texto.');
      return;
    }
    setBusy(true);
    const result = await revealClaim(selected[0], revealSummary.trim());
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess(
      'Claim revelado. No extrato do beneficiário a ponta passa de estimativa para valor liberado (pendente de saque).',
    );
    await loadClaims();
  }

  async function handleReverseCharge() {
    if (!reverseChargeId) {
      reportError('Escolha explicitamente a cobrança a estornar.');
      setConfirm(null);
      return;
    }
    if (!reverseCause) {
      reportError('Informe a causa do estorno.');
      return;
    }
    setBusy(true);
    const result = await reverseCharge(reverseChargeId, reverseCause);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Estorno aplicado. Claims e livro-mundo desta cobrança foram revertidos.');
    setReverseChargeId('');
    await loadClaims();
  }

  async function openFicha(claimId: string) {
    const result = await getClaim(claimId);
    if (!result.ok || !result.data) {
      reportError(result.ok ? 'Claim indisponível.' : result.error);
      return;
    }
    setFicha(result.data);
  }

  function requestAction(kind: ConfirmKind) {
    if (kind === 'reveal' && selected.length !== 1) {
      reportError('Selecione um claim para revelar.');
      return;
    }
    if (kind === 'reveal' && !revealSummary.trim()) {
      reportError('Preencha o relatório controlado antes de revelar.');
      return;
    }
    if (kind === 'reverse') {
      if (!reverseChargeId) {
        reportError('Escolha a cobrança no bloco Estornar — o filtro da lista não é o alvo.');
        return;
      }
      if (!reverseCause) {
        reportError('Informe a causa do estorno.');
        return;
      }
    }
    if (kind === 'hop') {
      const message = hopValidationMessage();
      setHopFieldError(message);
      if (message) {
        reportError(message);
        return;
      }
    }
    if (kind === 'repass' && (!(repassOriginId || originAccountId) || !payoutAccountId)) {
      reportError('Origem e conta payout são obrigatórias para o repasse.');
      return;
    }
    setConfirm(kind);
  }

  const fichaHops = ficha
    ? hops.filter(
        (hop) =>
          hop.bundleClaimIds.includes(ficha.claimId) ||
          hop.originAccountId === ficha.locationAccountId,
      )
    : [];

  const emptyCatalog = !loadingClaims && !loadingCatalog && items.length === 0 && !chargeId && !accountId && !beneficiaryId;

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Claims (a receber)"
        description="Direitos no livro-mundo: hops, cut proporcional, revelar e repasse. Contas pelo rótulo."
      />

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <ScrollText className="size-4" />
            Filtros
          </CardTitle>
          <CardDescription>A lista atualiza ao mudar os combos. Marque claims para hop, repasse ou revelar.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-1.5">
              <Label>Cobrança</Label>
              <Select value={chargeId || NONE} onValueChange={(value) => setChargeId(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Todas" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Todas</SelectItem>
                  {charges.map((item) => (
                    <SelectItem key={item.chargeId} value={item.chargeId}>
                      {chargeOptionLabel(item)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Conta</Label>
              <Select value={accountId || NONE} onValueChange={(value) => setAccountId(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Todas" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Todas</SelectItem>
                  {worldAccounts.map((item) => (
                    <SelectItem key={item.accountId} value={item.accountId}>
                      {item.label} · {worldKindLabel(item.kind)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Beneficiário</Label>
              <Select
                value={beneficiaryId || NONE}
                onValueChange={(value) => setBeneficiaryId(value === NONE ? '' : value)}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Todos" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Todos</SelectItem>
                  {members.map((item) => (
                    <SelectItem key={item.id} value={item.id}>
                      {item.username}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Status</Label>
              <Select value={statusFilter || NONE} onValueChange={(value) => setStatusFilter(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Todos" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Todos</SelectItem>
                  {Object.entries(CLAIM_STATUS_LABEL).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Button type="button" size="sm" variant="outline" onClick={() => void loadClaims()}>
              Atualizar
            </Button>
            {selectedCount > 0 ? (
              <p className="text-xs text-muted-foreground">{selectedCount} selecionado(s)</p>
            ) : null}
          </div>

          {loadingClaims || loadingCatalog ? (
            <div className="space-y-2">
              <Skeleton className="h-9 w-full" />
              <Skeleton className="h-9 w-full" />
              <Skeleton className="h-9 w-2/3" />
            </div>
          ) : visibleItems.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border/70 px-4 py-8 text-center">
              <p className="text-sm font-medium">Nenhum direito a receber nesta vista</p>
              {emptyCatalog ? (
                <ol className="mx-auto mt-3 max-w-md space-y-2 text-left text-sm">
                  <li>
                    <Link className="text-primary underline-offset-4 hover:underline" to="/dashboard/operations">
                      1. Abrir uma operação ativa
                    </Link>
                  </li>
                  <li>
                    <Link className="text-primary underline-offset-4 hover:underline" to="/dashboard/world-accounts">
                      2. Ligar um trilho (conta Gateway)
                    </Link>
                  </li>
                  <li>
                    <Link className="text-primary underline-offset-4 hover:underline" to="/dashboard/charges">
                      3. Gerar cobrança e marcar Paga
                    </Link>
                  </li>
                  <li>
                    <Link className="text-primary underline-offset-4 hover:underline" to="/dashboard/charges">
                      4. Materializar na aterrissagem
                    </Link>
                  </li>
                </ol>
              ) : (
                <p className="mt-1 text-xs text-muted-foreground">Ajuste os filtros ou materialize uma cobrança paga.</p>
              )}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-8" />
                  <TableHead>Beneficiário</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Visível</TableHead>
                  <TableHead>Montante</TableHead>
                  <TableHead>Conta</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {visibleItems.map((item) => (
                  <TableRow key={item.claimId} data-state={selected.includes(item.claimId) ? 'selected' : undefined}>
                    <TableCell>
                      <Checkbox
                        checked={selected.includes(item.claimId)}
                        onCheckedChange={() => toggle(item.claimId)}
                        aria-label={`Selecionar ${memberLabel(members, item.beneficiaryId)}`}
                      />
                    </TableCell>
                    <TableCell className="max-w-[10rem] truncate font-medium">
                      {memberLabel(members, item.beneficiaryId)}
                    </TableCell>
                    <TableCell className="text-xs">{kindLabel(item.kind)}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">{claimStatusLabel(item.status)}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={item.visible ? 'outline' : 'secondary'}>{item.visible ? 'Sim' : 'Não'}</Badge>
                    </TableCell>
                    <TableCell>
                      {item.amount} {item.currency}
                    </TableCell>
                    <TableCell className="max-w-[10rem] truncate">{worldLabel(worldAccounts, item.locationAccountId)}</TableCell>
                    <TableCell>
                      <Button type="button" size="sm" variant="ghost" className="h-7 px-2 text-xs" onClick={() => void openFicha(item.claimId)}>
                        Ficha
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="text-base">Registrar hop</CardTitle>
          <CardDescription>
            Claims no hop (bundle): {bundleClaims.length} · {afterCut} {currency} após cut. Destinos {destSum} {currency}.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Origem</Label>
              <Select value={originAccountId || NONE} onValueChange={(value) => setOriginAccountId(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Conta de origem" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Escolher conta</SelectItem>
                  {worldAccounts.map((item) => (
                    <SelectItem key={item.accountId} value={item.accountId}>
                      {item.label} · {worldKindLabel(item.kind)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Moeda origem</Label>
              <Select value={currency} onValueChange={setCurrency}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CURRENCIES.map((code) => (
                    <SelectItem key={code} value={code}>
                      {code}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between gap-2">
              <Label>Destinos</Label>
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={() => setDestinations((current) => [...current, { ...EMPTY_DESTINATION }])}
              >
                <Plus className="size-3.5" />
                Adicionar destino
              </Button>
            </div>
            <div className="space-y-2">
              {destinations.map((row, index) => (
                <div key={`dest-${index}`} className="grid gap-2 rounded-lg border border-border/60 p-2 sm:grid-cols-[minmax(0,1.4fr)_6rem_5.5rem_auto]">
                  <Select
                    value={row.accountId || NONE}
                    onValueChange={(value) => updateDestination(index, { accountId: value === NONE ? '' : value })}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder={`Destino ${index + 1}`} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={NONE}>Escolher conta</SelectItem>
                      {worldAccounts.map((item) => (
                        <SelectItem key={item.accountId} value={item.accountId}>
                          {item.label} · {worldKindLabel(item.kind)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Input
                    value={row.amount}
                    onChange={(e) => updateDestination(index, { amount: e.target.value })}
                    placeholder="Valor"
                    inputMode="decimal"
                  />
                  <Select value={row.currency} onValueChange={(value) => updateDestination(index, { currency: value })}>
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {CURRENCIES.map((code) => (
                        <SelectItem key={code} value={code}>
                          {code}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Button
                    type="button"
                    size="icon"
                    variant="ghost"
                    className="justify-self-end"
                    disabled={destinations.length === 1}
                    onClick={() => setDestinations((current) => current.filter((_, i) => i !== index))}
                    aria-label="Remover destino"
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              ))}
            </div>
            {hopFieldError ? <p className="text-xs text-destructive">{hopFieldError}</p> : null}
          </div>

          {hopLoss > 0 ? (
            <div className="space-y-3 rounded-lg border border-amber-500/40 bg-amber-500/5 px-3 py-3 text-sm">
              <p>
                Perda deste hop: <strong>{hopLoss} {currency}</strong> (bundle {afterCut} − destinos {destSum}).
                Sem aviso isto some do livro.
              </p>
              <label className="flex items-center gap-2">
                <Checkbox checked={keepRemainder} onCheckedChange={(value) => setKeepRemainder(value === true)} />
                Manter o resto na origem (não registrar perda)
              </label>
              {!keepRemainder ? (
                <div className="space-y-1.5">
                  <Label>Causa da perda</Label>
                  <Select value={lossCause || NONE} onValueChange={(value) => setLossCause(value === NONE ? '' : value)}>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Escolher causa" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={NONE}>Escolher causa</SelectItem>
                      {ATTRITION_CAUSES.map((item) => (
                        <SelectItem key={item.value} value={item.value}>
                          {item.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ) : null}
            </div>
          ) : null}

          <div className="rounded-lg border border-border/60">
            <button
              type="button"
              className="flex w-full items-center justify-between px-3 py-2 text-left text-sm font-medium"
              onClick={() => setCutOpen((open) => !open)}
            >
              Cut proporcional no bundle
              <ChevronDown className={cn('size-4 text-muted-foreground transition-transform', cutOpen && 'rotate-180')} />
            </button>
            {cutOpen ? (
              <div className="space-y-3 border-t border-border/60 px-3 py-3">
                <p className="text-xs text-muted-foreground">
                  O cut tira uma fatia proporcional dos claims deste hop, não substitui um destino.
                </p>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Cut %</Label>
                    <Input value={cutPercent} onChange={(e) => setCutPercent(e.target.value)} placeholder="Opcional" inputMode="decimal" />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Laranja</Label>
                    <Select value={cutOrangeId || NONE} onValueChange={(value) => setCutOrangeId(value === NONE ? '' : value)}>
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Membro" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value={NONE}>Nenhum</SelectItem>
                        {members.map((item) => (
                          <SelectItem key={item.id} value={item.id}>
                            {item.username}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-1.5">
                    <Label>Conta do Laranja</Label>
                    <Select
                      value={cutOrangeAccountId || NONE}
                      onValueChange={(value) => setCutOrangeAccountId(value === NONE ? '' : value)}
                      disabled={cutInPlace}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Conta" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value={NONE}>Nenhuma</SelectItem>
                        {worldAccounts.map((item) => (
                          <SelectItem key={item.accountId} value={item.accountId}>
                            {item.label} · {worldKindLabel(item.kind)}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <label className="flex items-center gap-2 text-sm">
                  <Checkbox checked={cutInPlace} onCheckedChange={(value) => setCutInPlace(value === true)} />
                  Cut fica na origem (proporcional no bundle)
                </label>
              </div>
            ) : null}
          </div>

          <Button type="button" size="sm" disabled={busy || hopBlocked} onClick={() => requestAction('hop')}>
            Registrar hop
          </Button>
        </CardContent>
      </Card>

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="text-base">Repasse</CardTitle>
          <CardDescription>Payout próprio. Os claims selecionados passam a status repassado e a origem é debitada.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Origem</Label>
              <Select value={repassOriginId || originAccountId || NONE} onValueChange={(value) => setRepassOriginId(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Conta de origem" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Escolher conta</SelectItem>
                  {worldAccounts.map((item) => (
                    <SelectItem key={item.accountId} value={item.accountId}>
                      {item.label} · {worldKindLabel(item.kind)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Conta payout</Label>
              <Select value={payoutAccountId || NONE} onValueChange={(value) => setPayoutAccountId(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Payout" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Escolher</SelectItem>
                  {(payoutAccounts.length > 0 ? payoutAccounts : worldAccounts).map((item) => (
                    <SelectItem key={item.accountId} value={item.accountId}>
                      {item.label} · {worldKindLabel(item.kind)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <Button type="button" size="sm" variant="secondary" disabled={busy} onClick={() => requestAction('repass')}>
            Confirmar repasse
          </Button>
        </CardContent>
      </Card>

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="text-base">Revelar</CardTitle>
          <CardDescription>
            Relatório controlado visível aqui. No extrato, a ponta deixa a estimativa e mostra o valor liberado.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="space-y-1.5">
            <Label>Relatório controlado</Label>
            <Textarea
              value={revealSummary}
              onChange={(e) => setRevealSummary(e.target.value)}
              placeholder="Texto que o beneficiário verá no extrato"
            />
          </div>
          <Button type="button" size="sm" variant="secondary" disabled={busy} onClick={() => requestAction('reveal')}>
            Revelar claim selecionado
          </Button>
        </CardContent>
      </Card>

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="text-base">Estornar cobrança</CardTitle>
          <CardDescription>
            Escolha a cobrança neste bloco — o filtro da tabela não é o alvo. Reverte claims e o livro-mundo.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Cobrança a estornar</Label>
              <Select value={reverseChargeId || NONE} onValueChange={(value) => setReverseChargeId(value === NONE ? '' : value)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Seleção explícita" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Nenhuma</SelectItem>
                  {reverseOptions.map((item) => (
                    <SelectItem key={item.chargeId} value={item.chargeId}>
                      {chargeOptionLabel(item)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Causa</Label>
              <Select value={reverseCause || NONE} onValueChange={(value) => setReverseCause(value === NONE ? '' : value)}>
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
            </div>
          </div>
          <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => requestAction('reverse')}>
            Estornar
          </Button>
        </CardContent>
      </Card>

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="text-base">Hops</CardTitle>
          <CardDescription>Últimos movimentos da origem (ou da conta filtrada). Perda = origem − destinos.</CardDescription>
        </CardHeader>
        <CardContent>
          {hops.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhum hop listado.</p>
          ) : (
            <div className="space-y-2">
              {hops.map((hop) => (
                <div key={hop.hopId} className="rounded-lg border border-border/60 px-3 py-2 text-sm">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="text-xs text-muted-foreground">{new Date(hop.occurredAt).toLocaleString('pt-BR')}</span>
                    <span className={cn('text-xs', hop.lossAmount > 0 ? 'font-medium text-amber-600' : 'text-muted-foreground')}>
                      {hop.lossAmount > 0 ? `perda ${hop.lossAmount} ${hop.originCurrency}` : 'sem perda'}
                    </span>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {worldLabel(worldAccounts, hop.originAccountId)} →{' '}
                    {hop.destinations.map((dest) => `${worldLabel(worldAccounts, dest.accountId)} (${dest.amount})`).join(', ')}
                  </p>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={ficha !== null} onOpenChange={(open) => { if (!open) setFicha(null); }}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Ficha do claim</DialogTitle>
            <DialogDescription>Fato no livro, não só o identificador.</DialogDescription>
          </DialogHeader>
          {ficha ? (
            <dl className="grid gap-2 text-sm sm:grid-cols-2">
              <div>
                <dt className="text-xs text-muted-foreground">Beneficiário</dt>
                <dd>{memberLabel(members, ficha.beneficiaryId)}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Conta</dt>
                <dd>{worldLabel(worldAccounts, ficha.locationAccountId)}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Montante</dt>
                <dd>
                  {ficha.amount} {ficha.currency}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Status</dt>
                <dd>{claimStatusLabel(ficha.status)}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Visível</dt>
                <dd>{ficha.visible ? 'Sim' : 'Não'}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Tipo</dt>
                <dd>{kindLabel(ficha.kind)}</dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="text-xs text-muted-foreground">Cobrança origem</dt>
                <dd>{chargeOptionLabel(charges.find((item) => item.chargeId === ficha.originChargeId), ficha.originChargeId)}</dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="text-xs text-muted-foreground">Hops</dt>
                <dd>
                  {fichaHops.length === 0
                    ? 'Nenhum hop listado para esta conta.'
                    : fichaHops.map((hop) => (
                        <p key={hop.hopId} className="text-xs text-muted-foreground">
                          {worldLabel(worldAccounts, hop.originAccountId)} → {hop.destinations.map((d) => worldLabel(worldAccounts, d.accountId)).join(', ')}
                          {hop.lossAmount > 0 ? ` · perda ${hop.lossAmount}` : ''}
                        </p>
                      ))}
                </dd>
              </div>
            </dl>
          ) : null}
        </DialogContent>
      </Dialog>

      <Dialog open={confirm !== null} onOpenChange={(open) => { if (!open) setConfirm(null); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {confirm === 'hop' && 'Registrar hop'}
              {confirm === 'repass' && 'Confirmar repasse'}
              {confirm === 'reveal' && 'Revelar claim'}
              {confirm === 'reverse' && 'Estornar cobrança'}
            </DialogTitle>
            <DialogDescription asChild>
              <div className="space-y-2">
                {confirm === 'hop' && (
                  <>
                    <p>
                      Mover {selectedCount || bundleClaims.length} claim(s) de {worldLabel(worldAccounts, originAccountId)} para{' '}
                      {destRows.length} destino(s). Bundle {afterCut} {currency} · destinos {destSum} {currency}.
                    </p>
                    {hopLoss > 0 ? (
                      <p className="font-medium text-foreground">
                        Perda {hopLoss} {currency}.{' '}
                        {keepRemainder
                          ? 'O resto permanece na origem.'
                          : `Causa: ${ATTRITION_CAUSES.find((item) => item.value === lossCause)?.label ?? lossCause}.`}
                      </p>
                    ) : (
                      <p>Sem perda: destinos cobrem o bundle.</p>
                    )}
                  </>
                )}
                {confirm === 'repass' && (
                  <p>
                    Repassar para {worldLabel(worldAccounts, payoutAccountId)}. Status → repassado. Esta ação debita a origem.
                  </p>
                )}
                {confirm === 'reveal' && (
                  <p>
                    Tornar visível o claim de {selected[0] ? memberLabel(members, items.find((item) => item.claimId === selected[0])?.beneficiaryId ?? '') : '—'}.
                    Relatório: {revealSummary.trim()}. No extrato a ponta deixa a estimativa.
                  </p>
                )}
                {confirm === 'reverse' && (
                  <p>
                    Estornar {chargeOptionLabel(charges.find((item) => item.chargeId === reverseChargeId), reverseChargeId)}.
                    Causa: {ATTRITION_CAUSES.find((item) => item.value === reverseCause)?.label}. Reverte claims e o livro-mundo.
                  </p>
                )}
              </div>
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirm(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant={confirm === 'reverse' || (confirm === 'hop' && hopLoss > 0 && !keepRemainder) ? 'destructive' : 'default'}
              disabled={busy || (confirm === 'hop' && hopBlocked)}
              onClick={() => {
                if (confirm === 'hop') void handleHop();
                if (confirm === 'repass') void handleRepass();
                if (confirm === 'reveal') void handleReveal();
                if (confirm === 'reverse') void handleReverseCharge();
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
