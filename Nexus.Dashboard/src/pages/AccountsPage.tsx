import { ChevronDown, ChevronRight } from 'lucide-react';
import { useMemo, useState } from 'react';
import { createAdministratorAccount, searchAdministratorAccounts } from '@/api/administrator/accounts';
import type { AccountRow } from '@/api/types';
import { AccountAccessEditor } from '@/components/admin/AccountAccessEditor';
import { DataTable } from '@/components/data/data-table';
import { ListPagination } from '@/components/data/list-pagination';
import { ListPageLayout } from '@/components/layout/list-page-layout';
import { createAccountColumns } from '@/features/accounts/account-columns';
import { usePaginatedQuery, adaptSearchResponse } from '@/hooks/use-paginated-query';
import { useNotifications } from '@/notifications/NotificationContext';
import { ACCOUNT_ROLE_CATALOG, type AccessTone } from '@/utils/accountAccess';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { cn } from '@/lib/utils';

const toneAccent: Record<AccessTone, string> = {
  admin: 'border-l-warning',
  operator: 'border-l-primary',
  straw: 'border-l-muted-foreground',
  olx: 'border-l-success',
  permission: 'border-l-border',
};

export function AccountsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [createBusy, setCreateBusy] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [manageAccount, setManageAccount] = useState<AccountRow | null>(null);

  const {
    search,
    setSearch,
    currentPage,
    totalItems,
    totalPages,
    items,
    isLoading,
    error,
    refetch,
    submitSearch,
    clearSearch,
    goPrev,
    goNext,
  } = usePaginatedQuery<AccountRow>({
    queryKey: ['admin-accounts'],
    fetchPage: async (params) => adaptSearchResponse(await searchAdministratorAccounts({
      limit: params.limit,
      offset: params.offset,
      keyword: params.keyword,
    })),
  });

  const columns = useMemo(
    () => createAccountColumns({ onManage: setManageAccount }),
    [],
  );

  async function handleCreate() {
    setCreateBusy(true);
    try {
      if (!username.trim() || !password.trim()) {
        notifyError('Usuário e senha são obrigatórios.');
        return;
      }
      const result = await createAdministratorAccount(username.trim(), password);
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Conta criada com sucesso.');
      setUsername('');
      setPassword('');
      setCreateOpen(false);
      clearSearch();
      await refetch();
    } finally {
      setCreateBusy(false);
    }
  }

  return (
    <>
      <ListPageLayout
        kicker="Administração"
        kickerVariant="admin"
        title="Contas"
        description="Gerencie usuários e ative funções com um clique. Cada conta pode acumular vários papéis."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Contas' },
        ]}
        searchId="accSearch"
        searchLabel="Buscar contas"
        searchPlaceholder="Buscar por @username…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={submitSearch}
        onRefresh={() => void refetch()}
        totalLabel={`${totalItems} registro(s)`}
        isLoading={isLoading}
        error={error}
        isEmpty={!isLoading && !error && items.length === 0}
        emptyTitle="Nenhuma conta encontrada"
        emptyMessage="Registre uma conta abaixo ou ajuste o filtro de busca."
        footer={totalItems > 0 ? (
          <ListPagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={goPrev}
            onNext={goNext}
          />
        ) : undefined}
      >
        <div className="mb-6 space-y-6">
          <Card className="border-border/60 bg-card/80">
            <CardHeader>
              <CardTitle>Funções disponíveis</CardTitle>
              <CardDescription>
                Referência rápida dos papéis que você pode atribuir a qualquer conta.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ul className="grid gap-2 sm:grid-cols-2">
                {ACCOUNT_ROLE_CATALOG.map((role) => (
                  <li
                    key={role.id}
                    className={cn(
                      'rounded-lg border border-border/60 border-l-4 bg-background/40 px-3 py-2',
                      toneAccent[role.tone],
                    )}
                  >
                    <strong className="block text-sm text-foreground">{role.label}</strong>
                    <span className="text-sm text-muted-foreground">{role.description}</span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>

          <Card className="border-border/60 bg-card/80">
            <Collapsible open={createOpen} onOpenChange={setCreateOpen}>
              <CollapsibleTrigger asChild>
                <button
                  type="button"
                  className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/30"
                >
                  <span className="space-y-0.5">
                    <strong className="block text-sm font-semibold text-foreground">Registrar nova conta</strong>
                    <span className="block text-sm text-muted-foreground">
                      Login e senha inicial — funções são atribuídas depois.
                    </span>
                  </span>
                  {createOpen
                    ? <ChevronDown className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                    : <ChevronRight className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />}
                </button>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <CardContent className="space-y-4 border-t border-border/60 pt-4">
                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="accUsername">Usuário</Label>
                      <Input
                        id="accUsername"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        autoComplete="off"
                        placeholder="nome.de.login"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="accPassword">Senha</Label>
                      <Input
                        id="accPassword"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        autoComplete="new-password"
                        placeholder="Senha inicial"
                      />
                    </div>
                  </div>
                  <Button type="button" onClick={() => void handleCreate()} disabled={createBusy}>
                    {createBusy ? 'Registrando…' : 'Registrar conta'}
                  </Button>
                </CardContent>
              </CollapsibleContent>
            </Collapsible>
          </Card>
        </div>

        <DataTable columns={columns} data={items} getRowId={(row) => row.id} />
      </ListPageLayout>

      <Sheet open={manageAccount !== null} onOpenChange={(open) => { if (!open) setManageAccount(null); }}>
        <SheetContent className="overflow-y-auto sm:max-w-lg">
          {manageAccount ? (
            <>
              <SheetHeader>
                <SheetTitle>@{manageAccount.username}</SheetTitle>
                <SheetDescription>Alterne funções e permissões desta conta.</SheetDescription>
              </SheetHeader>
              <div className="mt-6">
                <AccountAccessEditor
                  accountId={manageAccount.id}
                  roles={manageAccount.roles ?? []}
                  permissions={manageAccount.permissions ?? []}
                  onMutated={() => {
                    notifySuccess('Conta atualizada.');
                    void refetch();
                  }}
                  onError={notifyError}
                />
              </div>
            </>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
