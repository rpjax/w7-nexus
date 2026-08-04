import { Outlet } from 'react-router-dom';
import { LogOut, User } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { NavMenu } from './NavMenu';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Sidebar,
  SidebarFooter,
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from '@/components/ui/sidebar';
import { useAuth } from '@/auth/AuthContext';
import { cn } from '@/lib/utils';

function userInitial(username: string | undefined): string {
  const letter = username?.trim().charAt(0);
  return letter ? letter.toUpperCase() : '?';
}

export function DashboardLayout() {
  const navigate = useNavigate();
  const { user, signOut } = useAuth();
  const username = user?.username ?? 'Conta';

  return (
    <SidebarProvider defaultOpen>
      <Sidebar collapsible="offcanvas">
        <NavMenu />
        <SidebarFooter className="border-t border-sidebar-border p-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-auto w-full justify-start gap-2 px-2 py-2">
                <Avatar className="size-8">
                  <AvatarFallback>{userInitial(user?.username)}</AvatarFallback>
                </Avatar>
                <div className="min-w-0 text-left">
                  <p className="truncate text-sm font-medium">{username}</p>
                  <p className="truncate text-xs text-muted-foreground">
                    {user?.roles.join(', ') || 'Sessão ativa'}
                  </p>
                </div>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-56">
              <DropdownMenuLabel>Minha conta</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem disabled>
                <User className="size-4" />
                {username}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => {
                  signOut();
                  navigate('/auth', { replace: true });
                }}
              >
                <LogOut className="size-4" />
                Sair
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </SidebarFooter>
      </Sidebar>

      <SidebarInset className="min-h-dvh">
        <header className="sticky top-0 z-40 flex h-14 items-center gap-2 border-b bg-background/80 px-4 backdrop-blur-md">
          <SidebarTrigger />
          <div className="text-sm font-medium text-muted-foreground">Websete Nexus</div>
        </header>
        <main id="main-content" className={cn('flex-1 overflow-auto p-4 md:p-6')}>
          <Outlet />
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}
