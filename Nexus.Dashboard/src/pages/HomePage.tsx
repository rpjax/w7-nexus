import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchGatewayCredentials } from '../api/gateways';
import { searchOperations } from '../api/operations';
import { StatCard } from '../components/StatCard';

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
  const [operationsTotal, setOperationsTotal] = useState(0);
  const [frendz, setFrendz] = useState<CredentialStat>(defaultStat);
  const [sigiloPay, setSigiloPay] = useState<CredentialStat>(defaultStat);
  const [wintech, setWintech] = useState<CredentialStat>(defaultStat);

  useEffect(() => {
    void (async () => {
      const ops = await searchOperations({ limit: 1, offset: 0, keyword: null });
      if (ops.ok) setOperationsTotal(ops.data?.total ?? 0);

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
  }, []);

  return (
    <>
      <section className="page-header">
        <h1>Visão geral</h1>
        <p>Visibilidade operacional e acesso rápido aos módulos críticos do Nexus.</p>
      </section>

      <section className="stats-grid">
        <StatCard label="Operações registradas" value={operationsTotal.toString()} caption="Total no repositório" tone="info" />
        <StatCard label="Credencial Frendz" value={frendz.status} caption={frendz.caption} tone={frendz.tone} />
        <StatCard label="Credencial SigiloPay" value={sigiloPay.status} caption={sigiloPay.caption} tone={sigiloPay.tone} />
        <StatCard label="Credencial Wintech" value={wintech.status} caption={wintech.caption} tone={wintech.tone} />
        <StatCard label="Ambiente" value="Interno" caption="Painel sem autenticação (uso interno)" tone="warn" />
      </section>

      <section className="card">
        <h2>Atalhos</h2>
        <div className="quick-actions">
          <Link className="quick-action" to="/dashboard/operations">Nova operação</Link>
          <Link className="quick-action" to="/dashboard/accounts">Contas</Link>
          <Link className="quick-action" to="/dashboard/payments">Pagamentos</Link>
          <Link className="quick-action" to="/dashboard/payments/pix">Gerar cobrança PIX</Link>
          <Link className="quick-action" to="/dashboard/gateways/frendz">Credenciais Frendz</Link>
          <Link className="quick-action" to="/dashboard/gateways/sigilopay">Credenciais SigiloPay</Link>
          <Link className="quick-action" to="/dashboard/gateways/wintech">Credenciais Wintech</Link>
        </div>
      </section>
    </>
  );
}
