import { useMemo, useState } from 'react';
import { API_FLOWS, API_GROUPS, API_ENDPOINTS } from '../../features/api-docs/catalog';
import type { ApiDocsView } from '../../features/api-docs/types';
import { MethodBadge } from './MethodBadge';

type ApiDocsSidebarProps = {
  activeView: ApiDocsView;
  onNavigate: (view: ApiDocsView) => void;
  mobileOpen?: boolean;
  onMobileClose?: () => void;
};

function matchesQuery(text: string, query: string): boolean {
  return text.toLowerCase().includes(query.toLowerCase());
}

export function ApiDocsSidebar({
  activeView,
  onNavigate,
  mobileOpen = false,
  onMobileClose,
}: ApiDocsSidebarProps) {
  const [query, setQuery] = useState('');

  const filteredFlows = useMemo(() => {
    if (!query.trim()) return API_FLOWS;
    return API_FLOWS.filter(
      (f) => matchesQuery(f.title, query) || matchesQuery(f.description, query),
    );
  }, [query]);

  const filteredEndpoints = useMemo(() => {
    const trimmed = query.trim();
    if (!trimmed) return [];
    return API_ENDPOINTS.filter(
      (e) => matchesQuery(e.title, trimmed)
        || matchesQuery(e.path, trimmed)
        || matchesQuery(e.description, trimmed),
    ).slice(0, 10);
  }, [query]);

  const filteredGroups = useMemo(() => {
    if (!query.trim()) return API_GROUPS;
    return API_GROUPS.filter((g) => {
      const endpoints = API_ENDPOINTS.filter((e) => e.groupId === g.id);
      return matchesQuery(g.title, query)
        || matchesQuery(g.description, query)
        || endpoints.some((e) => matchesQuery(e.title, query) || matchesQuery(e.path, query));
    });
  }, [query]);

  const isActive = (view: ApiDocsView) => {
    if (view.kind === 'overview') return activeView.kind === 'overview';
    if (activeView.kind === 'overview') return false;
    return activeView.kind === view.kind && activeView.id === view.id;
  };

  const navigate = (view: ApiDocsView) => {
    onNavigate(view);
    onMobileClose?.();
  };

  return (
    <>
      <div
        className={`api-docs-drawer-backdrop${mobileOpen ? ' is-open' : ''}`}
        onClick={onMobileClose}
        aria-hidden={!mobileOpen}
      />

      <aside className={`api-docs-sidebar${mobileOpen ? ' is-mobile-open' : ''}`}>
        <div className="api-docs-sidebar__head">
          <p className="api-docs-sidebar__title">Explorar</p>
          {onMobileClose ? (
            <button type="button" className="api-docs-sidebar__close" onClick={onMobileClose} aria-label="Fechar menu">
              ✕
            </button>
          ) : null}
        </div>

        <div className="api-docs-sidebar__search">
          <svg viewBox="0 0 20 20" width="16" height="16" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.6">
            <circle cx="9" cy="9" r="5.5" />
            <path d="M13.5 13.5L17 17" strokeLinecap="round" />
          </svg>
          <input
            type="search"
            className="api-docs-sidebar__input"
            placeholder="Buscar..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            aria-label="Buscar na documentação"
          />
        </div>

        <nav className="api-docs-sidebar__nav">
          {filteredEndpoints.length > 0 ? (
            <div className="api-docs-sidebar__section">
              <p className="api-docs-sidebar__heading">Resultados</p>
              {filteredEndpoints.map((endpoint) => (
                <button
                  key={endpoint.id}
                  type="button"
                  className={`api-docs-sidebar__link api-docs-sidebar__link--endpoint${isActive({ kind: 'endpoint', id: endpoint.id }) ? ' is-active' : ''}`}
                  onClick={() => navigate({ kind: 'endpoint', id: endpoint.id })}
                >
                  <MethodBadge method={endpoint.method} compact />
                  <span className="api-docs-sidebar__endpoint-label">{endpoint.title}</span>
                </button>
              ))}
            </div>
          ) : null}

          <div className="api-docs-sidebar__section">
            <p className="api-docs-sidebar__heading">Início</p>
            <button
              type="button"
              className={`api-docs-sidebar__link${isActive({ kind: 'overview' }) ? ' is-active' : ''}`}
              onClick={() => navigate({ kind: 'overview' })}
            >
              Visão geral
            </button>
          </div>

          <div className="api-docs-sidebar__section">
            <p className="api-docs-sidebar__heading">Fluxos guiados</p>
            {filteredFlows.map((flow) => (
              <button
                key={flow.id}
                type="button"
                className={`api-docs-sidebar__link api-docs-sidebar__link--flow${isActive({ kind: 'flow', id: flow.id }) ? ' is-active' : ''}`}
                onClick={() => navigate({ kind: 'flow', id: flow.id })}
              >
                <span className={`api-flow-dot api-flow-dot--${flow.accent}`} aria-hidden="true" />
                <span className="api-docs-sidebar__flow-text">
                  <span>{flow.title}</span>
                  <span className="api-docs-sidebar__flow-meta">{flow.steps.length} passos · ~{flow.estimatedMinutes} min</span>
                </span>
              </button>
            ))}
          </div>

          <div className="api-docs-sidebar__section">
            <p className="api-docs-sidebar__heading">Referência</p>
            {filteredGroups.map((group) => {
              const count = API_ENDPOINTS.filter((e) => e.groupId === group.id).length;
              return (
                <button
                  key={group.id}
                  type="button"
                  className={`api-docs-sidebar__link${isActive({ kind: 'group', id: group.id }) ? ' is-active' : ''}`}
                  onClick={() => navigate({ kind: 'group', id: group.id })}
                >
                  {group.title}
                  <span className="api-docs-sidebar__count">{count}</span>
                </button>
              );
            })}
          </div>
        </nav>

        <div className="api-docs-sidebar__footer muted small">
          <p>OpenAPI (dev):</p>
          <a href="/openapi/v1.json" target="_blank" rel="noreferrer" className="api-docs-sidebar__spec-link">
            /openapi/v1.json
          </a>
        </div>
      </aside>
    </>
  );
}
