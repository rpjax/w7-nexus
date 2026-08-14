import { useCallback, useEffect, useState } from 'react';
import { Handshake } from 'lucide-react';
import {
  closeAgencyDeal,
  listAgencyDeals,
  upsertAgencyDeal,
  type AgencyDeal,
} from '@/api/administrator/mandates';
import { PageHeader } from '@/components/layout/page-header';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { toast } from 'sonner';

export function DealsPage() {
  const [items, setItems] = useState<AgencyDeal[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [recruiterAccountId, setRecruiterAccountId] = useState('');
  const [operatorAccountId, setOperatorAccountId] = useState('');
  const [operatorPercent, setOperatorPercent] = useState('80');
  const [recruiterPercent, setRecruiterPercent] = useState('0');

  const load = useCallback(async () => {
    setLoading(true);
    const result = await listAgencyDeals();
    setLoading(false);
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }
    setItems(result.data.items ?? []);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleSave() {
    setBusy(true);
    const result = await upsertAgencyDeal({
      recruiterAccountId: recruiterAccountId.trim(),
      operatorAccountId: operatorAccountId.trim(),
      operatorPercent: Number(operatorPercent),
      recruiterPercent: Number(recruiterPercent),
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Deal salvo.');
    setOperatorAccountId('');
    await load();
  }

  async function handleClose(operatorId: string) {
    setBusy(true);
    const result = await closeAgencyDeal(operatorId);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Deal encerrado.');
    await load();
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Deals de agenciamento"
        description="Vínculo global Recrutador ↔ Operador. Soma dos % ≤ 100; resto = Residual da Org. Raiz: Admin com recrutador_pct = 0."
      />

      <div className="grid gap-4 lg:grid-cols-[22rem_minmax(0,1fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="text-base">Novo / atualizar deal</CardTitle>
            <CardDescription>IDs de conta (UUID). Atualiza o deal ativo do Operador.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="recruiter">Recrutador (account id)</Label>
              <Input id="recruiter" value={recruiterAccountId} onChange={(e) => setRecruiterAccountId(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="operator">Operador (account id)</Label>
              <Input id="operator" value={operatorAccountId} onChange={(e) => setOperatorAccountId(e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1.5">
                <Label htmlFor="opPct">Operador %</Label>
                <Input id="opPct" type="number" value={operatorPercent} onChange={(e) => setOperatorPercent(e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="recPct">Recrutador %</Label>
                <Input id="recPct" type="number" value={recruiterPercent} onChange={(e) => setRecruiterPercent(e.target.value)} />
              </div>
            </div>
            <Button type="button" disabled={busy} onClick={() => void handleSave()}>
              Salvar deal
            </Button>
          </CardContent>
        </Card>

        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Handshake className="size-4" />
              Deals ativos
            </CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="space-y-3 p-4">
            {loading ? <p className="text-sm text-muted-foreground">Carregando…</p> : null}
            {!loading && items.length === 0 ? (
              <p className="text-sm text-muted-foreground">Nenhum deal ativo.</p>
            ) : null}
            {items.map((deal) => (
              <div key={deal.dealId} className="rounded-lg border border-border/60 px-3 py-3 text-sm">
                <p className="font-medium">
                  Op {deal.operatorPercent}% · Rec {deal.recruiterPercent}%
                </p>
                <p className="mt-1 truncate font-mono text-[0.65rem] text-muted-foreground" title={deal.operatorAccountId}>
                  Operador: {deal.operatorAccountId}
                </p>
                <p className="truncate font-mono text-[0.65rem] text-muted-foreground" title={deal.recruiterAccountId}>
                  Recrutador: {deal.recruiterAccountId}
                </p>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  className="mt-2"
                  disabled={busy}
                  onClick={() => void handleClose(deal.operatorAccountId)}
                >
                  Encerrar…
                </Button>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
