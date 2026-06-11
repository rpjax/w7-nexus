import { useEffect, useId, useState } from 'react';
import { searchAccountsForPicker } from '../api/accounts';
import type { AccountPickerRow } from '../api/types';

type AccountPickerModalProps = {
  open: boolean;
  onClose: () => void;
  title?: string;
  subtitle?: string;
  disabledAccountIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: AccountPickerRow) => void;
};

const PAGE_SIZE = 8;

export function AccountPickerModal({
  open,
  onClose,
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

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  useEffect(() => {
    if (!open) return;
    setCurrentPage(1);
    setKeyword('');
    void load(1, '');
  }, [open]);

  async function load(page: number, term: string) {
    setLoading(true);
    try {
      const result = await searchAccountsForPicker({
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        keyword: term.trim() || null,
      });
      if (!result.ok) {
        setItems([]);
        setTotalItems(0);
        return;
      }
      setTotalItems(result.data?.total ?? 0);
      setItems(result.data?.items ?? []);
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
      <div className="dialog-card account-picker" onClick={(e) => e.stopPropagation()}>
        <div className="account-picker-header">
          <div>
            <h3 className="account-picker-title">{title}</h3>
            {subtitle ? <p className="account-picker-sub muted">{subtitle}</p> : null}
          </div>
          <button type="button" className="btn btn-ghost btn-small" onClick={onClose}>Fechar</button>
        </div>

        <div className="toolbar account-picker-toolbar">
          <div className="field grow">
            <label htmlFor={searchInputId}>Pesquisar</label>
            <input
              id={searchInputId}
              className="nexus-input account-picker-search"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') void search(); }}
              placeholder="ID ou nome de usuário..."
            />
          </div>
          <button type="button" className="btn btn-primary" onClick={() => void search()}>Buscar</button>
        </div>

        <div className="account-picker-list" tabIndex={-1}>
          {loading ? (
            <p className="muted account-picker-hint">Carregando…</p>
          ) : items.length === 0 ? (
            <p className="muted account-picker-hint">Nenhuma conta encontrada.</p>
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
                  <span className="account-picker-meta">
                    <span className="account-picker-name">{row.username}</span>
                    <span className="account-picker-id">{row.id}</span>
                  </span>
                  {disabled ? <span className="account-picker-badge">{disabledBadgeText}</span> : null}
                </button>
              );
            })
          )}
        </div>

        <div className="pagination account-picker-pagination">
          <button type="button" className="btn btn-ghost" onClick={() => void prevPage()} disabled={currentPage <= 1 || loading}>Anterior</button>
          <span className="muted">Página {currentPage} de {totalPages}</span>
          <button type="button" className="btn btn-ghost" onClick={() => void nextPage()} disabled={currentPage >= totalPages || loading}>Próxima</button>
        </div>
      </div>
    </div>
  );
}
