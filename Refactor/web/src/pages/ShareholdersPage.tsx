import { useCallback, useEffect, useState } from 'react';
import { PieChart } from 'lucide-react';
import {
  listShareholders,
  removeShareholder,
  upsertShareholder,
  type Shareholder,
} from '@/api/administrator/mandates';
import { PageHeader } from '@/components/layout/page-header';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { toast } from 'sonner';

export function ShareholdersPage() {
  const [items, setItems] = useState<Shareholder[]>([]);
  const [totalPercent, setTotalPercent] = useState(0);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [accountId, setAccountId] = useState('');
  const [percentage, setPercentage] = useState('10');

  const load = useCallback(async () => {
    setLoading(true);
    const result = await listShareholders();
    setLoading(false);
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }
    setItems(result.data.items ?? []);
    setTotalPercent(result.data.totalPercent ?? 0);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleSave() {
    setBusy(true);
    const result = await upsertShareholder(accountId.trim(), Number(percentage));
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Participação salva.');
    setAccountId('');
    await load();
  }

  async function handleRemove(id: string) {
    setBusy(true);
    const result = await removeShareholder(id);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Participação removida.');
    await load();
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Acionistas"
        description="Beneficiários do nível 2 (login read-only). Soma das participações ≤ 100%. Não é mandato de gestão."
      />

      <div className="grid gap-4 lg:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="text-base">Incluir / atualizar</CardTitle>
            <CardDescription>Account id (UUID) + percentual global.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="share-account">Conta</Label>
              <Input id="share-account" value={accountId} onChange={(e) => setAccountId(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="share-pct">Participação %</Label>
              <Input id="share-pct" type="number" value={percentage} onChange={(e) => setPercentage(e.target.value)} />
            </div>
            <Button type="button" disabled={busy} onClick={() => void handleSave()}>
              Salvar
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
          <CardContent className="space-y-3 p-4">
            {loading ? <p className="text-sm text-muted-foreground">Carregando…</p> : null}
            {!loading && items.length === 0 ? (
              <p className="text-sm text-muted-foreground">Nenhum Acionista cadastrado.</p>
            ) : null}
            {items.map((item) => (
              <div key={item.accountId} className="flex items-center justify-between gap-3 rounded-lg border border-border/60 px-3 py-3 text-sm">
                <div className="min-w-0">
                  <p className="font-medium">{item.percentage}%</p>
                  <p className="truncate font-mono text-[0.65rem] text-muted-foreground">{item.accountId}</p>
                </div>
                <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleRemove(item.accountId)}>
                  Remover
                </Button>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
