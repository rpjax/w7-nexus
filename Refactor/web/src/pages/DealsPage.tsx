import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Handshake } from 'lucide-react';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import {
  closeAgencyDeal,
  listAgencyDeals,
  upsertAgencyDeal,
  type AgencyDeal,
} from '@/api/administrator/mandates';
import type { AccountDetails } from '@/auth/types';
import { DataTable } from '@/components/data/data-table';
import { PageHeader } from '@/components/layout/page-header';
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
import { type ColumnDef } from '@tanstack/react-table';
import { reportError, reportSuccess } from '@/feedback';

function personLabel(accounts: AccountDetails[], id: string): string {
  const match = accounts.find((item) => item.id === id);
  return match ? match.username : `${id.slice(0, 8)}…`;
}

function parsePercent(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === '') return null;
  const value = Number(trimmed);
  if (!Number.isFinite(value)) return null;
  if (value < 0 || value > 100) return null;
  return value;
}

function humanizeDealError(error: string): string {
  const text = error.toLowerCase();
  if (text.includes('agencydeal') || text.includes('pct=0') || text.includes('percent')) {
    return 'Não foi possível gravar o vínculo. Informe fatias de 0 a 100 cuja soma não passe de 100%. O que sobrar fica com a organização.';
  }
  return error;
}

export function DealsPage() {
  const [searchParams] = useSearchParams();
  const operatorFromQuery = searchParams.get('operator') ?? '';
  const [items, setItems] = useState<AgencyDeal[]>([]);
  const [accounts, setAccounts] = useState<AccountDetails[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [recruiterAccountId, setRecruiterAccountId] = useState('');
  const [operatorAccountId, setOperatorAccountId] = useState(operatorFromQuery);
  const [operatorPercent, setOperatorPercent] = useState('80');
  const [recruiterPercent, setRecruiterPercent] = useState('0');
  const [pctTouched, setPctTouched] = useState(false);
  const [confirm, setConfirm] = useState<null | 'save' | { close: string }>(null);

  const operatorPct = parsePercent(operatorPercent);
  const recruiterPct = parsePercent(recruiterPercent);
  const pctSum = operatorPct != null && recruiterPct != null ? operatorPct + recruiterPct : null;
  const percentsValid = operatorPct != null && recruiterPct != null && pctSum != null && pctSum <= 100;
  const percentHint = !percentsValid
    ? 'Informe duas fatias de 0 a 100 cuja soma não passe de 100%.'
    : null;

  const load = useCallback(async () => {
    setLoading(true);
    const [dealsResult, accountsResult] = await Promise.all([
      listAgencyDeals(),
      searchAdministratorAccounts({ limit: 200, offset: 0 }),
    ]);
    setLoading(false);

    if (!dealsResult.ok || !dealsResult.data) {
      const message = dealsResult.ok ? 'Resposta inválida.' : humanizeDealError(dealsResult.error);
      setLoadError(message);
      reportError(message);
      return;
    }
    setLoadError(null);
    setItems(dealsResult.data.items ?? []);
    if (!accountsResult.ok || !accountsResult.data) {
      reportError(accountsResult.ok ? 'Não foi possível listar pessoas.' : accountsResult.error);
    } else {
      setAccounts(accountsResult.data.items ?? []);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (operatorFromQuery) setOperatorAccountId(operatorFromQuery);
  }, [operatorFromQuery]);

  async function handleSave() {
    if (!percentsValid || operatorPct == null || recruiterPct == null) {
      setPctTouched(true);
      reportError('Informe fatias válidas (0 a 100, soma até 100%) antes de salvar.');
      return;
    }
    setBusy(true);
    const result = await upsertAgencyDeal({
      recruiterAccountId: recruiterAccountId.trim(),
      operatorAccountId: operatorAccountId.trim(),
      operatorPercent: operatorPct,
      recruiterPercent: recruiterPct,
    });
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(humanizeDealError(result.error));
      return;
    }
    reportSuccess('Vínculo salvo.');
    setOperatorAccountId('');
    await load();
  }

  async function handleClose(operatorId: string) {
    setBusy(true);
    const result = await closeAgencyDeal(operatorId);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(humanizeDealError(result.error));
      return;
    }
    reportSuccess('Agenciamento encerrado.');
    await load();
  }

  const columns = useMemo<ColumnDef<AgencyDeal>[]>(() => [
    {
      id: 'operator',
      header: 'Operador',
      cell: ({ row }) => (
        <span className="font-medium">{personLabel(accounts, row.original.operatorAccountId)}</span>
      ),
    },
    {
      id: 'recruiter',
      header: 'Recrutador',
      cell: ({ row }) => personLabel(accounts, row.original.recruiterAccountId),
    },
    {
      accessorKey: 'operatorPercent',
      header: 'Op. %',
      cell: ({ row }) => (
        <span className="tabular-nums">{row.original.operatorPercent}%</span>
      ),
    },
    {
      accessorKey: 'recruiterPercent',
      header: 'Rec. %',
      cell: ({ row }) => (
        <span className="tabular-nums">{row.original.recruiterPercent}%</span>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={busy}
          onClick={(event) => {
            event.stopPropagation();
            setConfirm({ close: row.original.operatorAccountId });
          }}
        >
          Encerrar…
        </Button>
      ),
    },
  ], [accounts, busy]);

  const recruiterName = personLabel(accounts, recruiterAccountId);
  const operatorName = personLabel(accounts, operatorAccountId);
  const shareCopy = percentsValid
    ? `Operador ${operatorPct}% e Recrutador ${recruiterPct}%`
    : null;

  function requestSave() {
    setPctTouched(true);
    if (!percentsValid) {
      reportError('Informe fatias válidas (0 a 100, soma até 100%) antes de salvar.');
      return;
    }
    setConfirm('save');
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Agenciamento"
        description="Vínculo Recrutador ↔ Operador. Defina a fatia de cada um; o cadastro vale para toda a organização."
      />

      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="text-base">Como funciona</CardTitle>
          <CardDescription>
            Cada pessoa agenciada tem duas fatias: Operador e Recrutador, de 0 a 100%, com soma até 100%.
            O que sobrar fica com a organização. A raiz (Admin) usa Recrutador 0%.
          </CardDescription>
        </CardHeader>
      </Card>

      <div className="grid gap-4 lg:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="text-base">Novo / atualizar vínculo</CardTitle>
            <CardDescription>Escolha as pessoas na lista. Atualiza o agenciamento ativo do Operador.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label>Recrutador</Label>
              <Select value={recruiterAccountId} onValueChange={setRecruiterAccountId}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecionar pessoa" />
                </SelectTrigger>
                <SelectContent>
                  {accounts.map((account) => (
                    <SelectItem key={account.id} value={account.id}>
                      {account.username}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Operador</Label>
              <Select value={operatorAccountId} onValueChange={setOperatorAccountId}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecionar pessoa" />
                </SelectTrigger>
                <SelectContent>
                  {accounts.map((account) => (
                    <SelectItem key={account.id} value={account.id}>
                      {account.username}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1.5">
                <Label htmlFor="opPct">Operador %</Label>
                <Input
                  id="opPct"
                  type="number"
                  min={0}
                  max={100}
                  value={operatorPercent}
                  aria-invalid={pctTouched && operatorPct == null}
                  onChange={(e) => {
                    setOperatorPercent(e.target.value);
                    setPctTouched(true);
                  }}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="recPct">Recrutador %</Label>
                <Input
                  id="recPct"
                  type="number"
                  min={0}
                  max={100}
                  value={recruiterPercent}
                  aria-invalid={pctTouched && recruiterPct == null}
                  onChange={(e) => {
                    setRecruiterPercent(e.target.value);
                    setPctTouched(true);
                  }}
                />
              </div>
            </div>
            {pctTouched && percentHint ? (
              <p className="text-sm text-destructive">{percentHint}</p>
            ) : null}
            <Button
              type="button"
              disabled={busy || !recruiterAccountId || !operatorAccountId || !percentsValid}
              onClick={requestSave}
            >
              Salvar vínculo…
            </Button>
          </CardContent>
        </Card>

        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Handshake className="size-4" />
              Vínculos ativos
            </CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="p-0">
            <DataTable
              columns={columns}
              data={items}
              loading={loading}
              errorMessage={loadError}
              emptyMessage="Nenhum agenciamento ativo."
              getRowId={(row) => row.dealId}
            />
          </CardContent>
        </Card>
      </div>

      <Dialog open={confirm !== null} onOpenChange={(open) => { if (!open) setConfirm(null); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {confirm === 'save' ? 'Salvar vínculo' : 'Encerrar agenciamento'}
            </DialogTitle>
            <DialogDescription>
              {confirm === 'save' ? (
                shareCopy ? (
                  <>Vincular {operatorName} a {recruiterName}: {shareCopy}.</>
                ) : (
                  <>Informe fatias válidas antes de confirmar.</>
                )
              ) : (
                <>Encerrar o agenciamento ativo desta pessoa. O papel de Operador pode ficar sem efeito até um vínculo novo.</>
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirm(null)}>Cancelar</Button>
            <Button
              type="button"
              variant={confirm === 'save' ? 'default' : 'destructive'}
              disabled={busy || (confirm === 'save' && !percentsValid)}
              onClick={() => {
                if (confirm === 'save') void handleSave();
                else if (confirm && typeof confirm === 'object') void handleClose(confirm.close);
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
