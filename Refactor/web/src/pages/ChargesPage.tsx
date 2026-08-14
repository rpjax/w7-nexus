import { useCallback, useEffect, useState } from 'react';
import { Receipt } from 'lucide-react';
import {
  createCharge,
  getCharge,
  listCharges,
  markChargePaid,
  transitionCharge,
  type Charge,
} from '@/api/administrator/charging';
import { materializeCharge } from '@/api/administrator/ledger';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

export function ChargesPage() {
  const [items, setItems] = useState<Charge[]>([]);
  const [selected, setSelected] = useState<Charge | null>(null);
  const [busy, setBusy] = useState(false);
  const [operationId, setOperationId] = useState('');
  const [operatorId, setOperatorId] = useState('');
  const [amount, setAmount] = useState('100');
  const [netAmount, setNetAmount] = useState('95');
  const [landingAccountId, setLandingAccountId] = useState('');

  const load = useCallback(async () => {
    const result = await listCharges();
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }
    setItems(result.data.items ?? []);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function select(id: string) {
    const result = await getCharge(id);
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Cobrança indisponível.' : result.error);
      return;
    }
    setSelected(result.data);
  }

  async function handleCreate() {
    setBusy(true);
    const result = await createCharge({
      operationId: operationId.trim(),
      operatorMemberId: operatorId.trim() || undefined,
      grossAmount: Number(amount),
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Cobrança gerada.');
    await load();
    if (result.data?.chargeId) await select(result.data.chargeId);
  }

  async function handlePaid() {
    if (!selected) return;
    setBusy(true);
    const result = await markChargePaid(selected.chargeId);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Paga.');
    await load();
    await select(selected.chargeId);
  }

  async function handleMaterialize() {
    if (!selected) return;
    setBusy(true);
    const result = await materializeCharge({
      chargeId: selected.chargeId,
      netAmount: Number(netAmount),
      landingWorldAccountId: landingAccountId.trim(),
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Materializada.');
    await load();
    await select(selected.chargeId);
  }

  async function handleTerminal(target: string) {
    if (!selected) return;
    setBusy(true);
    const result = await transitionCharge(selected.chargeId, target);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success(target);
    await load();
    await select(selected.chargeId);
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Cobranças"
        description="Geração com snapshot de split. Paga é fato externo; materialização cria Claims no ledger."
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.2fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Receipt className="size-4" />
              Lista
            </CardTitle>
            <CardDescription>Operação Ativa + operador assigned + trilho com quota.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <Input value={operationId} onChange={(e) => setOperationId(e.target.value)} placeholder="Operation id" />
            <Input value={operatorId} onChange={(e) => setOperatorId(e.target.value)} placeholder="Operator account id" />
            <div className="flex gap-2">
              <Input value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="Valor bruto" />
              <Button type="button" disabled={busy || !operationId.trim()} onClick={() => void handleCreate()}>
                Gerar
              </Button>
            </div>
            <Separator />
            <div className="space-y-2">
              {items.map((item) => (
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
                    <span className="font-mono text-xs">{item.chargeId.slice(0, 8)}…</span>
                    <Badge variant="secondary">{item.status}</Badge>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">{item.grossAmount} {item.currency}</p>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardHeader>
              <CardTitle className="text-base">Detalhe</CardTitle>
              <CardDescription className="font-mono text-xs">{selected.chargeId}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <p className="text-sm">Rail {selected.emissionRailId}</p>
              <p className="text-sm">Laranja {selected.orangeMemberId}</p>
              <p className="text-xs text-muted-foreground">{selected.externalReference}</p>
              <Separator />
              <h3 className="text-sm font-medium">Split (snapshot)</h3>
              <ul className="space-y-1 text-xs">
                {selected.splitIntent.lines.map((line) => (
                  <li key={line.order}>
                    {line.order}. {line.kind} · {line.percentOfRemainder}% da base
                  </li>
                ))}
              </ul>
              {selected.status === 'Paid' ? (
                <div className="space-y-2">
                  <Input value={netAmount} onChange={(e) => setNetAmount(e.target.value)} placeholder="Líquido X" />
                  <Input value={landingAccountId} onChange={(e) => setLandingAccountId(e.target.value)} placeholder="Conta de aterrissagem" />
                  <Button type="button" size="sm" disabled={busy || !landingAccountId.trim()} onClick={() => void handleMaterialize()}>
                    Materializar
                  </Button>
                </div>
              ) : null}
              {selected.status === 'Open' ? (
                <div className="flex flex-wrap gap-2">
                  <Button type="button" size="sm" disabled={busy} onClick={() => void handlePaid()}>Marcar Paga</Button>
                  <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleTerminal('Cancelled')}>Cancelar</Button>
                  <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleTerminal('Expired')}>Expirar</Button>
                  <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleTerminal('Failed')}>Falhou</Button>
                </div>
              ) : null}
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden border-dashed lg:flex">
            <CardContent className="flex flex-1 items-center justify-center p-8 text-sm text-muted-foreground">
              Selecione uma cobrança.
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
