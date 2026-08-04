import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';

type ScriptInventoryKpisProps = {
  total: number;
  hostScoped: number;
  nameOnly: number;
  missingProd: number;
};

export function ScriptInventoryKpis({ total, hostScoped, nameOnly, missingProd }: ScriptInventoryKpisProps) {
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4" role="list" aria-label="Resumo do inventário">
      <KpiItem value={total} label="Scripts" />
      <KpiItem value={hostScoped} label="Por host" />
      <KpiItem value={nameOnly} label="Só por nome" />
      <KpiItem value={missingProd} label="Sem prod" warn />
    </div>
  );
}

function KpiItem({ value, label, warn }: { value: number; label: string; warn?: boolean }) {
  return (
    <Card
      role="listitem"
      className={cn(
        'py-3 transition-colors',
        warn
          ? 'border-warning/30 bg-warning/5 hover:border-warning/45'
          : 'border-warning/15 bg-card/70 hover:border-warning/30 hover:bg-muted/50',
      )}
    >
      <CardContent className="flex flex-col gap-0.5 px-3.5 py-0">
        <span className="text-xl font-bold leading-tight text-warning">{value}</span>
        <span className="text-[0.72rem] leading-tight text-muted-foreground">{label}</span>
      </CardContent>
    </Card>
  );
}
