import { useState } from 'react';
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
} from '../../utils/accountAccess';

type AccountAccessEditorProps = {
  accountId: string;
  roles: string[];
  permissions: string[];
  onMutated: () => void;
  onError: (message: string) => void;
};

export function AccountAccessEditor({
  accountId,
  roles,
  permissions,
  onMutated,
  onError,
}: AccountAccessEditorProps) {
  const [busyKey, setBusyKey] = useState<string | null>(null);

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
    <div className="account-access-editor">
      <section className="account-access-block">
        <header className="account-access-block__head">
          <div>
            <h4 className="account-access-block__title">Funções</h4>
            <p className="account-access-block__hint muted small">
              Ative os papéis que esta conta pode exercer na plataforma.
            </p>
          </div>
          <span className="account-access-block__count muted small">
            {activeRoleCount} / {ACCOUNT_ROLE_CATALOG.length}
          </span>
        </header>

        <div className="access-toggle-grid" role="group" aria-label="Funções da conta">
          {ACCOUNT_ROLE_CATALOG.map((role) => {
            const active = hasRoleIgnoreCase(roles, role.id);

            return (
              <button
                key={role.id}
                type="button"
                className={`access-toggle-card access-toggle-card--${role.tone}${active ? ' is-active' : ''}`}
                disabled={busyKey !== null}
                aria-pressed={active}
                title={role.id}
                onClick={() => void toggleRole(role.id, active)}
              >
                <span className="access-toggle-card__row">
                  <span className="access-toggle-card__copy">
                    <strong className="access-toggle-card__label">{role.label}</strong>
                    <span className="access-toggle-card__desc">{role.description}</span>
                  </span>
                  <span className={`access-toggle-card__switch${active ? ' is-on' : ''}`} aria-hidden="true">
                    <span className="access-toggle-card__knob" />
                  </span>
                </span>
              </button>
            );
          })}
        </div>
      </section>

      <section className="account-access-block account-access-block--permissions">
        <header className="account-access-block__head">
          <div>
            <h4 className="account-access-block__title">Permissões extras</h4>
            <p className="account-access-block__hint muted small">
              Capacidades adicionais além das funções base.
            </p>
          </div>
          <span className="account-access-block__count muted small">
            {activePermissionCount} / {ACCOUNT_PERMISSION_CATALOG.length}
          </span>
        </header>

        <div className="access-toggle-grid access-toggle-grid--compact" role="group" aria-label="Permissões da conta">
          {ACCOUNT_PERMISSION_CATALOG.map((permission) => {
            const active = hasPermissionIgnoreCase(permissions, permission.id);

            return (
              <button
                key={permission.id}
                type="button"
                className={`access-toggle-card access-toggle-card--permission${active ? ' is-active' : ''}`}
                disabled={busyKey !== null}
                aria-pressed={active}
                title={permission.id}
                onClick={() => void togglePermission(permission.id, active)}
              >
                <span className="access-toggle-card__row">
                  <span className="access-toggle-card__copy">
                    <strong className="access-toggle-card__label">{permission.label}</strong>
                    <span className="access-toggle-card__desc">{permission.description}</span>
                  </span>
                  <span className={`access-toggle-card__switch${active ? ' is-on' : ''}`} aria-hidden="true">
                    <span className="access-toggle-card__knob" />
                  </span>
                </span>
              </button>
            );
          })}
        </div>
      </section>
    </div>
  );
}
