import { useMemo, useState } from 'react';
import {
  grantAdministratorAccountPermission,
  grantAdministratorAccountRole,
  revokeAdministratorAccountPermission,
  revokeAdministratorAccountRole,
} from '../../api/administrator/accounts';
import {
  MANAGEABLE_ACCOUNT_PERMISSIONS,
  MANAGEABLE_ACCOUNT_ROLES,
  hasPermissionIgnoreCase,
  hasRoleIgnoreCase,
  permissionLabel,
  roleLabel,
} from '../../utils/accountAccess';
import { IconButton } from '../IconButton';

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

  const availableRoles = useMemo(
    () => MANAGEABLE_ACCOUNT_ROLES.filter((role) => !hasRoleIgnoreCase(roles, role)),
    [roles],
  );

  const availablePermissions = useMemo(
    () => MANAGEABLE_ACCOUNT_PERMISSIONS.filter((permission) => !hasPermissionIgnoreCase(permissions, permission)),
    [permissions],
  );

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

  return (
    <div className="account-access-editor">
      <section className="account-access-section">
        <div className="account-access-section__head">
          <h4 className="account-access-section__title">Funções (roles)</h4>
          <p className="account-access-section__hint muted small">
            Conceda ou remova papéis de acesso da conta.
          </p>
        </div>
        <div className="account-access-chips">
          {roles.length === 0 ? (
            <span className="account-access-empty muted small">Nenhuma função atribuída.</span>
          ) : (
            roles.map((role) => (
              <span key={role} className="account-access-chip account-access-chip--role">
                <span className="account-access-chip__label">{roleLabel(role)}</span>
                <span className="account-access-chip__code mono">{role}</span>
                <IconButton
                  icon="x"
                  label={`Remover função ${roleLabel(role)}`}
                  disabled={busyKey !== null}
                  onClick={() => void runAction(`role-remove:${role}`, () => revokeAdministratorAccountRole(accountId, role))}
                />
              </span>
            ))
          )}
        </div>
        {availableRoles.length > 0 ? (
          <div className="account-access-add-row">
            {availableRoles.map((role) => (
              <button
                key={role}
                type="button"
                className="account-access-add-btn"
                disabled={busyKey !== null}
                onClick={() => void runAction(`role-add:${role}`, () => grantAdministratorAccountRole(accountId, role))}
              >
                + {roleLabel(role)}
              </button>
            ))}
          </div>
        ) : null}
      </section>

      <section className="account-access-section">
        <div className="account-access-section__head">
          <h4 className="account-access-section__title">Permissões</h4>
          <p className="account-access-section__hint muted small">
            Permissões extras além das funções base.
          </p>
        </div>
        <div className="account-access-chips">
          {permissions.length === 0 ? (
            <span className="account-access-empty muted small">Nenhuma permissão atribuída.</span>
          ) : (
            permissions.map((permission) => (
              <span key={permission} className="account-access-chip account-access-chip--permission">
                <span className="account-access-chip__label">{permissionLabel(permission)}</span>
                <IconButton
                  icon="x"
                  label={`Remover permissão ${permissionLabel(permission)}`}
                  disabled={busyKey !== null}
                  onClick={() => void runAction(
                    `perm-remove:${permission}`,
                    () => revokeAdministratorAccountPermission(accountId, permission),
                  )}
                />
              </span>
            ))
          )}
        </div>
        {availablePermissions.length > 0 ? (
          <div className="account-access-add-row">
            {availablePermissions.map((permission) => (
              <button
                key={permission}
                type="button"
                className="account-access-add-btn account-access-add-btn--permission"
                disabled={busyKey !== null}
                onClick={() => void runAction(
                  `perm-add:${permission}`,
                  () => grantAdministratorAccountPermission(accountId, permission),
                )}
              >
                + {permissionLabel(permission)}
              </button>
            ))}
          </div>
        ) : null}
      </section>
    </div>
  );
}
