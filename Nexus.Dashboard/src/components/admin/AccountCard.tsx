import { useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import type { AccountRow } from '../../api/types';
import { roleLabel, roleTone, summarizeRoles, type AccessTone } from '../../utils/accountAccess';
import { formatDateTime, shortId } from '../../utils/format';
import { Copy } from 'lucide-react';
import { AccountAccessEditor } from './AccountAccessEditor';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

type AccountCardProps = {
  account: AccountRow;
  onMutated: () => void;
  onError: (message: string) => void;
  defaultExpanded?: boolean;
};

function accountInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

const toneVariant: Record<AccessTone, 'warning' | 'info' | 'secondary' | 'success' | 'outline'> = {
  admin: 'warning',
  operator: 'info',
  straw: 'secondary',
  olx: 'success',
  permission: 'outline',
};

export function AccountCard({ account, onMutated, onError, defaultExpanded = false }: AccountCardProps) {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const [technicalOpen, setTechnicalOpen] = useState(false);

  async function copyId() {
    try {
      await navigator.clipboard.writeText(account.id);
    } catch {
      // ignore clipboard errors
    }
  }

  const roles = account.roles ?? [];
  const permissions = account.permissions ?? [];
  const roleSummary = summarizeRoles(roles);

  return (
    <Card className={cn('border-border/60 bg-card/80', expanded && 'ring-1 ring-primary/15')}>
      <CardHeader className="p-0">
        <button
          type="button"
          className="flex w-full items-center gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/30"
          aria-expanded={expanded}
          onClick={() => setExpanded((open) => !open)}
        >
          <Avatar>
            <AvatarFallback>{accountInitial(account.username)}</AvatarFallback>
          </Avatar>
          <span className="min-w-0 flex-1 space-y-1">
            <span className="flex flex-wrap items-center gap-2">
              <strong className="text-sm font-semibold text-foreground">@{account.username}</strong>
              {roles.length > 0 ? (
                <span className="flex flex-wrap gap-1" aria-label={roleSummary}>
                  {roles.map((role) => (
                    <Badge key={role} variant={toneVariant[roleTone(role)]}>
                      {roleLabel(role)}
                    </Badge>
                  ))}
                </span>
              ) : (
                <Badge variant="outline">Sem funções</Badge>
              )}
            </span>
            <span className="block text-sm text-muted-foreground">
              {permissions.length > 0
                ? `${permissions.length} permissão(ões) extra(s)`
                : 'Somente funções base'}
              <span className="mx-1.5" aria-hidden="true">·</span>
              Atualizada {formatDateTime(account.lastUpdatedAt)}
            </span>
          </span>
          {expanded
            ? <ChevronDown className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
            : <ChevronRight className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />}
        </button>
      </CardHeader>

      {expanded ? (
        <>
          <Separator />
          <CardContent className="space-y-4 pt-4">
            <AccountAccessEditor
              accountId={account.id}
              roles={roles}
              permissions={permissions}
              onMutated={onMutated}
              onError={onError}
            />

            <Collapsible open={technicalOpen} onOpenChange={setTechnicalOpen}>
              <CollapsibleTrigger asChild>
                <Button type="button" variant="ghost" size="sm" className="w-full justify-between px-0">
                  Detalhes técnicos
                  {technicalOpen
                    ? <ChevronDown className="size-4" aria-hidden="true" />
                    : <ChevronRight className="size-4" aria-hidden="true" />}
                </Button>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <div className="mt-2 grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">ID</span>
                    <p className="flex items-center gap-1 font-mono text-sm text-foreground">
                      <span title={account.id}>{shortId(account.id, 28)}</span>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        aria-label="Copiar ID da conta"
                        onClick={() => void copyId()}
                      >
                        <Copy className="size-4" />
                      </Button>
                    </p>
                  </div>
                  <div className="space-y-1">
                    <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Criada em</span>
                    <p className="text-sm text-foreground">{formatDateTime(account.createdAt)}</p>
                  </div>
                </div>
              </CollapsibleContent>
            </Collapsible>
          </CardContent>
        </>
      ) : null}
    </Card>
  );
}
