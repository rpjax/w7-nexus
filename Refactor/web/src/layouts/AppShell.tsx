import { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import {
  Home,
  LogOut,
  Menu,
  Shield,
  UserRound,
  X,
} from 'lucide-react';
import { useAuth } from '@/auth/AuthContext';
import { BrandGlyph } from '@/components/brand/BrandMark';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { isAdministrator, roleLabel } from '@/utils/accountAccess';
import { cn } from '@/lib/utils';

function userInitial(username: string | undefined): string {
  const letter = username?.trim().charAt(0);
  return letter ? letter.toUpperCase() : '?';
}

type NavItem = {
  to: string;
  label: string;
  icon: typeof Home;
  end?: boolean;
  adminOnly?: boolean;
};

const NAV_ITEMS: NavItem[] = [
  { to: '/dashboard', label: 'Início', icon: Home, end: true },
  { to: '/dashboard/profile', label: 'Perfil', icon: UserRound },
  { to: '/dashboard/accounts', label: 'Contas', icon: Shield, adminOnly: true },
];

function SideNavItems({
  admin,
  onNavigate,
}: {
  admin: boolean;
  onNavigate?: () => void;
}) {
  return (
    <nav className="flex flex-col gap-1">
      {NAV_ITEMS.filter((item) => !item.adminOnly || admin).map((item) => {
        const Icon = item.icon;
        return (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                'group flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary/15 text-foreground ring-1 ring-primary/25'
                  : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground',
              )
            }
          >
            <Icon className="size-4 shrink-0 opacity-80" />
            {item.label}
          </NavLink>
        );
      })}
    </nav>
  );
}

function SidebarBody({
  admin,
  username,
  roles,
  onNavigate,
  onSignOut,
}: {
  admin: boolean;
  username?: string;
  roles: string;
  onNavigate?: () => void;
  onSignOut: () => void;
}) {
  return (
    <>
      <div className="flex flex-1 flex-col gap-4 p-3">
        <div>
          <p className="mb-2 px-3 text-[0.65rem] font-semibold uppercase tracking-[0.18em] text-muted-foreground/80">
            Navegação
          </p>
          <SideNavItems admin={admin} onNavigate={onNavigate} />
        </div>
      </div>

      <div className="border-t border-border/60 p-3">
        <div className="mb-2 flex items-center gap-2.5 rounded-lg px-2 py-2">
          <Avatar className="size-8">
            <AvatarFallback>{userInitial(username)}</AvatarFallback>
          </Avatar>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium">{username ?? 'Conta'}</p>
            <p className="truncate text-xs text-muted-foreground">{roles}</p>
          </div>
        </div>
        <Button variant="outline" className="w-full justify-start" onClick={onSignOut}>
          <LogOut data-icon="inline-start" />
          Sair
        </Button>
      </div>
    </>
  );
}

export function AppShell() {
  const { user, signOut } = useAuth();
  const location = useLocation();
  const admin = isAdministrator(user?.roles);
  const [mobileOpen, setMobileOpen] = useState(false);
  const roles = (user?.roles ?? []).map(roleLabel).join(' · ') || 'Identidade';

  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    if (!mobileOpen) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previous;
    };
  }, [mobileOpen]);

  return (
    <div className="relative flex min-h-dvh w-full min-w-0 overflow-x-hidden">
      <aside className="sticky top-0 z-30 hidden h-dvh w-64 shrink-0 flex-col border-r border-border/70 bg-[#080d1a]/95 backdrop-blur-xl lg:flex">
        <div className="flex h-14 items-center gap-2.5 px-4">
          <BrandGlyph className="size-7" />
          <div className="min-w-0">
            <p className="font-display text-[0.65rem] font-semibold uppercase tracking-[0.22em] text-primary">
              Websete
            </p>
            <p className="truncate text-sm font-medium">Nexus</p>
          </div>
        </div>
        <Separator />
        <SidebarBody
          admin={admin}
          username={user?.username}
          roles={roles}
          onSignOut={signOut}
        />
      </aside>

      {mobileOpen ? (
        <div className="fixed inset-0 z-50 lg:hidden">
          <button
            type="button"
            className="absolute inset-0 bg-background/70 backdrop-blur-sm"
            aria-label="Fechar menu"
            onClick={() => setMobileOpen(false)}
          />
          <aside
            className="absolute inset-y-0 left-0 flex w-[min(18.5rem,calc(100vw-3rem))] flex-col border-r border-border/70 bg-[#080d1a] shadow-2xl"
            role="dialog"
            aria-modal="true"
            aria-label="Menu de navegação"
          >
            <div className="flex h-14 items-center justify-between gap-2 px-4">
              <div className="flex min-w-0 items-center gap-2.5">
                <BrandGlyph className="size-7 shrink-0" />
                <div className="min-w-0">
                  <p className="font-display text-[0.65rem] font-semibold uppercase tracking-[0.22em] text-primary">
                    Websete
                  </p>
                  <p className="truncate text-sm font-medium">Nexus</p>
                </div>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Fechar menu"
                onClick={() => setMobileOpen(false)}
              >
                <X />
              </Button>
            </div>
            <Separator />
            <SidebarBody
              admin={admin}
              username={user?.username}
              roles={roles}
              onNavigate={() => setMobileOpen(false)}
              onSignOut={() => {
                setMobileOpen(false);
                signOut();
              }}
            />
          </aside>
        </div>
      ) : null}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 flex h-14 items-center gap-2 border-b border-border/70 bg-[#080d1a]/90 px-3 backdrop-blur-xl sm:gap-3 sm:px-4 lg:px-6">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="shrink-0 lg:hidden"
            aria-label="Abrir menu"
            aria-expanded={mobileOpen}
            onClick={() => setMobileOpen(true)}
          >
            <Menu />
          </Button>
          <div className="flex min-w-0 items-center gap-2 lg:hidden">
            <BrandGlyph className="size-6 shrink-0" />
            <span className="font-display truncate text-sm font-semibold">Nexus</span>
          </div>
          <div className="hidden min-w-0 lg:block">
            <p className="text-sm font-medium text-foreground">Websete Nexus</p>
            <p className="text-xs text-muted-foreground">Painel operacional</p>
          </div>
          <div className="ml-auto min-w-0 lg:hidden">
            <p className="truncate text-xs text-muted-foreground">{user?.username}</p>
          </div>
        </header>

        <main
          id="main-content"
          className="dashboard-stage min-w-0 flex-1 overflow-x-hidden overflow-y-auto bg-[#070b16]/55 px-3 py-4 sm:px-4 sm:py-5 lg:px-6 lg:py-7"
        >
          <div className="mx-auto w-full min-w-0 max-w-6xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
