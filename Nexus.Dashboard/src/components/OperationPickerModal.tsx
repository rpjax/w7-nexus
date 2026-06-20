import { useEffect, useId, useState } from 'react';
import type { OperationPickerSearchFn } from '../api/operationPicker';
import type { OperationPickerRow } from '../api/types';
import { IconButton } from './IconButton';
import { PaginationBar } from './ListControls';

type OperationPickerModalProps = {
  open: boolean;
  onClose: () => void;
  searchOperations: OperationPickerSearchFn;
  title?: string;
  subtitle?: string;
  disabledOperationIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: OperationPickerRow) => void;
};

const PAGE_SIZE = 8;

export function OperationPickerModal({
  open,
  onClose,
  searchOperations,
  title = 'Selecionar operação',
  subtitle,
  disabledOperationIds,
  disabledBadgeText = 'Indisponível',
  onSelected,
}: OperationPickerModalProps) {
  const searchInputId = useId();
  const [keyword, setKeyword] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [items, setItems] = useState<OperationPickerRow[]>([]);
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
      const result = await searchOperations({
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        keyword: term.trim() || null,
      });
      if (!result.ok) {
        setItems([]);
        setTotalItems(0);
        return;
      }
      setTotalItems(result.total);
      setItems(result.items);
    } finally {
      setLoading(false);
    }
  }

  function isDisabled(id: string) {
    return disabledOperationIds?.has(id) ?? false;
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

  function pick(row: OperationPickerRow) {
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
          <IconButton icon="x" label="Fechar" onClick={onClose} />
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
              placeholder="Nome, ID ou descrição…"
            />
          </div>
          <IconButton icon="search" label="Buscar operações" variant="primary" onClick={() => void search()} />
        </div>

        <div className="account-picker-list" tabIndex={-1}>
          {loading ? (
            <p className="muted account-picker-hint">Carregando…</p>
          ) : items.length === 0 ? (
            <p className="muted account-picker-hint">Nenhuma operação encontrada.</p>
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
                    <span className="account-picker-name">{row.name}</span>
                    <span className="account-picker-id">{row.id}</span>
                  </span>
                  {disabled ? <span className="account-picker-badge">{disabledBadgeText}</span> : null}
                </button>
              );
            })
          )}
        </div>

        <PaginationBar
          className="account-picker-pagination"
          currentPage={currentPage}
          totalPages={totalPages}
          disabled={loading}
          onPrev={() => void prevPage()}
          onNext={() => void nextPage()}
        />
      </div>
    </div>
  );
}
