import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchAdministratorOperations } from '../api/administrator/operations';
import { searchOlxAdminAdPatches } from '../api/olx/admin';
import { searchOlxOperatorAdPatches } from '../api/olx/operator';
import { searchGatewayCredentials } from '../api/gateways';
import { searchOperatorOperations } from '../api/operator/operations';
import { useAuth } from '../auth/AuthContext';
import { canUseOperatorPanel, canUseOlxPanel, canUseStrawManPanel, isAdministrator, isOlxOperator } from '../auth/roles';
import { StatCard } from '../components/StatCard';
import { PageHeading } from '../layouts/PageHeading';

type CredentialStat = {
  status: string;
  caption: string;
  tone: 'info' | 'success' | 'danger' | 'warn';
};

function resolveCredentialStat(total: number, enabled: number): CredentialStat {
  const caption = total === 0
    ? 'Nenhuma credencial cadastrada'
    : `${enabled} habilitada(s) de ${total} cadastrada(s)`;

  if (total === 0) return { status: 'Ausente', caption, tone: 'danger' };
  if (enabled === 0) return { status: 'Desativadas', caption, tone: 'warn' };
  return { status: 'Pronto', caption, tone: 'success' };
}

const defaultStat: CredentialStat = {
  status: 'Verificando...',
  caption: 'Cadastradas vs habilitadas para cobrança',
  tone: 'info',
};

export function HomePage() {
  const { user } = useAuth();
  const operatorPanel = canUseOperatorPanel(user);
  const adminView = isAdministrator(user);
  const strawManPanel = canUseStrawManPanel(user);
  const olxPanel = canUseOlxPanel(user);
  const olxOperator = isOlxOperator(user);

  const [myOperationsTotal, setMyOperationsTotal] = useState<number | null>(null);
  const [systemOperationsTotal, setSystemOperationsTotal] = useState<number | null>(null);
  const [frendz, setFrendz] = useState<CredentialStat>(defaultStat);
  const [sigiloPay, setSigiloPay] = useState<CredentialStat>(defaultStat);
  const [wintech, setWintech] = useState<CredentialStat>(defaultStat);
  const [olxPatchesTotal, setOlxPatchesTotal] = useState<number | null>(null);

  useEffect(() => {
    void (async () => {
      if (operatorPanel) {
        const ops = await searchOperatorOperations({ limit: 1, offset: 0, keyword: null });
        if (ops.ok) setMyOperationsTotal(ops.data?.total ?? 0);
      }

      if (adminView) {
        const ops = await searchAdministratorOperations({ limit: 1, offset: 0, keyword: null });
        if (ops.ok) setSystemOperationsTotal(ops.data?.total ?? 0);
      }

      if (olxPanel) {
        const search = olxOperator
          ? searchOlxOperatorAdPatches
          : adminView
            ? searchOlxAdminAdPatches
            : null;
        if (search) {
          const patches = await search({ limit: 1, offset: 0, keyword: null, operationIds: [] });
          if (patches.ok) setOlxPatchesTotal(patches.data?.total ?? 0);
        }
      }

      if (!operatorPanel) return;

      async function loadGateway(
        prefix: 'frendz' | 'sigilopay' | 'wintech',
        setter: (stat: CredentialStat) => void,
      ) {
        const [totalRes, enabledRes] = await Promise.all([
          searchGatewayCredentials(prefix, { limit: 1, offset: 0, keyword: null }),
          searchGatewayCredentials(prefix, { limit: 1, offset: 0, keyword: null, enabledOnly: true }),
        ]);
        if (totalRes.ok && enabledRes.ok) {
          setter(resolveCredentialStat(totalRes.data?.total ?? 0, enabledRes.data?.total ?? 0));
        }
      }

      await Promise.all([
        loadGateway('frendz', setFrendz),
        loadGateway('sigilopay', setSigiloPay),
        loadGateway('wintech', setWintech),
      ]);
    })();
  }, [operatorPanel, adminView, olxPanel, olxOperator]);

  const homeKicker = adminView && operatorPanel
    ? 'Administração e operação'
    : adminView
      ? 'Administração'
      : olxOperator
        ? 'Painel OLX'
        : operatorPanel
          ? 'Painel do operador'
          : strawManPanel
            ? 'Painel laranja'
            : 'Dashboard';

  const homeSubtitle = adminView && operatorPanel
    ? 'Visão operacional das suas alocações e gestão global do sistema.'
    : adminView
      ? 'Gerencie o catálogo global de operações e contas do sistema.'
      : olxOperator
        ? 'Impersonação de anúncios OLX, patch de preços e liberação de slots.'
        : operatorPanel
          ? 'Acompanhe suas operações alocadas, pagamentos e credenciais de gateway.'
          : strawManPanel
            ? 'Acompanhe pagamentos e configurações da sua conta laranja.'
            : 'Nenhum papel operacional ou administrativo associado à sua sessão.';

  return (
    <>
      <PageHeading
        kicker={homeKicker}
        kickerVariant={adminView ? 'admin' : 'default'}
        title="Visão geral"
        subtitle={homeSubtitle}
      />

      <section className="stats-grid">
        {operatorPanel ? (
          <StatCard
            label="Minhas operações"
            value={myOperationsTotal?.toString() ?? '—'}
            caption="Operações em que você está alocado"
            tone="info"
          />
        ) : null}
        {adminView ? (
          <StatCard
            label="Operações no sistema"
            value={systemOperationsTotal?.toString() ?? '—'}
            caption="Total no repositório global"
            tone="info"
          />
        ) : null}
        {operatorPanel ? (
          <>
            <StatCard label="Credencial Frendz" value={frendz.status} caption={frendz.caption} tone={frendz.tone} />
            <StatCard label="Credencial SigiloPay" value={sigiloPay.status} caption={sigiloPay.caption} tone={sigiloPay.tone} />
            <StatCard label="Credencial Wintech" value={wintech.status} caption={wintech.caption} tone={wintech.tone} />
          </>
        ) : null}
        {olxPanel ? (
          <StatCard
            label="Anúncios OLX"
            value={olxPatchesTotal?.toString() ?? '—'}
            caption={olxOperator ? 'Patches sob seu controle' : 'Registros globais de patch'}
            tone="success"
          />
        ) : null}
        <StatCard
          label="Sessão"
          value={user?.username ?? '—'}
          caption={user?.roles.length ? user.roles.join(', ') : 'Autenticado'}
          tone="success"
        />
      </section>

      <section className="card">
        <h2>Atalhos</h2>
        <div className="quick-actions">
          {operatorPanel ? (
            <>
              <Link className="quick-action" to="/dashboard/operations">Minhas operações</Link>
              <Link className="quick-action" to="/dashboard/payments">Meus pagamentos</Link>
              <Link className="quick-action" to="/dashboard/payments/pix">Gerar cobrança PIX</Link>
              <Link className="quick-action" to="/dashboard/gateways/frendz">Credenciais Frendz</Link>
              <Link className="quick-action" to="/dashboard/gateways/sigilopay">Credenciais SigiloPay</Link>
              <Link className="quick-action" to="/dashboard/gateways/wintech">Credenciais Wintech</Link>
            </>
          ) : null}
          {adminView ? (
            <>
              <Link className="quick-action quick-action-admin" to="/dashboard/admin/operations">Todas as operações</Link>
              <Link className="quick-action quick-action-admin" to="/dashboard/admin/payments">Todos os pagamentos</Link>
              <Link className="quick-action quick-action-admin" to="/dashboard/accounts">Contas</Link>
              <Link className="quick-action quick-action-admin" to="/dashboard/admin/straw-men">Gestão de laranjas</Link>
            </>
          ) : null}
          {strawManPanel ? (
            <>
              <Link className="quick-action" to="/dashboard/straw-man/payments">Meus pagamentos</Link>
              <Link className="quick-action" to="/dashboard/straw-man/settings">Minhas configurações</Link>
            </>
          ) : null}
          {olxPanel ? (
            <>
              {olxOperator ? (
                <Link className="quick-action" to="/dashboard/olx/ads">OLX — Meus anúncios</Link>
              ) : null}
              {adminView ? (
                <Link className="quick-action quick-action-admin" to="/dashboard/olx/admin/ads">OLX — Gestão global</Link>
              ) : null}
            </>
          ) : null}
        </div>
      </section>
    </>
  );
}
