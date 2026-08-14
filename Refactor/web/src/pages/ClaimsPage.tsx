import { useCallback, useEffect, useState } from 'react';
import { ScrollText } from 'lucide-react';
import {
  listClaims,
  listHops,
  registerHop,
  repassClaims,
  type LedgerClaim,
  type LedgerHop,
} from '@/api/administrator/ledger';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { toast } from 'sonner';

export function ClaimsPage() {
  const [items, setItems] = useState<LedgerClaim[]>([]);
  const [hops, setHops] = useState<LedgerHop[]>([]);
  const [chargeId, setChargeId] = useState('');
  const [accountId, setAccountId] = useState('');
  const [beneficiaryId, setBeneficiaryId] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [originAccountId, setOriginAccountId] = useState('');
  const [currency, setCurrency] = useState('BRL');
  const [destAccountId, setDestAccountId] = useState('');
  const [destAmount, setDestAmount] = useState('');
  const [destCurrency, setDestCurrency] = useState('BRL');
  const [cutPercent, setCutPercent] = useState('');
  const [cutOrangeId, setCutOrangeId] = useState('');
  const [cutOrangeAccountId, setCutOrangeAccountId] = useState('');
  const [cutInPlace, setCutInPlace] = useState(false);
  const [payoutAccountId, setPayoutAccountId] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    const result = await listClaims({
      chargeId: chargeId.trim() || undefined,
      accountId: accountId.trim() || undefined,
      beneficiaryId: beneficiaryId.trim() || undefined,
    });
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Resposta inválida.' : result.error);
      return;
    }
    setItems(result.data.items ?? []);
    const hopResult = await listHops(originAccountId.trim() || accountId.trim() || undefined);
    if (hopResult.ok && hopResult.data) setHops(hopResult.data.items ?? []);
  }, [accountId, beneficiaryId, chargeId, originAccountId]);

  useEffect(() => {
    void load();
  }, [load]);

  function toggle(id: string) {
    setSelected((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]));
  }

  async function handleHop() {
    setBusy(true);
    const result = await registerHop({
      originAccountId: originAccountId.trim(),
      currency: currency.trim() || 'BRL',
      claimIds: selected.length > 0 ? selected : undefined,
      destinations: destAccountId.trim()
        ? [{ accountId: destAccountId.trim(), amount: Number(destAmount), currency: destCurrency.trim() || 'BRL' }]
        : [],
      cut: cutPercent.trim()
        ? {
            orangeMemberId: cutOrangeId.trim(),
            percent: Number(cutPercent),
            inPlace: cutInPlace,
            orangeAccountId: cutInPlace ? undefined : cutOrangeAccountId.trim() || undefined,
          }
        : undefined,
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Hop registrado.');
    setSelected([]);
    await load();
  }

  async function handleRepass() {
    setBusy(true);
    const result = await repassClaims({
      originAccountId: originAccountId.trim(),
      claimIds: selected.length > 0 ? selected : undefined,
      payoutAccountId: payoutAccountId.trim(),
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Repasse registrado.');
    setSelected([]);
    await load();
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Claims"
        description="Ledger: hops origem→destino, cut mid-path e repasse para Conta Payout."
      />
      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <ScrollText className="size-4" />
            Filtros
          </CardTitle>
          <CardDescription>Por Cobrança, Conta ou beneficiário. Selecione claims para hop ou repasse.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="grid gap-2 sm:grid-cols-3">
            <Input value={chargeId} onChange={(e) => setChargeId(e.target.value)} placeholder="Charge id" />
            <Input value={accountId} onChange={(e) => setAccountId(e.target.value)} placeholder="World account id" />
            <Input value={beneficiaryId} onChange={(e) => setBeneficiaryId(e.target.value)} placeholder="Beneficiary id" />
          </div>
          <Button type="button" size="sm" onClick={() => void load()}>
            Filtrar
          </Button>
          <div className="space-y-2">
            {items.map((item) => (
              <label key={item.claimId} className="flex cursor-pointer gap-2 rounded-lg border border-border/60 px-3 py-2 text-sm">
                <input
                  type="checkbox"
                  checked={selected.includes(item.claimId)}
                  onChange={() => toggle(item.claimId)}
                  className="mt-1"
                />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-mono text-xs">{item.claimId.slice(0, 8)}…</span>
                    <Badge variant="secondary">{item.kind}</Badge>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {item.amount} {item.currency} · {item.status}
                  </p>
                  <p className="truncate font-mono text-[0.65rem] text-muted-foreground">{item.beneficiaryId}</p>
                </div>
              </label>
            ))}
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <Input value={originAccountId} onChange={(e) => setOriginAccountId(e.target.value)} placeholder="Origem (world account id)" />
            <Input value={currency} onChange={(e) => setCurrency(e.target.value)} placeholder="Moeda origem" />
            <Input value={destAccountId} onChange={(e) => setDestAccountId(e.target.value)} placeholder="Destino (world account id)" />
            <Input value={destAmount} onChange={(e) => setDestAmount(e.target.value)} placeholder="Valor destino" />
            <Input value={destCurrency} onChange={(e) => setDestCurrency(e.target.value)} placeholder="Moeda destino" />
            <Input value={cutPercent} onChange={(e) => setCutPercent(e.target.value)} placeholder="Cut % (opcional)" />
            <Input value={cutOrangeId} onChange={(e) => setCutOrangeId(e.target.value)} placeholder="Laranja (member id)" />
            <Input
              value={cutOrangeAccountId}
              onChange={(e) => setCutOrangeAccountId(e.target.value)}
              placeholder="Conta do Laranja"
              disabled={cutInPlace}
            />
            <Input value={payoutAccountId} onChange={(e) => setPayoutAccountId(e.target.value)} placeholder="Payout (repasse)" />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={cutInPlace} onChange={(e) => setCutInPlace(e.target.checked)} />
            Cut in-place
          </label>
          <div className="flex flex-wrap gap-2">
            <Button type="button" size="sm" disabled={busy} onClick={() => void handleHop()}>
              Registrar hop
            </Button>
            <Button type="button" size="sm" variant="secondary" disabled={busy} onClick={() => void handleRepass()}>
              Repasse
            </Button>
          </div>
          {hops.length > 0 ? (
            <div className="space-y-2">
              <p className="text-sm font-medium">Hops</p>
              {hops.map((hop) => (
                <p key={hop.hopId} className="font-mono text-xs text-muted-foreground">
                  {hop.hopId.slice(0, 8)}… perda {hop.lossAmount} · destinos {hop.destinations.length}
                </p>
              ))}
            </div>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
