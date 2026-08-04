import type { ColumnDef } from '@tanstack/react-table';
import type { AccountRow } from '@/api/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { roleLabel, roleTone, type AccessTone } from '@/utils/accountAccess';
import { formatDateTime } from '@/utils/format';
import { Copy, MoreHorizontal, Settings2 } from 'lucide-react';

const toneVariant: Record<AccessTone, 'warning' | 'info' | 'secondary' | 'success' | 'outline'> = {
  admin: 'warning',
  operator: 'info',
  straw: 'secondary',
  olx: 'success',
  permission: 'outline',
};

export function createAccountColumns(options: {
  onManage: (account: AccountRow) => void;
}): ColumnDef<AccountRow>[] {
  return [
    {
      accessorKey: 'username',
      header: 'Conta',
      cell: ({ row }) => (
        <span className="font-medium">@{row.original.username}</span>
      ),
    },
    {
      id: 'roles',
      header: 'Funções',
      cell: ({ row }) => {
        const roles = row.original.roles ?? [];
        if (roles.length === 0) {
          return <Badge variant="outline">Sem funções</Badge>;
        }
        return (
          <div className="flex flex-wrap gap-1">
            {roles.map((role) => (
              <Badge key={role} variant={toneVariant[roleTone(role)]}>
                {roleLabel(role)}
              </Badge>
            ))}
          </div>
        );
      },
    },
    {
      id: 'permissions',
      header: 'Permissões',
      cell: ({ row }) => {
        const count = row.original.permissions?.length ?? 0;
        return count > 0 ? `${count} extra(s)` : 'Somente base';
      },
    },
    {
      accessorKey: 'lastUpdatedAt',
      header: 'Atualizada',
      cell: ({ row }) => (
        <time className="text-sm text-muted-foreground" dateTime={row.original.lastUpdatedAt}>
          {formatDateTime(row.original.lastUpdatedAt)}
        </time>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <AccountRowActions account={row.original} onManage={options.onManage} />
      ),
    },
  ];
}

function AccountRowActions({
  account,
  onManage,
}: {
  account: AccountRow;
  onManage: (account: AccountRow) => void;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label="Ações" onClick={(e) => e.stopPropagation()}>
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => onManage(account)}>
          <Settings2 className="size-4" />
          Gerenciar acesso
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => void navigator.clipboard.writeText(account.id)}>
          <Copy className="size-4" />
          Copiar ID
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
