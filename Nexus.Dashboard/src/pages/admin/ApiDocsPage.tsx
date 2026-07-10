import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ApiDocsSidebar } from '../../components/api-docs/ApiDocsSidebar';
import { ApiDocsOverview } from '../../components/api-docs/ApiDocsOverview';
import { ApiDocsFlowView } from '../../components/api-docs/ApiDocsFlowView';
import { ApiDocsGroupView } from '../../components/api-docs/ApiDocsGroupView';
import { ApiDocsEndpointView } from '../../components/api-docs/ApiDocsEndpointDetail';
import { buildApiDocsUrl, parseApiDocsView } from '../../features/api-docs/utils';
import { flowById } from '../../features/api-docs/catalog';
import { API_GROUPS } from '../../features/api-docs/catalog';
import type { ApiDocsView } from '../../features/api-docs/types';

function viewTitle(view: ApiDocsView): string {
  if (view.kind === 'overview') return 'Visão geral';
  if (view.kind === 'flow') return flowById.get(view.id)?.title ?? 'Fluxo';
  if (view.kind === 'group') return API_GROUPS.find((g) => g.id === view.id)?.title ?? 'Referência';
  return 'Endpoint';
}

export function ApiDocsPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const activeView = parseApiDocsView(searchParams.toString());
  const [navOpen, setNavOpen] = useState(false);

  const handleNavigate = useCallback((view: ApiDocsView) => {
    navigate(buildApiDocsUrl(view));
    setNavOpen(false);
  }, [navigate]);

  useEffect(() => {
    const main = document.querySelector('.api-docs-content');
    main?.scrollTo({ top: 0, behavior: 'smooth' });
  }, [searchParams]);

  useEffect(() => {
    if (!navOpen) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setNavOpen(false);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [navOpen]);

  return (
    <div className="api-docs-page">
      <header className="api-docs-toolbar">
        <button
          type="button"
          className="api-docs-toolbar__menu btn btn-ghost"
          onClick={() => setNavOpen(true)}
          aria-label="Abrir navegação"
        >
          <svg viewBox="0 0 20 20" width="18" height="18" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.6">
            <path d="M3 5h14M3 10h14M3 15h14" strokeLinecap="round" />
          </svg>
          Explorar
        </button>
        <div className="api-docs-toolbar__context">
          <span className="api-docs-toolbar__kicker">API Nexus</span>
          <span className="api-docs-toolbar__title">{viewTitle(activeView)}</span>
        </div>
      </header>

      <div className="api-docs-layout">
        <ApiDocsSidebar
          activeView={activeView}
          onNavigate={handleNavigate}
          mobileOpen={navOpen}
          onMobileClose={() => setNavOpen(false)}
        />

        <div className="api-docs-content admin-surface">
          {activeView.kind === 'overview' ? (
            <ApiDocsOverview onNavigate={handleNavigate} />
          ) : null}
          {activeView.kind === 'flow' ? (
            <ApiDocsFlowView flowId={activeView.id} onNavigate={handleNavigate} />
          ) : null}
          {activeView.kind === 'group' ? (
            <ApiDocsGroupView groupId={activeView.id} onNavigate={handleNavigate} />
          ) : null}
          {activeView.kind === 'endpoint' ? (
            <ApiDocsEndpointView endpointId={activeView.id} onNavigate={handleNavigate} />
          ) : null}
        </div>
      </div>
    </div>
  );
}
