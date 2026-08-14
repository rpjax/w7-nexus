import { useCallback, useEffect, useMemo, useState } from 'react';
import { PieChart } from 'lucide-react';
import { type ColumnDef } from '@tanstack/react-table';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import {
  listShareholders,
  removeShareholder,
  upsertShareholder,
  type Shareholder,
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
import { reportError, reportSuccess } from '@/feedback';

const STAKE_SUM_ASCII = 'A soma das participacoes de Acionistas nao pode exceder 100%.';
const STAKE_SUM_PT = 'A soma das participações de Acionistas não pode exceder 100%.';

function usernameLabel(accounts: AccountDetails[], id: string): string {
  const match = accounts.find((item) => item.id === id);
  return match ? match.username : `${id.slice(0, 8)}…`;
}

function stakeErrorMessage(raw: string): string {
  if (raw === STAKE_SUM_ASCII || raw.includes('participacoes de Acionistas') || raw.includes('exceder 100%')) {
    return STAKE_SUM_PT;
  }
  return raw;
}

export function ShareholdersPage() {
  const [items, setItems] = useState<Shareholder[]>([]);
  const [accounts, setAccounts] = useState<AccountDetails[]>([]);
  const [totalPercent, setTotalPercent] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [accountId, setAccountId] = useState('');
  const [percentage, setPercentage] = useState('10');
  const [confirm, setConfirm] = useState<null | 'save' | { remove: string }>(null);

  const load = useCallback(async () => {
    setLoading(true);
    const [shareResult, accountsResult] = await Promise.all([
      listShareholders(),
      searchAdministratorAccounts({ limit: 200, offset: 0 }),
    ]);
    setLoading(false);
    if (!shareResult.ok || !shareResult.data) {
      const message = shareResult.ok ? 'Resposta inválida.' : shareResult.error;
      setLoadError(message);
      reportError(message);
      return;
    }
    setLoadError(null);
    setItems(shareResult.data.items ?? []);
    setTotalPercent(shareResult.data.totalPercent ?? 0);
    if (!accountsResult.ok || !accountsResult.data) {
      reportError(accountsResult.ok ? 'Não foi possível listar usuários.' : accountsResult.error);
    } else {
      setAccounts(accountsResult.data.items ?? []);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleSave() {
    setBusy(true);
    const result = await upsertShareholder(accountId.trim(), Number(percentage));
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(stakeErrorMessage(result.error));
      return;
    }
    reportSuccess('Participação salva.');
    setAccountId('');
    await load();
  }

  async function handleRemove(id: string) {
    setBusy(true);
    const result = await removeShareholder(id);
    setBusy(false);
    setConfirm(null);
    if (!result.ok) {
      reportError(result.error);
      return;
    }
    reportSuccess('Participação removida.');
    await load();
  }

  const columns = useMemo<ColumnDef<Shareholder>[]>(() => [
    {
      id: 'account',
      header: 'Usuário',
      cell: ({ row }) => (
        <span className="font-medium">{usernameLabel(accounts, row.original.accountId)}</span>
      ),
    },
    {
      accessorKey: 'percentage',
      header: '%',
      cell: ({ row }) => (
        <span className="tabular-nums">{row.original.percentage}%</span>
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
            setConfirm({ remove: row.original.accountId });
          }}
        >
          Remover…
        </Button>
      ),
    },
  ], [accounts, busy]);

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Acionistas"
        description="Fatia residual da Org: percentual do que sobra depois do agenciamento. Não dá poder de gestão — Admin continua a mandar na casa. Soma das participações ≤ 100%."
      />

      <div className="grid gap-4 lg:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="text-base">Incluir / atualizar</CardTitle>
            <CardDescription>Usuário da lista + percentual da fatia residual.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label>Usuário</Label>
              <Select value={accountId} onValueChange={setAccountId}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecionar usuário" />
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
              <Label htmlFor="share-pct">Participação %</Label>
              <Input id="share-pct" type="number" value={percentage} onChange={(e) => setPercentage(e.target.value)} />
            </div>
            <Button type="button" disabled={busy || !accountId} onClick={() => setConfirm('save')}>
              Salvar…
            </Button>
          </CardContent>
        </Card>

        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <PieChart className="size-4" />
              Lista · total {totalPercent}%
            </CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="p-0">
            <DataTable
              columns={columns}
              data={items}
              loading={loading}
              errorMessage={loadError}
              emptyMessage="Nenhum acionista cadastrado. Inclua um usuário e o percentual à esquerda — a soma das fatias não pode passar de 100%."
              getRowId={(row) => row.accountId}
            />
          </CardContent>
        </Card>
      </div>

      <Dialog open={confirm !== null} onOpenChange={(open) => { if (!open) setConfirm(null); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{confirm === 'save' ? 'Salvar participação' : 'Remover participação'}</DialogTitle>
            <DialogDescription>
              {confirm === 'save' ? (
                <>
                  {usernameLabel(accounts, accountId)} passará a ter {percentage}% da fatia residual da Org.
                </>
              ) : confirm && typeof confirm === 'object' ? (
                <>
                  Remover {usernameLabel(accounts, confirm.remove)} ({items.find((item) => item.accountId === confirm.remove)?.percentage ?? '—'}%) da soma de Acionistas. Sem poder de gestão — só a fatia residual.
                </>
              ) : null}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirm(null)}>Cancelar</Button>
            <Button
              type="button"
              variant={confirm === 'save' ? 'default' : 'destructive'}
              disabled={busy}
              onClick={() => {
                if (confirm === 'save') void handleSave();
                else if (confirm && typeof confirm === 'object') void handleRemove(confirm.remove);
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
