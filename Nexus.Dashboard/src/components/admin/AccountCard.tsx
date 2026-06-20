import type { AccountRow } from '../../api/types';
import { roleLabel } from '../../utils/accountAccess';
import { formatDateTime, shortId } from '../../utils/format';
import { IconButton } from '../IconButton';
import { AccountAccessEditor } from './AccountAccessEditor';

type AccountCardProps = {
  account: AccountRow;
  onMutated: () => void;
  onError: (message: string) => void;
};

function accountInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

export function AccountCard({ account, onMutated, onError }: AccountCardProps) {
  async function copyId() {
    try {
      await navigator.clipboard.writeText(account.id);
    } catch {
      // ignore clipboard errors
    }
  }

  const roles = account.roles ?? [];
  const permissions = account.permissions ?? [];

  return (
    <article className="account-card">
      <header className="account-card-header">
        <span className="account-card-avatar" aria-hidden="true">
          {accountInitial(account.username)}
        </span>
        <div className="account-card-heading">
          <div className="account-card-name-row">
            <h3 className="account-card-title">{account.username}</h3>
            {roles.map((role) => (
              <span key={role} className="account-card-role-pill">{roleLabel(role)}</span>
            ))}
          </div>
          <p className="account-card-id">
            <span className="mono" title={account.id}>{shortId(account.id, 24)}</span>
            <IconButton icon="copy" label="Copiar ID da conta" onClick={() => void copyId()} />
          </p>
        </div>
      </header>

      <div className="account-card-body">
        <AccountAccessEditor
          accountId={account.id}
          roles={roles}
          permissions={permissions}
          onMutated={onMutated}
          onError={onError}
        />

        <div className="account-card-meta-grid account-card-meta-grid--footer">
          <div className="account-card-meta-item">
            <span className="account-card-meta-label">Criada em</span>
            <p className="account-card-meta-value">{formatDateTime(account.createdAt)}</p>
          </div>
          <div className="account-card-meta-item">
            <span className="account-card-meta-label">Atualizada em</span>
            <p className="account-card-meta-value">{formatDateTime(account.lastUpdatedAt)}</p>
          </div>
        </div>
      </div>
    </article>
  );
}
