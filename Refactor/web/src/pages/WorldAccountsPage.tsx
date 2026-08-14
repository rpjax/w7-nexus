import { useCallback, useEffect, useState } from 'react';
import { Wallet } from 'lucide-react';
import {
  configureWorldAccount,
  listWorldAccountTransactions,
  listWorldAccounts,
  openWorldAccount,
  recordWorldAccountObservation,
  type WorldAccount,
  type WorldAccountTransaction,
} from '@/api/administrator/worldAccounts';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

function formatMap(map: Record<string, number> | undefined): string {
  const entries = Object.entries(map ?? {});
  if (entries.length === 0) return '—';
  return entries.map(([currency, amount]) => `${currency} ${amount}`).join(' · ');
}

export function WorldAccountsPage() {
  const [items, setItems] = useState<WorldAccount[]>([]);
  const [selected, setSelected] = useState<WorldAccount | null>(null);
  const [transactions, setTransactions] = useState<WorldAccountTransaction[]>([]);
  const [busy, setBusy] = useState(false);
  const [kind, setKind] = useState('Gateway');
  const [label, setLabel] = useState('');
  const [orangeId, setOrangeId] = useState('');
  const [cut, setCut] = useState('10');
  const [quotaCurrency, setQuotaCurrency] = useState('BRL');
  const [quotaRemaining, setQuotaRemaining] = useState('10000');
  const [obsCurrency, setObsCurrency] = useState('BRL');
  const [obsAmount, setObsAmount] = useState('100');
  const [obsMemo, setObsMemo] = useState('');

  const load = useCallback(async () => {
    const result = await listWorldAccounts();
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }
    setItems(result.data.items ?? []);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function select(accountId: string) {
    const current = (await listWorldAccounts()).data?.items?.find((item) => item.accountId === accountId);
    if (!current) return;
    setSelected(current);
    const tx = await listWorldAccountTransactions(accountId);
    setTransactions(tx.ok && tx.data ? tx.data.items ?? [] : []);
  }

  async function handleOpen() {
    setBusy(true);
    const result = await openWorldAccount({
      kind,
      label: label.trim(),
      orangeMemberId: kind === 'Gateway' ? orangeId.trim() : undefined,
      level1CutPercent: kind === 'Gateway' ? Number(cut) : undefined,
      quotaCurrency: quotaCurrency.trim() || undefined,
      quotaRemaining: quotaRemaining.trim() === '' ? undefined : Number(quotaRemaining),
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Conta aberta.');
    setLabel('');
    await load();
    if (result.data?.accountId) await select(result.data.accountId);
  }

  async function handleObserve(direction: 'credit' | 'debit') {
    if (!selected) return;
    setBusy(true);
    const result = await recordWorldAccountObservation(selected.accountId, {
      direction,
      currency: obsCurrency.trim(),
      amount: Number(obsAmount),
      memo: obsMemo.trim() || undefined,
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success(direction === 'credit' ? 'Crédito observado.' : 'Débito observado.');
    await load();
    await select(selected.accountId);
  }

  async function handleQuota() {
    if (!selected) return;
    setBusy(true);
    const result = await configureWorldAccount(selected.accountId, {
      quotaCurrency: quotaCurrency.trim(),
      quotaRemaining: Number(quotaRemaining),
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Quota atualizada.');
    await load();
    await select(selected.accountId);
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Livro-mundo"
        description="Contas de gateway, banco, crypto e payout. Saldo só muda por observação; quota por moeda."
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.2fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Wallet className="size-4" />
              Contas
            </CardTitle>
            <CardDescription>Abra uma conta e selecione para ver saldos e quotas.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-1">
                <Label>Tipo</Label>
                <Input value={kind} onChange={(e) => setKind(e.target.value)} placeholder="Gateway | Bank | Crypto | Payout" />
              </div>
              <div className="space-y-1">
                <Label>Rótulo</Label>
                <Input value={label} onChange={(e) => setLabel(e.target.value)} />
              </div>
              {kind.toLowerCase() === 'gateway' ? (
                <>
                  <div className="space-y-1 sm:col-span-2">
                    <Label>Laranja</Label>
                    <Input value={orangeId} onChange={(e) => setOrangeId(e.target.value)} placeholder="Account id (UUID)" />
                  </div>
                  <div className="space-y-1">
                    <Label>Cut nível-1</Label>
                    <Input value={cut} onChange={(e) => setCut(e.target.value)} />
                  </div>
                </>
              ) : null}
              <div className="space-y-1">
                <Label>Moeda da quota</Label>
                <Input value={quotaCurrency} onChange={(e) => setQuotaCurrency(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>Quota</Label>
                <Input value={quotaRemaining} onChange={(e) => setQuotaRemaining(e.target.value)} />
              </div>
            </div>
            <Button type="button" disabled={busy || !label.trim()} onClick={() => void handleOpen()}>
              Abrir
            </Button>
            <Separator />
            <div className="space-y-2">
              {items.map((item) => (
                <button
                  key={item.accountId}
                  type="button"
                  className={cn(
                    'w-full rounded-lg border border-border/60 px-3 py-2 text-left text-sm transition-colors hover:bg-muted/40',
                    selected?.accountId === item.accountId && 'border-primary/40 bg-muted/30',
                  )}
                  onClick={() => void select(item.accountId)}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">{item.label}</span>
                    <Badge variant="secondary">{item.kind}</Badge>
                  </div>
                  <p className="mt-1 truncate text-xs text-muted-foreground">
                    quota {formatMap(item.quotas)} · saldo {formatMap(item.balances)}
                  </p>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardHeader>
              <CardTitle className="text-base">{selected.label}</CardTitle>
              <CardDescription className="font-mono text-xs">{selected.accountId}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Emissão {selected.emissionStatus} · saldo {selected.balanceStatus}
              </p>
              <p className="text-sm">Quotas: {formatMap(selected.quotas)}</p>
              <p className="text-sm">Saldos: {formatMap(selected.balances)}</p>
              <Separator />
              <section className="space-y-2">
                <h3 className="text-sm font-medium">Quota</h3>
                <div className="flex gap-2">
                  <Input value={quotaCurrency} onChange={(e) => setQuotaCurrency(e.target.value)} />
                  <Input value={quotaRemaining} onChange={(e) => setQuotaRemaining(e.target.value)} />
                  <Button type="button" size="sm" disabled={busy} onClick={() => void handleQuota()}>
                    Salvar
                  </Button>
                </div>
              </section>
              <section className="space-y-2">
                <h3 className="text-sm font-medium">Observação de TX</h3>
                <div className="flex gap-2">
                  <Input value={obsCurrency} onChange={(e) => setObsCurrency(e.target.value)} />
                  <Input value={obsAmount} onChange={(e) => setObsAmount(e.target.value)} />
                </div>
                <Input value={obsMemo} onChange={(e) => setObsMemo(e.target.value)} placeholder="Memo (opcional)" />
                <div className="flex gap-2">
                  <Button type="button" size="sm" disabled={busy} onClick={() => void handleObserve('credit')}>
                    Crédito
                  </Button>
                  <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleObserve('debit')}>
                    Débito
                  </Button>
                </div>
              </section>
              <Separator />
              <div className="space-y-1">
                {transactions.map((tx, index) => (
                  <p key={`${tx.occurredAt}-${index}`} className="font-mono text-xs text-muted-foreground">
                    {tx.kind} {tx.currency} {tx.amount}
                    {tx.memo ? ` · ${tx.memo}` : ''}
                  </p>
                ))}
              </div>
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden border-dashed border-border/60 bg-card/50 lg:flex">
            <CardContent className="flex flex-1 items-center justify-center p-8 text-sm text-muted-foreground">
              Selecione uma conta.
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
