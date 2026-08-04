import { NavLink, useLocation } from 'react-router-dom';
import { useEffect, useState, type ReactNode } from 'react';
import {
  BookOpen,
  ChevronDown,
  CreditCard,
  Home,
  Layers,
  Megaphone,
  Settings2,
  Shield,
  Users,
  Wallet,
} from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import {
  canUseOperatorPanel,
  canUseOlxPanel,
  canUseStrawManPanel,
  isAdministrator,
  isOlxOperator,
} from '../auth/roles';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import {
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarSeparator,
} from '@/components/ui/sidebar';

type NavSectionId = 'operations' | 'accounts' | 'payments' | 'strawMen' | 'olx' | 'scripts' | 'gateways' | 'dev';

function sectionForPath(pathname: string): NavSectionId | null {
  const path = pathname.replace(/\/$/, '').toLowerCase();
  if (path.startsWith('/dashboard/gateways')) return 'gateways';
  if (path.startsWith('/dashboard/admin/api-docs')) return 'dev';
  if (path.startsWith('/dashboard/admin/scripts')) return 'scripts';
  if (path.startsWith('/dashboard/olx')) return 'olx';
  if (path.startsWith('/dashboard/straw-man') || path.startsWith('/dashboard/admin/straw-men')) return 'strawMen';
  if (path.startsWith('/dashboard/payments') || path.startsWith('/dashboard/admin/payments')) return 'payments';
  if (path.startsWith('/dashboard/accounts')) return 'accounts';
  if (
    path.startsWith('/dashboard/operations')
    || path.startsWith('/dashboard/admin/operations')
    || path.startsWith('/dashboard/operation-admin')
    || path.startsWith('/dashboard/team-leader')
  ) {
    return 'operations';
  }
  return null;
}

function allClosedSections(): Record<NavSectionId, boolean> {
  return {
    operations: false,
    accounts: false,
    payments: false,
    strawMen: false,
    olx: false,
    scripts: false,
    gateways: false,
    dev: false,
  };
}

function defaultOpenSections(pathname: string): Record<NavSectionId, boolean> {
  const active = sectionForPath(pathname);
  return {
    operations: active === 'operations',
    accounts: active === 'accounts',
    payments: active === 'payments',
    strawMen: active === 'strawMen',
    olx: active === 'olx',
    scripts: active === 'scripts',
    gateways: active === 'gateways',
    dev: active === 'dev',
  };
}

function NavSection({
  id,
  label,
  icon: Icon,
  open,
  active,
  onToggle,
  variant,
  children,
}: {
  id: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  open: boolean;
  active?: boolean;
  onToggle: () => void;
  variant?: 'straw-men' | 'olx';
  children: ReactNode;
}) {
  return (
    <Collapsible open={open} onOpenChange={onToggle} className="group/collapsible">
      <SidebarGroup className="p-0">
        <SidebarGroupLabel asChild>
          <CollapsibleTrigger
            className={cn(
              'flex w-full items-center justify-between rounded-md px-2 py-1.5 text-sm font-medium transition-colors hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
              active && 'text-sidebar-primary',
              variant === 'olx' && 'text-warning',
              variant === 'straw-men' && 'text-success',
            )}
          >
            <span className="flex items-center gap-2">
              <Icon className="size-4 shrink-0 opacity-70" />
              {label}
            </span>
            <ChevronDown className={cn('size-4 transition-transform', open && 'rotate-180')} />
          </CollapsibleTrigger>
        </SidebarGroupLabel>
        <CollapsibleContent>
          <SidebarGroupContent>
            <SidebarMenuSub id={`${id}-submenu`}>{children}</SidebarMenuSub>
          </SidebarGroupContent>
        </CollapsibleContent>
      </SidebarGroup>
    </Collapsible>
  );
}

function NavSublink({
  to,
  admin,
  children,
  onNavigate,
}: {
  to: string;
  admin?: boolean;
  children: ReactNode;
  onNavigate?: () => void;
}) {
  return (
    <SidebarMenuSubItem>
      <SidebarMenuSubButton asChild>
        <NavLink
          to={to}
          onClick={onNavigate}
          className={({ isActive }) => cn(isActive && 'bg-sidebar-accent font-medium text-sidebar-accent-foreground')}
        >
          <span className="flex flex-1 items-center justify-between gap-2">
            <span>{children}</span>
            {admin ? (
              <Badge variant="warning" className="h-4 px-1.5 text-[10px]">
                Admin
              </Badge>
            ) : null}
          </span>
        </NavLink>
      </SidebarMenuSubButton>
    </SidebarMenuSubItem>
  );
}

export function NavMenu() {
  const { user } = useAuth();
  const location = useLocation();
  const showOperatorPanel = canUseOperatorPanel(user);
  const showGlobalAdminItems = isAdministrator(user);
  const showStrawManPanel = canUseStrawManPanel(user);
  const showOlxPanel = canUseOlxPanel(user);

  const [openSections, setOpenSections] = useState(() => defaultOpenSections(location.pathname));
  const activeSection = sectionForPath(location.pathname);

  useEffect(() => {
    setOpenSections(defaultOpenSections(location.pathname));
  }, [location.pathname]);

  const toggle = (id: NavSectionId) => {
    setOpenSections((prev) => {
      if (prev[id]) return { ...prev, [id]: false };
      return { ...allClosedSections(), [id]: true };
    });
  };
  const keepOpen = (id: NavSectionId) => () => setOpenSections((prev) => ({ ...prev, [id]: true }));

  const brandSubtitle = showGlobalAdminItems && showOperatorPanel
    ? 'Operações, pagamentos e administração'
    : showGlobalAdminItems
      ? 'Administração do sistema'
      : showOperatorPanel
        ? 'Operações e pagamentos'
        : showOlxPanel
          ? 'Painel OLX'
          : showStrawManPanel
            ? 'Painel do laranja'
            : 'Dashboard';

  return (
    <>
      <SidebarHeader className="border-b border-sidebar-border p-4">
        <div className="flex items-center gap-3">
          <div
            className="size-9 shrink-0 rounded-lg bg-gradient-to-br from-primary to-brand-violet shadow-lg shadow-primary/25"
            aria-hidden="true"
          />
          <div className="min-w-0">
            <p className="truncate text-sm font-bold tracking-tight">Websete Nexus</p>
            <p className="truncate text-xs text-muted-foreground">{brandSubtitle}</p>
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent className="gap-0">
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton asChild>
                  <NavLink
                    to="/dashboard"
                    end
                    className={({ isActive }) => cn(isActive && 'bg-sidebar-accent font-medium')}
                  >
                    <Home className="size-4" />
                    <span>Visão geral</span>
                  </NavLink>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        <div className="space-y-1 px-2 py-2">
          {showOperatorPanel || showGlobalAdminItems ? (
            <NavSection
              id="operations"
              label="Operações"
              icon={Layers}
              open={openSections.operations}
              active={activeSection === 'operations'}
              onToggle={() => toggle('operations')}
            >
              {showOperatorPanel ? (
                <NavSublink to="/dashboard/operations" onNavigate={keepOpen('operations')}>
                  Minhas operações
                </NavSublink>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/admin/operations" admin onNavigate={keepOpen('operations')}>
                  Todas as operações
                </NavSublink>
              ) : null}
              {showOperatorPanel ? (
                <>
                  <NavSublink to="/dashboard/operation-admin/operations" onNavigate={keepOpen('operations')}>
                    Administração de operações
                  </NavSublink>
                  <NavSublink to="/dashboard/team-leader/operations" onNavigate={keepOpen('operations')}>
                    Liderança de equipes
                  </NavSublink>
                </>
              ) : null}
            </NavSection>
          ) : null}

          {showGlobalAdminItems ? (
            <NavSection
              id="accounts"
              label="Contas"
              icon={Users}
              open={openSections.accounts}
              active={activeSection === 'accounts'}
              onToggle={() => toggle('accounts')}
            >
              <NavSublink to="/dashboard/accounts" admin onNavigate={keepOpen('accounts')}>
                Gerenciar contas
              </NavSublink>
            </NavSection>
          ) : null}

          {showOperatorPanel || showGlobalAdminItems ? (
            <NavSection
              id="payments"
              label="Pagamentos"
              icon={Wallet}
              open={openSections.payments}
              active={activeSection === 'payments'}
              onToggle={() => toggle('payments')}
            >
              {showOperatorPanel ? (
                <NavSublink to="/dashboard/payments" onNavigate={keepOpen('payments')}>
                  Meus pagamentos
                </NavSublink>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/admin/payments" admin onNavigate={keepOpen('payments')}>
                  Todos os pagamentos
                </NavSublink>
              ) : null}
              {showOperatorPanel ? (
                <NavSublink to="/dashboard/payments/pix" onNavigate={keepOpen('payments')}>
                  Gerar PIX
                </NavSublink>
              ) : null}
            </NavSection>
          ) : null}

          {showStrawManPanel || showGlobalAdminItems ? (
            <NavSection
              id="straw-men"
              label="Laranjas"
              icon={Shield}
              variant="straw-men"
              open={openSections.strawMen}
              active={activeSection === 'strawMen'}
              onToggle={() => toggle('strawMen')}
            >
              {showStrawManPanel ? (
                <>
                  <NavSublink to="/dashboard/straw-man/payments" onNavigate={keepOpen('strawMen')}>
                    Meus pagamentos
                  </NavSublink>
                  <NavSublink to="/dashboard/straw-man/settings" onNavigate={keepOpen('strawMen')}>
                    Minhas configurações
                  </NavSublink>
                </>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/admin/straw-men" admin onNavigate={keepOpen('strawMen')}>
                  Gestão de laranjas
                </NavSublink>
              ) : null}
            </NavSection>
          ) : null}

          {showOlxPanel ? (
            <NavSection
              id="olx"
              label="OLX"
              icon={Megaphone}
              variant="olx"
              open={openSections.olx}
              active={activeSection === 'olx'}
              onToggle={() => toggle('olx')}
            >
              {isOlxOperator(user) || showGlobalAdminItems ? (
                <NavSublink to="/dashboard/olx/ads" onNavigate={keepOpen('olx')}>
                  Meus anúncios
                </NavSublink>
              ) : null}
              {showGlobalAdminItems ? (
                <NavSublink to="/dashboard/olx/admin/ads" admin onNavigate={keepOpen('olx')}>
                  Gestão global
                </NavSublink>
              ) : null}
            </NavSection>
          ) : null}

          {showGlobalAdminItems ? (
            <NavSection
              id="scripts"
              label="Scripts"
              icon={Settings2}
              open={openSections.scripts}
              active={activeSection === 'scripts'}
              onToggle={() => toggle('scripts')}
            >
              <NavSublink to="/dashboard/admin/scripts" admin onNavigate={keepOpen('scripts')}>
                Inventário
              </NavSublink>
            </NavSection>
          ) : null}

          {showGlobalAdminItems ? (
            <NavSection
              id="dev"
              label="Desenvolvimento"
              icon={BookOpen}
              open={openSections.dev}
              active={activeSection === 'dev'}
              onToggle={() => toggle('dev')}
            >
              <NavSublink to="/dashboard/admin/api-docs" admin onNavigate={keepOpen('dev')}>
                Documentação da API
              </NavSublink>
            </NavSection>
          ) : null}

          {showOperatorPanel ? (
            <NavSection
              id="gateways"
              label="Gateways"
              icon={CreditCard}
              open={openSections.gateways}
              active={activeSection === 'gateways'}
              onToggle={() => toggle('gateways')}
            >
              <NavSublink to="/dashboard/gateways" onNavigate={keepOpen('gateways')}>
                Visão geral
              </NavSublink>
              <NavSublink to="/dashboard/gateways/frendz" onNavigate={keepOpen('gateways')}>
                Frendz
              </NavSublink>
              <NavSublink to="/dashboard/gateways/sigilopay" onNavigate={keepOpen('gateways')}>
                SigiloPay
              </NavSublink>
              <NavSublink to="/dashboard/gateways/wintech" onNavigate={keepOpen('gateways')}>
                Wintech
              </NavSublink>
            </NavSection>
          ) : null}
        </div>
      </SidebarContent>
    </>
  );
}
