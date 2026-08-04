import { useMemo, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from '@/components/ui/command';
import { ScrollArea } from '@/components/ui/scroll-area';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { cn } from '@/lib/utils';
import { API_FLOWS, API_GROUPS, API_ENDPOINTS } from '../../features/api-docs/catalog';
import type { ApiDocsView } from '../../features/api-docs/types';
import { MethodBadge } from './MethodBadge';

type ApiDocsSidebarProps = {
  activeView: ApiDocsView;
  onNavigate: (view: ApiDocsView) => void;
  mobileOpen?: boolean;
  onMobileClose?: () => void;
};

const accentDot: Record<string, string> = {
  blue: 'bg-primary',
  green: 'bg-success',
  amber: 'bg-warning',
  violet: 'bg-purple-400',
  rose: 'bg-rose-400',
};

function matchesQuery(text: string, query: string): boolean {
  return text.toLowerCase().includes(query.toLowerCase());
}

function SidebarNav({
  activeView,
  query,
  setQuery,
  onNavigate,
}: {
  activeView: ApiDocsView;
  query: string;
  setQuery: (value: string) => void;
  onNavigate: (view: ApiDocsView) => void;
}) {
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

  return (
    <>
      <Command shouldFilter={false} className="shrink-0 rounded-lg border border-border bg-background/60">
        <CommandInput
          placeholder="Buscar..."
          value={query}
          onValueChange={setQuery}
          aria-label="Buscar na documentação"
        />
        <CommandList className="max-h-0 overflow-hidden">
          <CommandEmpty />
        </CommandList>
      </Command>

      <ScrollArea className="min-h-0 flex-1">
        <Command shouldFilter={false} className="bg-transparent p-0">
          <CommandList className="max-h-none overflow-visible">
            {filteredEndpoints.length > 0 ? (
              <CommandGroup heading="Resultados">
                {filteredEndpoints.map((endpoint) => {
                  const view: ApiDocsView = { kind: 'endpoint', id: endpoint.id };
                  return (
                    <CommandItem
                      key={endpoint.id}
                      value={endpoint.id}
                      onSelect={() => onNavigate(view)}
                      className="p-0"
                    >
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className={cn(
                          'h-auto w-full justify-start gap-1.5 px-2 py-1.5 text-[0.82rem] font-normal',
                          isActive(view) && 'bg-primary/15 text-foreground',
                        )}
                        onClick={() => onNavigate(view)}
                      >
                        <MethodBadge method={endpoint.method} compact />
                        <span className="min-w-0 truncate">{endpoint.title}</span>
                      </Button>
                    </CommandItem>
                  );
                })}
              </CommandGroup>
            ) : null}

            {filteredEndpoints.length > 0 ? <CommandSeparator /> : null}

            <CommandGroup heading="Início">
              <CommandItem value="overview" onSelect={() => onNavigate({ kind: 'overview' })} className="p-0">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className={cn(
                    'h-auto w-full justify-start px-2 py-1.5 text-[0.82rem] font-normal',
                    isActive({ kind: 'overview' }) && 'bg-primary/15 text-foreground',
                  )}
                  onClick={() => onNavigate({ kind: 'overview' })}
                >
                  Visão geral
                </Button>
              </CommandItem>
            </CommandGroup>

            <CommandGroup heading="Fluxos guiados">
              {filteredFlows.map((flow) => {
                const view: ApiDocsView = { kind: 'flow', id: flow.id };
                return (
                  <CommandItem key={flow.id} value={flow.id} onSelect={() => onNavigate(view)} className="p-0">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className={cn(
                        'h-auto w-full justify-start gap-1.5 px-2 py-1.5 text-[0.82rem] font-normal',
                        isActive(view) && 'bg-primary/15 text-foreground',
                      )}
                      onClick={() => onNavigate(view)}
                    >
                      <span
                        className={cn('size-2 shrink-0 rounded-full', accentDot[flow.accent] ?? accentDot.blue)}
                        aria-hidden="true"
                      />
                      <span className="flex min-w-0 flex-col items-start gap-0.5">
                        <span>{flow.title}</span>
                        <span className="text-[0.68rem] text-muted-foreground">
                          {flow.steps.length} passos · ~{flow.estimatedMinutes} min
                        </span>
                      </span>
                    </Button>
                  </CommandItem>
                );
              })}
            </CommandGroup>

            <CommandGroup heading="Referência">
              {filteredGroups.map((group) => {
                const view: ApiDocsView = { kind: 'group', id: group.id };
                const count = API_ENDPOINTS.filter((e) => e.groupId === group.id).length;
                return (
                  <CommandItem key={group.id} value={group.id} onSelect={() => onNavigate(view)} className="p-0">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className={cn(
                        'h-auto w-full justify-start gap-1.5 px-2 py-1.5 text-[0.82rem] font-normal',
                        isActive(view) && 'bg-primary/15 text-foreground',
                      )}
                      onClick={() => onNavigate(view)}
                    >
                      <span className="min-w-0 flex-1 truncate text-left">{group.title}</span>
                      <Badge variant="secondary" className="ml-auto shrink-0 text-[0.7rem]">
                        {count}
                      </Badge>
                    </Button>
                  </CommandItem>
                );
              })}
            </CommandGroup>
          </CommandList>
        </Command>
      </ScrollArea>

      <div className="shrink-0 border-t border-border pt-2 text-xs text-muted-foreground">
        <p>OpenAPI (dev):</p>
        <a
          href="/openapi/v1.json"
          target="_blank"
          rel="noreferrer"
          className="text-[0.8rem] text-primary no-underline hover:underline"
        >
          /openapi/v1.json
        </a>
      </div>
    </>
  );
}

export function ApiDocsSidebar({
  activeView,
  onNavigate,
  mobileOpen = false,
  onMobileClose,
}: ApiDocsSidebarProps) {
  const [query, setQuery] = useState('');

  const navigate = (view: ApiDocsView) => {
    onNavigate(view);
    onMobileClose?.();
  };

  const sidebarBody = (
    <SidebarNav
      activeView={activeView}
      query={query}
      setQuery={setQuery}
      onNavigate={navigate}
    />
  );

  return (
    <>
      <aside className="hidden w-[280px] shrink-0 flex-col gap-2.5 rounded-xl border border-border bg-card/85 p-3.5 backdrop-blur-md lg:flex lg:max-h-full lg:min-h-0">
        {sidebarBody}
      </aside>

      <Sheet open={mobileOpen} onOpenChange={(open) => { if (!open) onMobileClose?.(); }}>
        <SheetContent side="left" className="flex w-[min(88vw,300px)] flex-col gap-3 p-4 sm:max-w-[300px]">
          <SheetHeader className="p-0">
            <SheetTitle>Explorar</SheetTitle>
          </SheetHeader>
          {sidebarBody}
        </SheetContent>
      </Sheet>
    </>
  );
}
