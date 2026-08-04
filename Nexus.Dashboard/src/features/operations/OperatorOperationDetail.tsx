import type { OperationDetails } from '../../api/types';
import { formatDateTime, shortId } from '../../utils/format';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';

type OperatorOperationDetailProps = {
  operation: OperationDetails;
};

function formatPercent(value: number): string {
  const rounded = Math.round(value * 100) / 100;
  return Number.isInteger(rounded) ? `${rounded}%` : `${rounded.toFixed(1)}%`;
}

export function OperatorOperationDetail({ operation }: OperatorOperationDetailProps) {
  const description = operation.description?.trim();

  return (
    <Card className="overflow-hidden">
      <CardHeader className="border-b border-border/40 pb-4">
        <CardTitle className="text-base">Visão geral</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 pt-4">
        <div>
          <h3 className="text-lg font-semibold tracking-tight text-foreground">{operation.name}</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {description || 'Sem descrição cadastrada.'}
          </p>
        </div>
        <dl className="grid gap-3 sm:grid-cols-3">
          <div className="space-y-0.5">
            <dt className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">ID</dt>
            <dd className="font-mono text-sm" title={operation.id}>{shortId(operation.id, 24)}</dd>
          </div>
          <div className="space-y-0.5">
            <dt className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Criada</dt>
            <dd className="text-sm">{formatDateTime(operation.createdAt)}</dd>
          </div>
          <div className="space-y-0.5">
            <dt className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Atualizada</dt>
            <dd className="text-sm">{formatDateTime(operation.updatedAt)}</dd>
          </div>
        </dl>
      </CardContent>

      <Separator />

      <CardHeader className="border-b border-border/40 pb-4">
        <CardTitle className="text-base">Suas equipes</CardTitle>
        <p className="text-sm text-muted-foreground">
          Equipes em que você está alocado nesta operação.
        </p>
      </CardHeader>
      <CardContent className="pt-4">
        {operation.teams.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhuma equipe vinculada.</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {operation.teams.map((team) => {
              const teamRow = team as typeof team & { profitShareRule?: { cuts: { accountId: string; username: string; percentage: number }[] } };
              const cuts = teamRow.profitShareRule?.cuts ?? [];
              return (
                <li
                  key={team.id}
                  className="rounded-lg border border-border/40 bg-muted/20 p-4"
                >
                  <div className="mb-3">
                    <span className="font-medium text-foreground">{team.name}</span>
                    <span className="mt-0.5 block font-mono text-xs text-muted-foreground" title={team.id}>
                      Equipe · {shortId(team.id, 18)}
                    </span>
                  </div>
                  <div className="space-y-2">
                    <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Repasse
                    </span>
                    {cuts.length === 0 ? (
                      <p className="text-sm text-muted-foreground">
                        Sem repasse configurado para você nesta equipe.
                      </p>
                    ) : (
                      <ul className="space-y-1.5">
                        {cuts.map((cut) => (
                          <li
                            key={`${cut.accountId}-${cut.percentage}`}
                            className="flex items-center justify-between gap-2 text-sm"
                          >
                            <span className="truncate">{cut.username || shortId(cut.accountId, 18)}</span>
                            <span className="shrink-0 font-medium tabular-nums">{formatPercent(cut.percentage)}</span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
