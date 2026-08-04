import { useQuery } from '@tanstack/react-query';
import { searchAdministratorOperations } from '@/api/administrator/operations';
import { searchOlxAdminAdPatches } from '@/api/olx/admin';
import { searchOlxOperatorAdPatches } from '@/api/olx/operator';
import { searchGatewayCredentials } from '@/api/gateways';
import { searchOperatorOperations } from '@/api/operations/operator';

type CredentialStat = {
  status: string;
  caption: string;
  tone: 'info' | 'success' | 'danger' | 'warn';
};

export const defaultCredentialStat: CredentialStat = {
  status: 'Verificando...',
  caption: 'Cadastradas vs habilitadas para cobrança',
  tone: 'info',
};

function resolveCredentialStat(total: number, enabled: number): CredentialStat {
  const caption = total === 0
    ? 'Nenhuma credencial cadastrada'
    : `${enabled} habilitada(s) de ${total} cadastrada(s)`;

  if (total === 0) return { status: 'Ausente', caption, tone: 'danger' };
  if (enabled === 0) return { status: 'Desativadas', caption, tone: 'warn' };
  return { status: 'Pronto', caption, tone: 'success' };
}

async function loadGatewayStat(prefix: 'frendz' | 'sigilopay' | 'wintech'): Promise<CredentialStat> {
  const [totalRes, enabledRes] = await Promise.all([
    searchGatewayCredentials(prefix, { limit: 1, offset: 0, keyword: null }),
    searchGatewayCredentials(prefix, { limit: 1, offset: 0, keyword: null, enabledOnly: true }),
  ]);

  if (totalRes.ok && enabledRes.ok) {
    return resolveCredentialStat(totalRes.data?.total ?? 0, enabledRes.data?.total ?? 0);
  }

  return defaultCredentialStat;
}

export type HomeMetrics = {
  myOperationsTotal: number | null;
  systemOperationsTotal: number | null;
  olxPatchesTotal: number | null;
  frendz: CredentialStat;
  sigiloPay: CredentialStat;
  wintech: CredentialStat;
};

type UseHomeMetricsOptions = {
  operatorPanel: boolean;
  adminView: boolean;
  olxPanel: boolean;
  olxOperator: boolean;
};

export function useHomeMetrics({
  operatorPanel,
  adminView,
  olxPanel,
  olxOperator,
}: UseHomeMetricsOptions) {
  return useQuery({
    queryKey: ['home-metrics', operatorPanel, adminView, olxPanel, olxOperator],
    queryFn: async (): Promise<HomeMetrics> => {
      const metrics: HomeMetrics = {
        myOperationsTotal: null,
        systemOperationsTotal: null,
        olxPatchesTotal: null,
        frendz: defaultCredentialStat,
        sigiloPay: defaultCredentialStat,
        wintech: defaultCredentialStat,
      };

      const tasks: Promise<void>[] = [];

      if (operatorPanel) {
        tasks.push(
          searchOperatorOperations({ limit: 1, offset: 0, keyword: null }).then((ops) => {
            if (ops.ok) metrics.myOperationsTotal = ops.data?.total ?? 0;
          }),
        );
      }

      if (adminView) {
        tasks.push(
          searchAdministratorOperations({ limit: 1, offset: 0, keyword: null }).then((ops) => {
            if (ops.ok) metrics.systemOperationsTotal = ops.data?.total ?? 0;
          }),
        );
      }

      if (olxPanel) {
        const search = olxOperator
          ? searchOlxOperatorAdPatches
          : adminView
            ? searchOlxAdminAdPatches
            : null;
        if (search) {
          tasks.push(
            search({ limit: 1, offset: 0, keyword: null, operationIds: [] }).then((patches) => {
              if (patches.ok) metrics.olxPatchesTotal = patches.data?.total ?? 0;
            }),
          );
        }
      }

      if (operatorPanel) {
        tasks.push(
          loadGatewayStat('frendz').then((stat) => { metrics.frendz = stat; }),
          loadGatewayStat('sigilopay').then((stat) => { metrics.sigiloPay = stat; }),
          loadGatewayStat('wintech').then((stat) => { metrics.wintech = stat; }),
        );
      }

      await Promise.all(tasks);
      return metrics;
    },
  });
}

export type { CredentialStat };
