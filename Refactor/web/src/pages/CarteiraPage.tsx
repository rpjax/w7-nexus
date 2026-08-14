import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { type ColumnDef } from '@tanstack/react-table';
import { Copy, Handshake, Users } from 'lucide-react';
import { searchAdministratorAccounts } from '@/api/administrator/accounts';
import { grantMandatePreset, MANDATE_PRESETS, presetLabel } from '@/api/administrator/mandates';
import { getMyCarteira, type CarteiraDeal } from '@/api/authenticated/mandates';
import { useHubAccess } from '@/auth/MandateContext';
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
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { reportError, reportSuccess } from '@/feedback';

function shortId(id: string): string {
  return id.length > 10 ? `${id.slice(0, 8)}…` : id;
}

function humanizeGrantError(error: string): string {
  const text = error.toLowerCase();
  if (
    text.includes('agencydeal')
    || text.includes('pct=0')
    || (text.includes('operator') && text.includes('deal'))
  ) {
    return 'Para liberar Operador, crie o agenciamento primeiro em Agenciamento (vínculo Recrutador ↔ pessoa). Sem vínculo ativo, o papel não vale.';
  }
  return error;
}

export function CarteiraPage() {
  const access = useHubAccess();
  const [items, setItems] = useState<CarteiraDeal[]>([]);
  const [accounts, setAccounts] = useState<AccountDetails[]>([]);
  const [canListAccounts, setCanListAccounts] = useState(false);
  const [accountId, setAccountId] = useState('');
  const [presetId, setPresetId] = useState('Operator');
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [confirmGrant, setConfirmGrant] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    const result = await getMyCarteira();
    setLoading(false);
    if (!result.ok || !result.data) {
      const message = result.ok ? 'Resposta inválida.' : result.error;
      setLoadError(message);
      reportError(message);
      return;
    }
    setLoadError(null);
    setItems(result.data.items ?? []);

    const listed = await searchAdministratorAccounts({ limit: 200, offset: 0 });
    if (listed.ok && listed.data) {
      setCanListAccounts(true);
      setAccounts(listed.data.items ?? []);
    } else {
      setCanListAccounts(false);
      setAccounts([]);
      if (!listed.ok && listed.status !== 403) {
        reportError(listed.error);
      }
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function resolveName(id: string): string | null {
    return accounts.find((item) => item.id === id)?.username ?? null;
  }

  async function copyId(id: string) {
    try {
      await navigator.clipboard.writeText(id);
      reportSuccess('Identificador copiado.');
    } catch {
      reportError('Não foi possível copiar.');
    }
  }

  async function handleGrant() {
    setBusy(true);
    const result = await grantMandatePreset(accountId.trim(), presetId);
    setBusy(false);
    setConfirmGrant(false);
    if (!result.ok) {
      reportError(humanizeGrantError(result.error));
      return;
    }
    reportSuccess(`${presetLabel(presetId)} liberado para ${selectedLabel}.`);
    setAccountId('');
    await load();
  }

  const grantOptions = useMemo(() => {
    const fromDownline = items.map((item) => ({
      id: item.operatorAccountId,
      label: resolveName(item.operatorAccountId) ?? shortId(item.operatorAccountId),
    }));
    if (canListAccounts) {
      return accounts.map((account) => ({ id: account.id, label: account.username }));
    }
    const unique = new Map(fromDownline.map((item) => [item.id, item]));
    return [...unique.values()];
  }, [accounts, canListAccounts, items]);

  const columns = useMemo<ColumnDef<CarteiraDeal>[]>(() => [
    {
      accessorKey: 'operatorAccountId',
      header: 'Pessoa',
      cell: ({ row }) => {
        const name = resolveName(row.original.operatorAccountId);
        return (
          <div className="flex min-w-0 items-center gap-1.5">
            <span className="truncate font-medium">
              {name ?? shortId(row.original.operatorAccountId)}
            </span>
            {!name ? (
              <Button
                type="button"
                size="icon"
                variant="ghost"
                className="size-6"
                aria-label="Copiar identificador"
                onClick={(event) => {
                  event.stopPropagation();
                  void copyId(row.original.operatorAccountId);
                }}
              >
                <Copy className="size-3.5" />
              </Button>
            ) : null}
          </div>
        );
      },
    },
    {
      id: 'share',
      header: 'Fatia',
      cell: ({ row }) => (
        <span className="tabular-nums">
          Operador {row.original.operatorPercent}% · Recrutador {row.original.recruiterPercent}%
        </span>
      ),
    },
    {
      id: 'deal',
      header: '',
      cell: ({ row }) => (
        <Button variant="link" size="sm" className="h-auto px-0" asChild>
          <Link
            to={`/dashboard/deals?operator=${encodeURIComponent(row.original.operatorAccountId)}`}
            onClick={(event) => event.stopPropagation()}
          >
            Abrir agenciamento
          </Link>
        </Button>
      ),
    },
  ], [accounts]);

  const selectedLabel = grantOptions.find((item) => item.id === accountId)?.label ?? shortId(accountId);
  const empty = !loading && !loadError && items.length === 0;

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Carteira"
        title="Minha carteira"
        description="Pessoas que você agenciou. Sem organograma global."
      />
      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Users className="size-4" />
            Sua downline
          </CardTitle>
          <CardDescription>
            Fatia Recrutador × Operador. Só quem você agenciou — o cadastro global fica em Agenciamento.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4 p-0">
          <DataTable
            columns={columns}
            data={items}
            loading={loading}
            errorMessage={loadError}
            emptyMessage="Ninguém na sua downline ainda. O próximo passo é agenciar."
            getRowId={(row) => row.dealId}
          />
          {empty ? (
            <div className="flex flex-wrap items-center gap-2 border-t border-border/60 px-4 py-3">
              <Button size="sm" asChild>
                <Link to="/dashboard/deals">
                  <Handshake className="size-3.5" />
                  Ir para Agenciamento
                </Link>
              </Button>
            </div>
          ) : null}
          {access.canGrant ? (
            <div className="grid gap-2 border-t border-border/60 p-4 sm:grid-cols-[1fr_10rem_auto] sm:items-end">
              <div className="space-y-1.5">
                <Label>{canListAccounts ? 'Pessoa' : 'Usuário'}</Label>
                <Select value={accountId} onValueChange={setAccountId}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder={canListAccounts ? 'Selecionar pessoa' : 'Downline'} />
                  </SelectTrigger>
                  <SelectContent>
                    {grantOptions.map((option) => (
                      <SelectItem key={option.id} value={option.id}>{option.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Papel</Label>
                <Select value={presetId} onValueChange={setPresetId}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {MANDATE_PRESETS.map((preset) => (
                      <SelectItem key={preset.id} value={preset.id}>{preset.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button
                type="button"
                size="sm"
                disabled={busy || !accountId}
                onClick={() => setConfirmGrant(true)}
              >
                Conceder…
              </Button>
            </div>
          ) : null}
        </CardContent>
      </Card>

      <Dialog open={confirmGrant} onOpenChange={setConfirmGrant}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Liberar papel</DialogTitle>
            <DialogDescription>
              Liberar {presetLabel(presetId)} para {selectedLabel}. Sem agenciamento ativo, Operador não vale — crie o vínculo em Agenciamento.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirmGrant(false)}>Cancelar</Button>
            <Button type="button" disabled={busy} onClick={() => void handleGrant()}>
              Confirmar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
