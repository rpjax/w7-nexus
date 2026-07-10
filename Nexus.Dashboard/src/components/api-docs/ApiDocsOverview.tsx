import { API_FLOWS, API_GROUPS, API_ENDPOINTS } from '../../features/api-docs/catalog';
import type { ApiDocsView } from '../../features/api-docs/types';

type ApiDocsOverviewProps = {
  onNavigate: (view: ApiDocsView) => void;
};

const QUICK_START = [
  {
    step: '1',
    title: 'Autentique-se',
    text: 'Obtenha um JWT via sign-in e envie em Authorization: Bearer em todas as rotas protegidas.',
    flowId: 'auth-jwt',
  },
  {
    step: '2',
    title: 'Estruture a operação',
    text: 'Crie operação, equipes e atribua operadores antes de qualquer cobrança.',
    flowId: 'operations-setup',
  },
  {
    step: '3',
    title: 'Configure o gateway',
    text: 'Cadastre credenciais Frendz, Wintech ou SigiloPay e vincule à operação.',
    flowId: 'gateways-setup',
  },
  {
    step: '4',
    title: 'Gere e acompanhe PIX',
    text: 'Crie cobranças e use SignalR para status em tempo real.',
    flowId: 'payments-pix',
  },
];

export function ApiDocsOverview({ onNavigate }: ApiDocsOverviewProps) {
  return (
    <div className="api-docs-overview">
      <header className="api-docs-hero">
        <p className="api-docs-hero__kicker">Nexus API · v1</p>
        <h2 className="api-docs-hero__title">Guia completo de integração</h2>
        <p className="api-docs-hero__subtitle">
          Esta documentação explica <em>como</em> usar a API — não apenas lista endpoints.
          Comece pelos fluxos guiados para entender o contexto de cada chamada, ou navegue
          pela referência técnica quando já souber o que precisa.
        </p>
        <div className="api-docs-hero__stats">
          <div className="api-docs-stat">
            <span className="api-docs-stat__value">{API_FLOWS.length}</span>
            <span className="api-docs-stat__label">Fluxos explicativos</span>
          </div>
          <div className="api-docs-stat">
            <span className="api-docs-stat__value">{API_ENDPOINTS.length}</span>
            <span className="api-docs-stat__label">Endpoints</span>
          </div>
          <div className="api-docs-stat">
            <span className="api-docs-stat__value">{API_GROUPS.length}</span>
            <span className="api-docs-stat__label">Domínios</span>
          </div>
        </div>
      </header>

      <section className="api-docs-panel">
        <h3 className="api-docs-panel__title">Por onde começar?</h3>
        <p className="api-docs-panel__lead muted">
          Sequência recomendada para colocar um ambiente Nexus em produção.
        </p>
        <ol className="api-quick-start">
          {QUICK_START.map((item) => (
            <li key={item.step} className="api-quick-start__item">
              <span className="api-quick-start__num">{item.step}</span>
              <div className="api-quick-start__body">
                <strong>{item.title}</strong>
                <p className="muted">{item.text}</p>
                <button
                  type="button"
                  className="api-quick-start__link"
                  onClick={() => onNavigate({ kind: 'flow', id: item.flowId })}
                >
                  Ver fluxo guiado →
                </button>
              </div>
            </li>
          ))}
        </ol>
      </section>

      <section className="api-docs-panel">
        <h3 className="api-docs-panel__title">Fluxos guiados</h3>
        <p className="api-docs-panel__lead muted">
          Cada fluxo explica o contexto, armadilhas comuns e referência técnica sob demanda.
        </p>
        <div className="api-flow-grid">
          {API_FLOWS.map((flow) => (
            <button
              key={flow.id}
              type="button"
              className={`api-flow-card api-flow-card--${flow.accent}`}
              onClick={() => onNavigate({ kind: 'flow', id: flow.id })}
            >
              <span className={`api-flow-dot api-flow-dot--${flow.accent}`} aria-hidden="true" />
              <span className="api-flow-card__title">{flow.title}</span>
              <span className="api-flow-card__desc">{flow.description}</span>
              <span className="api-flow-card__meta">
                {flow.steps.length} passos · ~{flow.estimatedMinutes} min
              </span>
            </button>
          ))}
        </div>
      </section>

      <div className="api-docs-columns">
        <section className="api-docs-panel">
          <h3 className="api-docs-panel__title">Autenticação</h3>
          <p className="muted">
            A API é stateless: cada requisição protegida precisa do JWT no header.
          </p>
          <pre className="api-inline-code">Authorization: Bearer {'{accessToken}'}</pre>
          <ul className="api-docs-list">
            <li><strong>Público</strong> — checkout PIX, resolução de scripts, OLX victim</li>
            <li><strong>JWT Bearer</strong> — painéis operador, admin e laranja</li>
            <li><strong>Token mestre</strong> — bootstrap do primeiro administrador</li>
          </ul>
          <button
            type="button"
            className="btn btn-small"
            onClick={() => onNavigate({ kind: 'flow', id: 'auth-jwt' })}
          >
            Entender autenticação →
          </button>
        </section>

        <section className="api-docs-panel">
          <h3 className="api-docs-panel__title">Códigos HTTP</h3>
          <div className="api-status-grid">
            <div className="api-status-item api-status-item--success"><span className="api-status-item__code">200</span><span className="api-status-item__label">Sucesso</span></div>
            <div className="api-status-item api-status-item--info"><span className="api-status-item__code">304</span><span className="api-status-item__label">Cache (ETag)</span></div>
            <div className="api-status-item api-status-item--warning"><span className="api-status-item__code">401</span><span className="api-status-item__label">Sem token</span></div>
            <div className="api-status-item api-status-item--warning"><span className="api-status-item__code">403</span><span className="api-status-item__label">Sem permissão</span></div>
            <div className="api-status-item api-status-item--danger"><span className="api-status-item__code">422</span><span className="api-status-item__label">Validação</span></div>
          </div>
        </section>
      </div>

      <section className="api-docs-panel">
        <h3 className="api-docs-panel__title">Referência por domínio</h3>
        <div className="api-domain-grid">
          {API_GROUPS.map((group) => (
            <button
              key={group.id}
              type="button"
              className="api-domain-card"
              onClick={() => onNavigate({ kind: 'group', id: group.id })}
            >
              <span className="api-domain-card__title">{group.title}</span>
              <span className="api-domain-card__desc">{group.description}</span>
            </button>
          ))}
        </div>
      </section>
    </div>
  );
}
