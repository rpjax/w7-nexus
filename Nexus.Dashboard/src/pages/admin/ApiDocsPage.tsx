import { useCallback, useEffect, useState } from 'react';
import { MenuIcon } from 'lucide-react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb';
import { ScrollArea } from '@/components/ui/scroll-area';
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
    if (!navOpen) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setNavOpen(false);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [navOpen]);

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-0">
      <header className="flex shrink-0 flex-col gap-2 pb-3">
        <div className="flex items-center gap-3">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="inline-flex shrink-0 gap-1.5 lg:hidden"
            onClick={() => setNavOpen(true)}
            aria-label="Abrir navegação"
          >
            <MenuIcon className="size-[18px]" aria-hidden="true" />
            Explorar
          </Button>
          <Breadcrumb className="min-w-0 flex-1">
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink
                  className="cursor-pointer"
                  onClick={() => handleNavigate({ kind: 'overview' })}
                >
                  API Nexus
                </BreadcrumbLink>
              </BreadcrumbItem>
              {activeView.kind !== 'overview' ? (
                <>
                  <BreadcrumbSeparator />
                  <BreadcrumbItem>
                    <BreadcrumbPage className="truncate">{viewTitle(activeView)}</BreadcrumbPage>
                  </BreadcrumbItem>
                </>
              ) : null}
            </BreadcrumbList>
          </Breadcrumb>
        </div>
      </header>

      <div className="relative flex min-h-0 flex-1 gap-4">
        <ApiDocsSidebar
          activeView={activeView}
          onNavigate={handleNavigate}
          mobileOpen={navOpen}
          onMobileClose={() => setNavOpen(false)}
        />

        <ScrollArea className="min-h-0 min-w-0 flex-1 rounded-xl border border-border bg-card/80">
          <div className="p-4 lg:p-5">
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
        </ScrollArea>
      </div>
    </div>
  );
}
