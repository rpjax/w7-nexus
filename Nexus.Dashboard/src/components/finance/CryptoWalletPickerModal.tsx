import { useEffect, useState } from 'react';
import { searchCryptoWallets } from '../../api/withdrawals';
import type { CryptoWalletRow } from '../../api/types';
import { shortId } from '../../utils/format';
import { IconButton } from '../IconButton';
import { PaginationBar } from '../ListControls';

type CryptoWalletPickerModalProps = {
  open: boolean;
  onClose: () => void;
  strawManAccountId: string;
  onSelected: (row: CryptoWalletRow) => void;
};

const PAGE_SIZE = 8;

export function CryptoWalletPickerModal({
  open,
  onClose,
  strawManAccountId,
  onSelected,
}: CryptoWalletPickerModalProps) {
  const [currentPage, setCurrentPage] = useState(1);
  const [items, setItems] = useState<CryptoWalletRow[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  useEffect(() => {
    if (!open || !strawManAccountId.trim()) return;
    setCurrentPage(1);
    void load(1);
  }, [open, strawManAccountId]);

  useEffect(() => {
    if (!open || !strawManAccountId.trim()) return;
    void load(currentPage);
  }, [currentPage]);

  async function load(page: number) {
    setLoading(true);
    setLoadError('');
    try {
      const result = await searchCryptoWallets({
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        strawManAccountId,
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
      <div className="dialog-card account-picker finance-picker" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <header className="account-picker-header">
          <div className="account-picker-heading">
            <h3 className="account-picker-title">Carteira crypto</h3>
            <p className="account-picker-sub">Carteiras cadastradas para o laranja selecionado.</p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        {loadError ? <p className="feedback error">{loadError}</p> : null}
        {loading ? <p className="muted account-picker-hint">Carregando carteiras…</p> : null}

        {!loading && items.length === 0 ? (
          <p className="muted account-picker-hint">Nenhuma carteira cadastrada para este laranja.</p>
        ) : (
          <div className="table-wrap finance-picker-table">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th>Rede</th>
                  <th>Ativo</th>
                  <th>Endereço</th>
                  <th>Label</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {items.map((row) => (
                  <tr key={row.id}>
                    <td data-label="Rede">{row.chain}</td>
                    <td data-label="Ativo">{row.asset}</td>
                    <td data-label="Endereço"><span className="mono token-mask" title={row.address}>{shortId(row.address, 22)}</span></td>
                    <td data-label="Label">{row.label ?? '—'}</td>
                    <td data-label="Ação">
                      <button
                        type="button"
                        className="btn btn-ghost btn-sm"
                        onClick={() => { onSelected(row); onClose(); }}
                      >
                        Selecionar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <footer className="account-picker-footer">
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
            onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
            disabled={loading}
          />
        </footer>
      </div>
    </div>
  );
}
