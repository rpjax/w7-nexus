import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import {
  grantAdministratorAccountPermission,
  grantAdministratorAccountRole,
  revokeAdministratorAccountPermission,
  revokeAdministratorAccountRole,
} from '../../api/administrator/accounts';
import {
  ACCOUNT_PERMISSION_CATALOG,
  ACCOUNT_ROLE_CATALOG,
  hasPermissionIgnoreCase,
  hasRoleIgnoreCase,
  type AccessTone,
} from '../../utils/accountAccess';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
} from '@/components/ui/form';
import { Switch } from '@/components/ui/switch';
import { cn } from '@/lib/utils';

type AccountAccessEditorProps = {
  accountId: string;
  roles: string[];
  permissions: string[];
  onMutated: () => void;
  onError: (message: string) => void;
};

type AccessFormValues = {
  roles: Record<string, boolean>;
  permissions: Record<string, boolean>;
};

const toneAccent: Record<AccessTone, string> = {
  admin: 'border-warning/40 data-[active=true]:bg-warning/10',
  operator: 'border-primary/40 data-[active=true]:bg-primary/10',
  straw: 'border-border data-[active=true]:bg-muted/60',
  olx: 'border-success/40 data-[active=true]:bg-success/10',
  permission: 'border-border data-[active=true]:bg-muted/60',
};

function buildDefaultValues(roles: string[], permissions: string[]): AccessFormValues {
  return {
    roles: Object.fromEntries(
      ACCOUNT_ROLE_CATALOG.map((role) => [role.id, hasRoleIgnoreCase(roles, role.id)]),
    ),
    permissions: Object.fromEntries(
      ACCOUNT_PERMISSION_CATALOG.map((permission) => [permission.id, hasPermissionIgnoreCase(permissions, permission.id)]),
    ),
  };
}

export function AccountAccessEditor({
  accountId,
  roles,
  permissions,
  onMutated,
  onError,
}: AccountAccessEditorProps) {
  const [busyKey, setBusyKey] = useState<string | null>(null);

  const form = useForm<AccessFormValues>({
    defaultValues: buildDefaultValues(roles, permissions),
  });

  useEffect(() => {
    form.reset(buildDefaultValues(roles, permissions));
  }, [roles, permissions, form]);

  async function runAction(key: string, action: () => Promise<{ ok: boolean; error?: string }>) {
    setBusyKey(key);
    try {
      const result = await action();
      if (!result.ok) {
        onError(result.error ?? 'Não foi possível atualizar a conta.');
        return;
      }
      onMutated();
    } finally {
      setBusyKey(null);
    }
  }

  async function toggleRole(roleId: string, active: boolean) {
    const key = active ? `role-remove:${roleId}` : `role-add:${roleId}`;
    await runAction(
      key,
      () => (active
        ? revokeAdministratorAccountRole(accountId, roleId)
        : grantAdministratorAccountRole(accountId, roleId)),
    );
  }

  async function togglePermission(permissionId: string, active: boolean) {
    const key = active ? `perm-remove:${permissionId}` : `perm-add:${permissionId}`;
    await runAction(
      key,
      () => (active
        ? revokeAdministratorAccountPermission(accountId, permissionId)
        : grantAdministratorAccountPermission(accountId, permissionId)),
    );
  }

  const activeRoleCount = ACCOUNT_ROLE_CATALOG.filter((role) => hasRoleIgnoreCase(roles, role.id)).length;
  const activePermissionCount = ACCOUNT_PERMISSION_CATALOG.filter(
    (permission) => hasPermissionIgnoreCase(permissions, permission.id),
  ).length;

  return (
    <Form {...form}>
      <div className="space-y-6">
        <section className="space-y-3">
          <header className="flex items-start justify-between gap-3">
            <div className="space-y-1">
              <h4 className="text-sm font-semibold text-foreground">Funções</h4>
              <p className="text-sm text-muted-foreground">
                Ative os papéis que esta conta pode exercer na plataforma.
              </p>
            </div>
            <span className="shrink-0 text-sm text-muted-foreground">
              {activeRoleCount} / {ACCOUNT_ROLE_CATALOG.length}
            </span>
          </header>

          <div className="grid gap-2 sm:grid-cols-2">
            {ACCOUNT_ROLE_CATALOG.map((role) => (
              <FormField
                key={role.id}
                control={form.control}
                name={`roles.${role.id}`}
                render={({ field }) => (
                  <FormItem
                    data-active={field.value}
                    className={cn(
                      'rounded-xl border bg-card/40 p-3 transition-colors',
                      toneAccent[role.tone],
                      field.value && 'ring-1 ring-primary/20',
                    )}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 space-y-0.5">
                        <FormLabel className="block text-sm font-medium text-foreground">{role.label}</FormLabel>
                        <p className="text-xs text-muted-foreground">{role.description}</p>
                      </div>
                      <FormControl>
                        <Switch
                          checked={field.value}
                          disabled={busyKey !== null}
                          aria-label={role.label}
                          onCheckedChange={(checked) => {
                            field.onChange(checked);
                            void toggleRole(role.id, !checked);
                          }}
                        />
                      </FormControl>
                    </div>
                  </FormItem>
                )}
              />
            ))}
          </div>
        </section>

        <section className="space-y-3">
          <header className="flex items-start justify-between gap-3">
            <div className="space-y-1">
              <h4 className="text-sm font-semibold text-foreground">Permissões extras</h4>
              <p className="text-sm text-muted-foreground">
                Capacidades adicionais além das funções base.
              </p>
            </div>
            <span className="shrink-0 text-sm text-muted-foreground">
              {activePermissionCount} / {ACCOUNT_PERMISSION_CATALOG.length}
            </span>
          </header>

          <div className="grid gap-2">
            {ACCOUNT_PERMISSION_CATALOG.map((permission) => (
              <FormField
                key={permission.id}
                control={form.control}
                name={`permissions.${permission.id}`}
                render={({ field }) => (
                  <FormItem
                    data-active={field.value}
                    className={cn(
                      'rounded-xl border border-border bg-card/40 p-3 transition-colors',
                      field.value && 'bg-muted/60 ring-1 ring-primary/20',
                    )}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 space-y-0.5">
                        <FormLabel className="block text-sm font-medium text-foreground">{permission.label}</FormLabel>
                        <p className="text-xs text-muted-foreground">{permission.description}</p>
                      </div>
                      <FormControl>
                        <Switch
                          checked={field.value}
                          disabled={busyKey !== null}
                          aria-label={permission.label}
                          onCheckedChange={(checked) => {
                            field.onChange(checked);
                            void togglePermission(permission.id, !checked);
                          }}
                        />
                      </FormControl>
                    </div>
                  </FormItem>
                )}
              />
            ))}
          </div>
        </section>
      </div>
    </Form>
  );
}
