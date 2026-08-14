import { useCallback, useEffect, useMemo, useState } from 'react';
import { type ColumnDef } from '@tanstack/react-table';
import { FileText, RefreshCw } from 'lucide-react';
import { getMyStatement, type StatementLine } from '@/api/authenticated/ledger';
import { DataTable } from '@/components/data/data-table';
import { PageHeader } from '@/components/layout/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { reportError } from '@/feedback';

function formatMoney(amount: number | null | undefined, currency: string | null | undefined): string {
  if (amount == null) return '—';
  const code = currency?.trim() || 'BRL';
  return `${amount.toLocaleString('pt-BR')} ${code}`;
}

function phaseLabel(phase: string): string {
  const map: Record<string, string> = {
    estimate: 'Estimativa',
    pending: 'Pendente',
    loss: 'Perda',
    revealed: 'Pendente',
  };
  return map[phase] ?? phase;
}

function phaseVariant(phase: string): 'secondary' | 'outline' | 'destructive' {
  if (phase === 'loss') return 'destructive';
  if (phase === 'estimate') return 'outline';
  return 'secondary';
}

export function StatementPage() {
  const [items, setItems] = useState<StatementLine[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    const result = await getMyStatement();
    setLoading(false);
    if (!result.ok || !result.data) {
      const message = result.ok ? 'Resposta inválida.' : result.error;
      setLoadError(message);
      reportError(message);
      return;
    }
    setLoadError(null);
    setItems(result.data.items ?? []);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const columns = useMemo<ColumnDef<StatementLine>[]>(() => [
    {
      accessorKey: 'originChargeId',
      header: 'Cobrança',
      cell: ({ row }) => (
        <span className="font-medium tabular-nums" title={row.original.originChargeId}>
          {row.original.originChargeId.slice(0, 8)}
        </span>
      ),
    },
    {
      accessorKey: 'phase',
      header: 'Fase',
      cell: ({ row }) => (
        <Badge variant={phaseVariant(row.original.phase)}>{phaseLabel(row.original.phase)}</Badge>
      ),
    },
    {
      id: 'estimate',
      header: 'Estimativa',
      cell: ({ row }) => (
        <span className="tabular-nums">
          {formatMoney(row.original.estimateAmount, row.original.estimateCurrency)}
          {row.original.audience === 'agency' ? ' · agenciado' : ''}
        </span>
      ),
    },
    {
      id: 'pending',
      header: 'Pendente',
      cell: ({ row }) => (
        row.original.phase === 'estimate'
          ? <span className="text-muted-foreground">—</span>
          : (
            <span className="tabular-nums">
              {formatMoney(row.original.releasedAmount, row.original.releasedCurrency)}
            </span>
          )
      ),
    },
    {
      accessorKey: 'summary',
      header: 'Resumo',
      cell: ({ row }) => (
        <span className="line-clamp-2 text-muted-foreground">{row.original.summary || '—'}</span>
      ),
    },
  ], []);

  return (
    <div className="min-w-0 space-y-5">
      <PageHeader
        kicker="Extrato"
        title="Meu extrato"
        description="O que você tem a receber por cobrança. Estimativa entra primeiro; Pendente só depois que o Contador revelar."
        actions={(
          <Button type="button" size="sm" variant="outline" disabled={loading} onClick={() => void load()}>
            <RefreshCw data-icon="inline-start" className={loading ? 'animate-spin' : undefined} />
            Atualizar
          </Button>
        )}
      />
      <Card className="border-border/60 bg-card/90">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <FileText className="size-4" />
            Por cobrança
          </CardTitle>
          <CardDescription>
            Estimativa é o valor esperado. Pendente é o valor já revelado pelo Contador. O valor ao vivo da rota não entra aqui.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <DataTable
            columns={columns}
            data={items}
            loading={loading}
            errorMessage={loadError}
            emptyMessage="Nenhum movimento visível. Quando uma cobrança entrar no seu extrato, a coluna Estimativa mostra o valor esperado. Pendente só aparece depois que o Contador revelar — até lá, não há o que sacar daqui."
            getRowId={(row) => row.originChargeId}
          />
        </CardContent>
      </Card>
    </div>
  );
}
