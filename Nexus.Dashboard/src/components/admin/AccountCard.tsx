import type { AccountRow } from '../../api/types';
import { formatDateTime, shortId } from '../../utils/format';

type AccountCardProps = {
  account: AccountRow;
};

function accountInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

export function AccountCard({ account }: AccountCardProps) {
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
              <span key={role} className="account-card-role-pill">{role}</span>
            ))}
          </div>
          <p className="account-card-id">
            <span className="mono" title={account.id}>{shortId(account.id, 24)}</span>
            <button type="button" className="btn btn-ghost btn-small account-card-copy-id" onClick={() => void copyId()}>
              Copiar ID
            </button>
          </p>
        </div>
      </header>

      <div className="account-card-body">
        <div className="account-card-meta-grid">
          <div className="account-card-meta-item account-card-meta-item--wide">
            <span className="account-card-meta-label">Permissões</span>
            {permissions.length === 0 ? (
              <p className="account-card-meta-value account-card-meta-empty">Nenhuma permissão atribuída.</p>
            ) : (
              <ul className="account-card-tag-list">
                {permissions.map((permission) => (
                  <li key={permission} className="account-card-tag">{permission}</li>
                ))}
              </ul>
            )}
          </div>
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
