import type { ReactNode } from 'react';
import { Icon } from '../IconButton';
import { ToolbarSearchActions } from '../ListControls';
import { PageHeading } from '../../layouts/PageHeading';

type OpsWorkspaceProps = {
  title: string;
  kicker?: string;
  kickerVariant?: 'default' | 'admin';
  lead?: string;
  searchId: string;
  searchLabel: string;
  searchPlaceholder: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  onSearch: () => void;
  onRefresh: () => void;
  totalItems: number;
  totalLabel?: string;
  onCreate?: () => void;
  createLabel?: string;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
};

export function OpsWorkspace({
  title,
  kicker,
  kickerVariant = 'default',
  lead,
  searchId,
  searchLabel,
  searchPlaceholder,
  searchValue,
  onSearchChange,
  onSearch,
  onRefresh,
  totalItems,
  totalLabel,
  onCreate,
  createLabel = 'Nova operação',
  children,
  footer,
  className = '',
}: OpsWorkspaceProps) {
  const totalText = totalLabel ?? `${totalItems} registro(s)`;

  return (
    <div className={`ops-page ${className}`.trim()}>
      <PageHeading
        title={title}
        kicker={kicker}
        kickerVariant={kickerVariant}
        subtitle={lead}
      />

      <section className="ops-workspace">
        <header className="ops-workspace__header">
          <div className="ops-workspace__toolbar">
            <div className="field grow">
              <label htmlFor={searchId}>{searchLabel}</label>
              <input
                id={searchId}
                className="nexus-input"
                value={searchValue}
                onChange={(e) => onSearchChange(e.target.value)}
                placeholder={searchPlaceholder}
                onKeyDown={(e) => { if (e.key === 'Enter') onSearch(); }}
              />
            </div>
            <ToolbarSearchActions onSearch={onSearch} onRefresh={onRefresh} />
            {onCreate ? (
              <button type="button" className="btn btn-primary btn-with-icon ops-workspace__create" onClick={onCreate}>
                <Icon name="plus" />
                <span className="ops-workspace__create-label">{createLabel}</span>
              </button>
            ) : null}
          </div>
          <div className="ops-workspace__meta">
            <span className="muted small">{totalText}</span>
          </div>
        </header>

        <div className="ops-workspace__divider" aria-hidden="true" />

        <div className="ops-workspace__body">
          {children}
        </div>

        {footer ? (
          <footer className="ops-workspace__footer">
            {footer}
          </footer>
        ) : null}
      </section>
    </div>
  );
}
