import { useEffect, useState, type ReactNode } from 'react';
import { ChevronRight, X } from 'lucide-react';
import type { AccountPickerSearchFn } from '@/api/accountPicker';
import type { AccountPickerRow } from '@/api/types';
import type { OperationPickerSearchFn } from '@/api/operationPicker';
import type { GatewayCredentialPickerRow, GatewayPrefix, OperationPickerRow } from '@/api/types';
import { searchCredentialsForPicker } from '@/api/gateways';
import { ListPagination } from '@/components/data/list-pagination';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { cn } from '@/lib/utils';
import { shortId } from '@/utils/format';

const PAGE_SIZE = 8;

type EntityPickerLoadResult<T> =
  | { ok: true; total: number; items: T[] }
  | { ok: false; error: string };

type EntityPickerDialogProps<T> = {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  searchPlaceholder?: string;
  emptyLabel?: string;
  disabledIds?: Set<string>;
  disabledBadgeText?: string;
  header?: ReactNode;
  loadPage: (params: { limit: number; offset: number; keyword: string | null }) => Promise<EntityPickerLoadResult<T>>;
  getItemId: (item: T) => string;
  renderItem: (item: T) => { label: string; description?: string; badges?: ReactNode; disabled?: boolean };
  onSelected: (item: T) => void;
  reloadKey?: string | number;
};

function accountInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

export function EntityPickerDialog<T>({
  open,
  onClose,
  title,
  subtitle,
  searchPlaceholder = 'Buscar…',
  emptyLabel = 'Nenhum resultado.',
  disabledIds,
  disabledBadgeText = 'Indisponível',
  header,
  loadPage,
  getItemId,
  renderItem,
  onSelected,
  reloadKey,
}: EntityPickerDialogProps<T>) {
  const [keyword, setKeyword] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [items, setItems] = useState<T[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  useEffect(() => {
    if (!open) return;
    setCurrentPage(1);
    setKeyword('');
    setLoadError('');
    void load(1, '');
  }, [open, reloadKey]);

  async function load(page: number, term: string) {
    setLoading(true);
    setLoadError('');
    try {
      const result = await loadPage({
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        keyword: term.trim() || null,
      });
      if (!result.ok) {
        setItems([]);
        setTotalItems(0);
        setLoadError(result.error);
        return;
      }
      setTotalItems(result.total);
      setItems(result.items);
    } finally {
      setLoading(false);
    }
  }

  function isDisabled(id: string) {
    return disabledIds?.has(id) ?? false;
  }

  async function search() {
    setCurrentPage(1);
    await load(1, keyword);
  }

  async function prevPage() {
    if (currentPage <= 1) return;
    const next = currentPage - 1;
    setCurrentPage(next);
    await load(next, keyword);
  }

  async function nextPage() {
    if (currentPage >= totalPages) return;
    const next = currentPage + 1;
    setCurrentPage(next);
    await load(next, keyword);
  }

  function pick(item: T) {
    const id = getItemId(item);
    if (isDisabled(id)) return;
    onSelected(item);
    onClose();
  }

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
      <DialogContent className="flex max-h-[85vh] flex-col gap-4 sm:max-w-lg" showCloseButton={false}>
        <DialogHeader>
          <div className="flex items-start justify-between gap-3">
            <div className="space-y-1">
              <DialogTitle>{title}</DialogTitle>
              {subtitle ? <DialogDescription>{subtitle}</DialogDescription> : null}
            </div>
            <Button type="button" variant="ghost" size="icon-sm" aria-label="Fechar" onClick={onClose}>
              <X className="size-4" />
            </Button>
          </div>
        </DialogHeader>

        {header}

        <Command shouldFilter={false} className="rounded-lg border">
          <CommandInput
            placeholder={searchPlaceholder}
            value={keyword}
            onValueChange={setKeyword}
            onKeyDown={(event) => {
              if (event.key === 'Enter') void search();
            }}
          />
          <CommandList className="max-h-[40vh]">
            {loading ? (
              <div className="flex flex-col items-center gap-2 py-10 text-center">
                <div className="size-5 animate-spin rounded-full border-2 border-muted-foreground/30 border-t-primary" aria-hidden="true" />
                <p className="text-sm text-muted-foreground">Carregando…</p>
              </div>
            ) : loadError ? (
              <div className="py-10 text-center">
                <p className="text-sm text-destructive">{loadError}</p>
              </div>
            ) : items.length === 0 ? (
              <CommandEmpty>{emptyLabel}</CommandEmpty>
            ) : (
              <CommandGroup>
                {items.map((item) => {
                  const id = getItemId(item);
                  const disabled = isDisabled(id) || renderItem(item).disabled;
                  const content = renderItem(item);
                  return (
                    <CommandItem
                      key={id}
                      value={id}
                      disabled={disabled}
                      onSelect={() => pick(item)}
                      className={cn('gap-3 py-2.5', disabled && 'opacity-60')}
                    >
                      <span className="min-w-0 flex-1">
                        <span className="flex flex-wrap items-center gap-1.5">
                          <span className="font-medium text-foreground">{content.label}</span>
                          {content.badges}
                        </span>
                        {content.description ? (
                          <span className="block truncate font-mono text-xs text-muted-foreground">{content.description}</span>
                        ) : null}
                      </span>
                      {disabled ? (
                        <Badge variant="outline">{disabledBadgeText}</Badge>
                      ) : (
                        <ChevronRight className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                      )}
                    </CommandItem>
                  );
                })}
              </CommandGroup>
            )}
          </CommandList>
        </Command>

        <DialogFooter className="items-center sm:justify-between">
          <span className="text-sm text-muted-foreground">
            {totalItems === 0 ? 'Sem resultados' : `${totalItems} registro${totalItems === 1 ? '' : 's'}`}
          </span>
          <ListPagination
            currentPage={currentPage}
            totalPages={totalPages}
            disabled={loading}
            onPrev={() => void prevPage()}
            onNext={() => void nextPage()}
          />
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

type AccountPickerDialogProps = {
  open: boolean;
  onClose: () => void;
  searchAccounts: AccountPickerSearchFn;
  title?: string;
  subtitle?: string;
  disabledAccountIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: AccountPickerRow) => void;
};

export function AccountPickerDialog({
  open,
  onClose,
  searchAccounts,
  title = 'Selecionar conta',
  subtitle,
  disabledAccountIds,
  disabledBadgeText = 'Já vinculado',
  onSelected,
}: AccountPickerDialogProps) {
  return (
    <EntityPickerDialog
      open={open}
      onClose={onClose}
      title={title}
      subtitle={subtitle}
      searchPlaceholder="Buscar por nome ou ID…"
      emptyLabel="Nenhuma conta encontrada."
      disabledIds={disabledAccountIds}
      disabledBadgeText={disabledBadgeText}
      loadPage={searchAccounts}
      getItemId={(row) => row.id}
      onSelected={onSelected}
      renderItem={(row) => ({
        label: row.username,
        description: shortId(row.id, 24),
        badges: row.roles?.map((role) => (
          <Badge key={role} variant="secondary">{role}</Badge>
        )),
      })}
    />
  );
}

type OperationPickerDialogProps = {
  open: boolean;
  onClose: () => void;
  searchOperations: OperationPickerSearchFn;
  title?: string;
  subtitle?: string;
  disabledOperationIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: OperationPickerRow) => void;
};

export function OperationPickerDialog({
  open,
  onClose,
  searchOperations,
  title = 'Selecionar operação',
  subtitle,
  disabledOperationIds,
  disabledBadgeText = 'Indisponível',
  onSelected,
}: OperationPickerDialogProps) {
  return (
    <EntityPickerDialog
      open={open}
      onClose={onClose}
      title={title}
      subtitle={subtitle}
      searchPlaceholder="Nome, ID ou descrição…"
      emptyLabel="Nenhuma operação encontrada."
      disabledIds={disabledOperationIds}
      disabledBadgeText={disabledBadgeText}
      loadPage={searchOperations}
      getItemId={(row) => row.id}
      onSelected={onSelected}
      renderItem={(row) => ({
        label: row.name,
        description: row.id,
      })}
    />
  );
}

const GATEWAY_LABELS: Record<GatewayPrefix, string> = {
  frendz: 'Frendz',
  sigilopay: 'SigiloPay',
  wintech: 'Wintech',
};

type GatewayCredentialPickerDialogProps = {
  open: boolean;
  onClose: () => void;
  title?: string;
  subtitle?: string;
  disabledCredentialIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: GatewayCredentialPickerRow) => void;
};

export function GatewayCredentialPickerDialog({
  open,
  onClose,
  title = 'Selecionar credencial',
  subtitle,
  disabledCredentialIds,
  disabledBadgeText = 'Já na lista',
  onSelected,
}: GatewayCredentialPickerDialogProps) {
  const [gateway, setGateway] = useState<GatewayPrefix>('frendz');

  useEffect(() => {
    if (open) setGateway('frendz');
  }, [open]);

  async function loadPage(params: { limit: number; offset: number; keyword: string | null }) {
    const result = await searchCredentialsForPicker(gateway, params);
    if (!result.ok) {
      return { ok: false as const, error: result.error };
    }
    const label = GATEWAY_LABELS[gateway];
    return {
      ok: true as const,
      total: result.data?.total ?? 0,
      items: (result.data?.items ?? []).map((item) => ({
        id: item.id,
        name: item.name?.trim() ? item.name : item.id,
        gatewayLabel: label,
      })),
    };
  }

  return (
    <EntityPickerDialog
      open={open}
      onClose={onClose}
      title={title}
      subtitle={subtitle}
      searchPlaceholder="Nome ou ID da credencial…"
      emptyLabel="Nenhuma credencial encontrada."
      disabledIds={disabledCredentialIds}
      disabledBadgeText={disabledBadgeText}
      loadPage={loadPage}
      reloadKey={gateway}
      getItemId={(row) => row.id}
      onSelected={onSelected}
      header={(
        <div className="flex flex-wrap gap-1">
          {(Object.keys(GATEWAY_LABELS) as GatewayPrefix[]).map((gw) => (
            <Button
              key={gw}
              type="button"
              size="sm"
              variant={gateway === gw ? 'default' : 'outline'}
              onClick={() => setGateway(gw)}
            >
              {GATEWAY_LABELS[gw]}
            </Button>
          ))}
        </div>
      )}
      renderItem={(row) => ({
        label: row.name,
        description: row.id,
        badges: <Badge variant="secondary">{row.gatewayLabel}</Badge>,
      })}
    />
  );
}

export function AccountPickerItemAvatar({ username }: { username: string }) {
  return (
    <Avatar size="sm">
      <AvatarFallback>{accountInitial(username)}</AvatarFallback>
    </Avatar>
  );
}
