import { useCallback, useEffect, useState } from 'react';
import { Layers } from 'lucide-react';
import {
  assignOperator,
  configureOperationCut,
  createOperation,
  deleteStoreObject,
  getOperation,
  listOperations,
  listStoreObjects,
  registerScript,
  resolveScript,
  transitionOperation,
  unassignOperator,
  upsertStoreObject,
  type Operation,
  type StoreObject,
} from '@/api/administrator/operations';
import {
  bindEmissionRail,
  listEmissionRails,
  listOperationRails,
  unbindEmissionRail,
  type EmissionRail,
} from '@/api/administrator/charging';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

const TRANSITIONS: Record<string, string[]> = {
  Draft: ['Active', 'Closed'],
  Active: ['Paused', 'Closed', 'Draft'],
  Paused: ['Active', 'Closed'],
  Closed: [],
};

export function OperationsPage() {
  const [items, setItems] = useState<Operation[]>([]);
  const [selected, setSelected] = useState<Operation | null>(null);
  const [storeItems, setStoreItems] = useState<StoreObject[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [name, setName] = useState('');
  const [memberId, setMemberId] = useState('');
  const [cut, setCut] = useState('');
  const [scriptName, setScriptName] = useState('default');
  const [scriptBody, setScriptBody] = useState('// script');
  const [resolvedScript, setResolvedScript] = useState('');
  const [objectType, setObjectType] = useState('note');
  const [payloadJson, setPayloadJson] = useState('{"ok":true}');
  const [rails, setRails] = useState<EmissionRail[]>([]);
  const [boundRailIds, setBoundRailIds] = useState<string[]>([]);
  const [bindAccountId, setBindAccountId] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    const result = await listOperations();
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

  async function select(operationId: string) {
    const result = await getOperation(operationId);
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Operação indisponível.' : result.error);
      return;
    }
    setSelected(result.data);
    setCut(result.data.managementCutPercent?.toString() ?? '');
    const store = await listStoreObjects(operationId);
    if (store.ok && store.data) setStoreItems(store.data.items ?? []);
    else setStoreItems([]);
    const allRails = await listEmissionRails();
    if (allRails.ok && allRails.data) setRails(allRails.data.items ?? []);
    const bound = await listOperationRails(operationId);
    if (bound.ok && bound.data) setBoundRailIds(bound.data.railIds ?? []);
  }

  async function handleCreate() {
    setBusy(true);
    const result = await createOperation(name.trim());
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Operação criada.');
    setName('');
    await load();
    if (result.data?.operationId) await select(result.data.operationId);
  }

  async function handleTransition(target: string) {
    if (!selected) return;
    setBusy(true);
    const result = await transitionOperation(selected.operationId, target);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success(`Status → ${target}`);
    await load();
    await select(selected.operationId);
  }

  async function handleCut() {
    if (!selected) return;
    setBusy(true);
    const value = cut.trim() === '' ? null : Number(cut);
    const result = await configureOperationCut(selected.operationId, value);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Cut salvo.');
    await select(selected.operationId);
  }

  async function handleAssign() {
    if (!selected) return;
    setBusy(true);
    const result = await assignOperator(selected.operationId, memberId.trim());
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Operador assigned.');
    setMemberId('');
    await select(selected.operationId);
  }

  async function handleUnassign(id: string) {
    if (!selected) return;
    setBusy(true);
    const result = await unassignOperator(selected.operationId, id);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Assign removido.');
    await select(selected.operationId);
  }

  async function handleScript() {
    if (!selected) return;
    setBusy(true);
    const result = await registerScript(selected.operationId, scriptName.trim(), scriptBody);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Script registrado.');
  }

  async function handleResolve() {
    if (!selected) return;
    setBusy(true);
    const result = await resolveScript(selected.operationKey);
    setBusy(false);
    if (!result.ok || !result.data) {
      toast.error(result.ok ? 'Sem script.' : result.error);
      setResolvedScript('');
      return;
    }
    setResolvedScript(result.data.body);
    toast.success('Script resolvido.');
  }

  async function handleStoreUpsert() {
    if (!selected) return;
    setBusy(true);
    const result = await upsertStoreObject(selected.operationId, {
      objectType: objectType.trim(),
      payloadJson,
    });
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Objeto salvo.');
    await select(selected.operationId);
  }

  async function handleStoreDelete(objectId: string) {
    if (!selected) return;
    setBusy(true);
    const result = await deleteStoreObject(selected.operationId, objectId);
    setBusy(false);
    if (!result.ok) {
      toast.error(result.error);
      return;
    }
    toast.success('Objeto removido.');
    await select(selected.operationId);
  }

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Administração"
        kickerVariant="admin"
        title="Operações"
        description="Ciclo de vida, assign, operation key, Script e Store. Sem entidade Equipe."
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.2fr)]">
        <Card className="border-border/60 bg-card/90">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Layers className="size-4" />
              Lista
            </CardTitle>
            <CardDescription>Crie e selecione uma frente.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex gap-2">
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome da operação" />
              <Button type="button" disabled={busy || !name.trim()} onClick={() => void handleCreate()}>
                Criar
              </Button>
            </div>
            <Separator />
            {loading ? <p className="text-sm text-muted-foreground">Carregando…</p> : null}
            <div className="space-y-2">
              {items.map((item) => (
                <button
                  key={item.operationId}
                  type="button"
                  className={cn(
                    'w-full rounded-lg border border-border/60 px-3 py-2 text-left text-sm transition-colors hover:bg-muted/40',
                    selected?.operationId === item.operationId && 'border-primary/40 bg-muted/30',
                  )}
                  onClick={() => void select(item.operationId)}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">{item.name}</span>
                    <Badge variant="secondary">{item.status}</Badge>
                  </div>
                  <p className="mt-1 truncate font-mono text-[0.65rem] text-muted-foreground">{item.operationKey}</p>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {selected ? (
          <Card className="border-border/60 bg-card/90">
            <CardHeader>
              <CardTitle className="text-base">{selected.name}</CardTitle>
              <CardDescription className="font-mono text-xs">{selected.operationKey}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-5">
              <section className="space-y-2">
                <h3 className="text-sm font-medium">Ciclo</h3>
                <div className="flex flex-wrap gap-2">
                  {(TRANSITIONS[selected.status] ?? []).map((target) => (
                    <Button key={target} type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleTransition(target)}>
                      → {target}
                    </Button>
                  ))}
                </div>
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Cut de gestão (stub)</h3>
                <div className="flex gap-2">
                  <Input value={cut} onChange={(e) => setCut(e.target.value)} placeholder="% opcional" />
                  <Button type="button" size="sm" disabled={busy} onClick={() => void handleCut()}>Salvar</Button>
                </div>
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Assign Operador</h3>
                <div className="flex gap-2">
                  <Input value={memberId} onChange={(e) => setMemberId(e.target.value)} placeholder="Account id (UUID)" />
                  <Button type="button" size="sm" disabled={busy || !memberId.trim()} onClick={() => void handleAssign()}>
                    Assign
                  </Button>
                </div>
                <div className="space-y-1">
                  {selected.assignedOperatorIds.map((id) => (
                    <div key={id} className="flex items-center justify-between gap-2 rounded border border-border/60 px-2 py-1.5 text-xs">
                      <span className="truncate font-mono">{id}</span>
                      <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleUnassign(id)}>
                        Remover
                      </Button>
                    </div>
                  ))}
                </div>
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Contas de Gateway (emissão)</h3>
                <div className="flex gap-2">
                  <Input value={bindAccountId} onChange={(e) => setBindAccountId(e.target.value)} placeholder="World account id" />
                  <Button
                    type="button"
                    size="sm"
                    disabled={busy || !bindAccountId.trim()}
                    onClick={async () => {
                      setBusy(true);
                      const bound = await bindEmissionRail(selected.operationId, bindAccountId.trim());
                      setBusy(false);
                      if (!bound.ok) {
                        toast.error(bound.error);
                        return;
                      }
                      toast.success('Conta ligada.');
                      setBindAccountId('');
                      await select(selected.operationId);
                    }}
                  >
                    Ligar
                  </Button>
                </div>
                <div className="space-y-1">
                  {rails
                    .filter((rail) => !boundRailIds.includes(rail.railId))
                    .map((rail) => (
                      <div key={rail.railId} className="flex items-center justify-between gap-2 rounded border border-border/60 px-2 py-1.5 text-xs">
                        <span className="truncate font-mono">{rail.railId} · quota {rail.quotaRemaining}</span>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={busy}
                          onClick={async () => {
                            setBusy(true);
                            const result = await bindEmissionRail(selected.operationId, rail.railId);
                            setBusy(false);
                            if (!result.ok) {
                              toast.error(result.error);
                              return;
                            }
                            await select(selected.operationId);
                          }}
                        >
                          Ligar
                        </Button>
                      </div>
                    ))}
                  {rails.filter((rail) => boundRailIds.includes(rail.railId)).map((rail) => (
                    <div key={rail.railId} className="flex items-center justify-between gap-2 rounded border border-border/60 px-2 py-1.5 text-xs">
                      <span className="truncate font-mono">{rail.railId} · quota {rail.quotaRemaining}</span>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        disabled={busy}
                        onClick={async () => {
                          setBusy(true);
                          const result = await unbindEmissionRail(selected.operationId, rail.railId);
                          setBusy(false);
                          if (!result.ok) {
                            toast.error(result.error);
                            return;
                          }
                          await select(selected.operationId);
                        }}
                      >
                        Desligar
                      </Button>
                    </div>
                  ))}
                </div>
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Script</h3>
                <Input value={scriptName} onChange={(e) => setScriptName(e.target.value)} />
                <Textarea value={scriptBody} onChange={(e) => setScriptBody(e.target.value)} rows={4} />
                <div className="flex gap-2">
                  <Button type="button" size="sm" disabled={busy} onClick={() => void handleScript()}>Registrar</Button>
                  <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleResolve()}>
                    Resolver (edge)
                  </Button>
                </div>
                {resolvedScript ? (
                  <pre className="overflow-auto rounded bg-muted/40 p-2 text-xs">{resolvedScript}</pre>
                ) : null}
              </section>

              <Separator />

              <section className="space-y-2">
                <h3 className="text-sm font-medium">Store</h3>
                <div className="grid gap-2 sm:grid-cols-2">
                  <div className="space-y-1">
                    <Label>Tipo</Label>
                    <Input value={objectType} onChange={(e) => setObjectType(e.target.value)} />
                  </div>
                </div>
                <Textarea value={payloadJson} onChange={(e) => setPayloadJson(e.target.value)} rows={3} />
                <Button type="button" size="sm" disabled={busy} onClick={() => void handleStoreUpsert()}>
                  Salvar objeto
                </Button>
                <div className="space-y-1">
                  {storeItems.map((item) => (
                    <div key={item.objectId} className="rounded border border-border/60 px-2 py-1.5 text-xs">
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-medium">{item.objectType}</span>
                        <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => void handleStoreDelete(item.objectId)}>
                          Remover
                        </Button>
                      </div>
                      <p className="mt-1 truncate font-mono text-muted-foreground">{item.objectId}</p>
                    </div>
                  ))}
                </div>
              </section>
            </CardContent>
          </Card>
        ) : (
          <Card className="hidden border-dashed border-border/60 bg-card/50 lg:flex">
            <CardContent className="flex flex-1 items-center justify-center p-8 text-sm text-muted-foreground">
              Selecione uma operação.
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
