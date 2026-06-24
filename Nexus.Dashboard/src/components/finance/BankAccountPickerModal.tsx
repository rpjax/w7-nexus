import { useEffect, useState } from 'react';
import { searchBankAccounts } from '../../api/accountNodes';
import type { BankAccountRow } from '../../api/types';
import { BankAccountCard } from './BankAccountCard';
import { IconButton } from '../IconButton';
import { PaginationBar } from '../ListControls';

type BankAccountPickerModalProps = {
  open: boolean;
  onClose: () => void;
  strawManId: string;
  onSelected: (row: BankAccountRow) => void;
  onCreateRequested?: () => void;
};

const PAGE_SIZE = 8;

export function BankAccountPickerModal({
  open,
  onClose,
  strawManId,
  onSelected,
  onCreateRequested,
}: BankAccountPickerModalProps) {
  const [currentPage, setCurrentPage] = useState(1);
  const [items, setItems] = useState<BankAccountRow[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  useEffect(() => {
    if (!open || !strawManId.trim()) return;
    setCurrentPage(1);
  }, [open, strawManId]);

  useEffect(() => {
    if (!open || !strawManId.trim()) return;
    void load(currentPage);
  }, [open, strawManId, currentPage]);

  async function load(page: number) {
    setLoading(true);
    setLoadError('');
    try {
      const result = await searchBankAccounts({
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        strawManId,
      });
      if (!result.ok) {
        setItems([]);
        setTotalItems(0);
        setLoadError(result.error);
        return;
      }
      setItems(result.data?.items ?? []);
      setTotalItems(result.data?.total ?? 0);
    } finally {
      setLoading(false);
    }
  }

  if (!open) return null;

  return (
    <div className="dialog-backdrop dialog-backdrop--picker" onClick={onClose}>
      <div className="dialog-card account-picker finance-picker bank-picker-modal" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <header className="account-picker-header">
          <div className="account-picker-heading">
            <h3 className="account-picker-title">Conta bancária</h3>
            <p className="account-picker-sub">Contas cadastradas para o laranja selecionado.</p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        {loadError ? <p className="feedback error">{loadError}</p> : null}
        {loading ? <p className="muted account-picker-hint">Carregando contas…</p> : null}

        {!loading && items.length === 0 ? (
          <div className="bank-picker-empty">
            <p className="muted account-picker-hint">Nenhuma conta bancária cadastrada para este laranja.</p>
            {onCreateRequested ? (
              <button type="button" className="btn btn-primary" onClick={() => { onClose(); onCreateRequested(); }}>
                Cadastrar conta para este laranja
              </button>
            ) : null}
          </div>
        ) : (
          <ul className="bank-account-list bank-account-list--picker">
            {items.map((row) => (
              <BankAccountCard
                key={row.id}
                row={row}
                variant="compact"
                selectable
                onSelect={(selected) => {
                  onSelected(selected);
                  onClose();
                }}
              />
            ))}
          </ul>
        )}

        <footer className="account-picker-footer account-picker-footer--split">
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
            onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
            disabled={loading}
          />
          {onCreateRequested ? (
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => { onClose(); onCreateRequested(); }}>
              Cadastrar nova conta
            </button>
          ) : null}
        </footer>
      </div>
    </div>
  );
}
