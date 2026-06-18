import { useEffect, useId, useState } from 'react';
import type { AccountPickerSearchFn } from '../api/accountPicker';
import type { AccountPickerRow } from '../api/types';
import { IconButton } from './IconButton';
import { PaginationBar } from './ListControls';
import { shortId } from '../utils/format';

type AccountPickerModalProps = {
  open: boolean;
  onClose: () => void;
  searchAccounts: AccountPickerSearchFn;
  title?: string;
  subtitle?: string;
  disabledAccountIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: AccountPickerRow) => void;
};

const PAGE_SIZE = 8;

function accountInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

export function AccountPickerModal({
  open,
  onClose,
  searchAccounts,
  title = 'Selecionar conta',
  subtitle,
  disabledAccountIds,
  disabledBadgeText = 'Já vinculado',
  onSelected,
}: AccountPickerModalProps) {
  const searchInputId = useId();
  const [keyword, setKeyword] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [items, setItems] = useState<AccountPickerRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  useEffect(() => {
    if (!open) return;
    setCurrentPage(1);
    setKeyword('');
    setLoadError('');
    void load(1, '');
  }, [open, searchAccounts]);

  async function load(page: number, term: string) {
    setLoading(true);
    setLoadError('');
    try {
      const result = await searchAccounts({
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
    return disabledAccountIds?.has(id) ?? false;
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

  function pick(row: AccountPickerRow) {
    if (isDisabled(row.id)) return;
    onSelected(row);
    onClose();
  }

  if (!open) return null;

  return (
    <div className="dialog-backdrop dialog-backdrop--picker account-picker-backdrop" onClick={onClose}>
      <div
        className="dialog-card account-picker"
        role="dialog"
        aria-modal="true"
        aria-labelledby="account-picker-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="account-picker-header">
          <div className="account-picker-heading">
            <h3 id="account-picker-title" className="account-picker-title">{title}</h3>
            {subtitle ? <p className="account-picker-sub">{subtitle}</p> : null}
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        <div className="account-picker-search-row">
          <input
            id={searchInputId}
            className="nexus-input account-picker-search"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') void search(); }}
            placeholder="Buscar por nome ou ID…"
            aria-label="Buscar contas"
          />
          <IconButton
            icon="search"
            label="Buscar contas"
            variant="primary"
            onClick={() => void search()}
            disabled={loading}
          />
        </div>

        <div className="account-picker-list" tabIndex={-1}>
          {loading ? (
            <div className="account-picker-state">
              <span className="account-picker-spinner" aria-hidden="true" />
              <p className="account-picker-hint">Carregando contas…</p>
            </div>
          ) : loadError ? (
            <div className="account-picker-state account-picker-state--error">
              <p className="account-picker-hint account-picker-error">{loadError}</p>
            </div>
          ) : items.length === 0 ? (
            <div className="account-picker-state">
              <p className="account-picker-hint">Nenhuma conta encontrada.</p>
              <p className="account-picker-hint-sub">Tente outro termo ou limpe a busca.</p>
            </div>
          ) : (
            items.map((row) => {
              const disabled = isDisabled(row.id);
              return (
                <button
                  key={row.id}
                  type="button"
                  className={`account-picker-row ${disabled ? 'is-disabled' : ''}`}
                  disabled={disabled}
                  onClick={() => pick(row)}
                >
                  <span className="account-picker-avatar" aria-hidden="true">
                    {accountInitial(row.username)}
                  </span>
                  <span className="account-picker-meta">
                    <span className="account-picker-name-row">
                      <span className="account-picker-name">{row.username}</span>
                      {row.roles?.map((role) => (
                        <span key={role} className="account-picker-role-pill">{role}</span>
                      ))}
                    </span>
                    <span className="account-picker-id mono" title={row.id}>
                      {shortId(row.id, 24)}
                    </span>
                  </span>
                  {disabled ? (
                    <span className="account-picker-badge">{disabledBadgeText}</span>
                  ) : (
                    <span className="account-picker-chevron" aria-hidden="true">›</span>
                  )}
                </button>
              );
            })
          )}
        </div>

        <footer className="account-picker-footer">
          <span className="account-picker-count">
            {totalItems === 0 ? 'Sem resultados' : `${totalItems} conta${totalItems === 1 ? '' : 's'}`}
          </span>
          <PaginationBar
            className="account-picker-pagination"
            currentPage={currentPage}
            totalPages={totalPages}
            disabled={loading}
            onPrev={() => void prevPage()}
            onNext={() => void nextPage()}
          />
        </footer>
      </div>
    </div>
  );
}
