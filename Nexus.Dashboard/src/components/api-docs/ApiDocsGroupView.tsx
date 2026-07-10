import { API_GROUPS, endpointsByGroup } from '../../features/api-docs/catalog';
import { MethodBadge } from './MethodBadge';
import { AuthBadge } from './AuthBadge';
import type { ApiDocsView } from '../../features/api-docs/types';

type ApiDocsGroupViewProps = {
  groupId: string;
  onNavigate: (view: ApiDocsView) => void;
};

export function ApiDocsGroupView({ groupId, onNavigate }: ApiDocsGroupViewProps) {
  const group = API_GROUPS.find((g) => g.id === groupId);
  const endpoints = endpointsByGroup(groupId);

  if (!group) {
    return (
      <div className="api-docs-empty">
        <p>Domínio não encontrado.</p>
      </div>
    );
  }

  return (
    <div className="api-docs-group">
      <header className="api-docs-group__header">
        <h2 className="api-docs-group__title">{group.title}</h2>
        <p className="api-docs-group__intro">{group.intro}</p>
        <span className="api-docs-group__count">{endpoints.length} endpoints documentados</span>
      </header>

      <div className="api-endpoint-list">
        {endpoints.map((endpoint) => (
          <button
            key={endpoint.id}
            type="button"
            className="api-endpoint-card"
            onClick={() => onNavigate({ kind: 'endpoint', id: endpoint.id })}
          >
            <div className="api-endpoint-card__top">
              <MethodBadge method={endpoint.method} compact />
              <AuthBadge auth={endpoint.auth} />
            </div>
            <h3 className="api-endpoint-card__title">{endpoint.title}</h3>
            <code className="api-endpoint-card__path">{endpoint.path}</code>
            <p className="api-endpoint-card__desc">{endpoint.summary ?? endpoint.description}</p>
          </button>
        ))}
      </div>
    </div>
  );
}
