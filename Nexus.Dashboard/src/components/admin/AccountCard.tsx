import { useState } from 'react';
import type { AccountRow } from '../../api/types';
import { roleLabel, roleTone, summarizeRoles } from '../../utils/accountAccess';
import { formatDateTime, shortId } from '../../utils/format';
import { IconButton } from '../IconButton';
import { AccountAccessEditor } from './AccountAccessEditor';

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
    <article className={`account-card${expanded ? ' account-card--expanded' : ''}`}>
      <header className="account-card-header">
        <button
          type="button"
          className="account-card-header__toggle"
          aria-expanded={expanded}
          onClick={() => setExpanded((open) => !open)}
        >
          <span className="account-card-avatar" aria-hidden="true">
            {accountInitial(account.username)}
          </span>
          <span className="account-card-heading">
            <span className="account-card-name-row">
              <strong className="account-card-title">@{account.username}</strong>
              {roles.length > 0 ? (
                <span className="account-card-role-stack" aria-label={roleSummary}>
                  {roles.map((role) => (
                    <span
                      key={role}
                      className={`account-card-role-pill account-card-role-pill--${roleTone(role)}`}
                    >
                      {roleLabel(role)}
                    </span>
                  ))}
                </span>
              ) : (
                <span className="account-card-role-pill account-card-role-pill--empty">Sem funções</span>
              )}
            </span>
            <span className="account-card-summary muted small">
              {permissions.length > 0
                ? `${permissions.length} permissão(ões) extra(s)`
                : 'Somente funções base'}
              <span className="account-card-summary__sep" aria-hidden="true">·</span>
              Atualizada {formatDateTime(account.lastUpdatedAt)}
            </span>
          </span>
          <span className="account-card-chevron" aria-hidden="true">{expanded ? '▾' : '▸'}</span>
        </button>
      </header>

      {expanded ? (
        <div className="account-card-body">
          <AccountAccessEditor
            accountId={account.id}
            roles={roles}
            permissions={permissions}
            onMutated={onMutated}
            onError={onError}
          />

          <section className="account-card-technical">
            <button
              type="button"
              className="account-card-technical__toggle"
              aria-expanded={technicalOpen}
              onClick={() => setTechnicalOpen((open) => !open)}
            >
              Detalhes técnicos
              <span aria-hidden="true">{technicalOpen ? '▾' : '▸'}</span>
            </button>
            {technicalOpen ? (
              <div className="account-card-technical__grid">
                <div>
                  <span className="account-card-meta-label">ID</span>
                  <p className="account-card-meta-value mono">
                    <span title={account.id}>{shortId(account.id, 28)}</span>
                    <IconButton icon="copy" label="Copiar ID da conta" onClick={() => void copyId()} />
                  </p>
                </div>
                <div>
                  <span className="account-card-meta-label">Criada em</span>
                  <p className="account-card-meta-value">{formatDateTime(account.createdAt)}</p>
                </div>
              </div>
            ) : null}
          </section>
        </div>
      ) : null}
    </article>
  );
}
