import { useEffect, useState } from 'react';
import { searchCryptoWallets } from '../../api/accountNodes';
import type { CryptoWalletRow } from '../../api/types';
import { formatCryptoWalletAddresses, formatCryptoWalletBalances } from '../../utils/cryptoWalletDisplay';
import { shortId } from '../../utils/format';
import { IconButton } from '../IconButton';
import { PaginationBar } from '../ListControls';

type CryptoWalletPickerModalProps = {
  open: boolean;
  onClose: () => void;
  strawManId: string;
  allowAnyStrawMan?: boolean;
  onSelected: (row: CryptoWalletRow) => void;
};

const PAGE_SIZE = 8;

export function CryptoWalletPickerModal({
  open,
  onClose,
  strawManId,
  allowAnyStrawMan = false,
  onSelected,
}: CryptoWalletPickerModalProps) {
  const [scopeSameStrawMan, setScopeSameStrawMan] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [items, setItems] = useState<CryptoWalletRow[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);
  const filterByStrawMan = allowAnyStrawMan ? scopeSameStrawMan : true;

  useEffect(() => {
    if (!open || !strawManId.trim()) return;
    setScopeSameStrawMan(true);
    setCurrentPage(1);
  }, [open, strawManId]);

  useEffect(() => {
    if (!open || !strawManId.trim()) return;
    void load(currentPage);
  }, [open, strawManId, currentPage, filterByStrawMan]);

  async function load(page: number) {
    setLoading(true);
    setLoadError('');
    try {
      const result = await searchCryptoWallets({
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        strawManId: filterByStrawMan ? strawManId : null,
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
            <p className="account-picker-sub">
              {filterByStrawMan
                ? 'Carteiras do laranja. O ativo exibido vem do saldo creditado em cada endereço.'
                : 'Todas as carteiras. O ativo exibido vem do saldo creditado em cada endereço.'}
            </p>
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </header>

        {allowAnyStrawMan ? (
          <div className="account-picker-search-row">
            <button
              type="button"
              className={`btn btn-sm ${filterByStrawMan ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => { setScopeSameStrawMan(true); setCurrentPage(1); }}
            >
              Laranja do saque
            </button>
            <button
              type="button"
              className={`btn btn-sm ${!filterByStrawMan ? 'btn-primary' : 'btn-ghost'}`}
              onClick={() => { setScopeSameStrawMan(false); setCurrentPage(1); }}
            >
              Todas as carteiras
            </button>
          </div>
        ) : null}

        {loadError ? <p className="feedback error">{loadError}</p> : null}
        {loading ? <p className="muted account-picker-hint">Carregando carteiras…</p> : null}

        {!loading && items.length === 0 ? (
          <p className="muted account-picker-hint">
            {filterByStrawMan
              ? 'Nenhuma carteira cadastrada para este laranja.'
              : 'Nenhuma carteira cadastrada.'}
          </p>
        ) : (
          <div className="table-wrap finance-picker-table">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th>Endereços</th>
                  <th>Saldos</th>
                  <th>Laranja</th>
                  <th>Label</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {items.map((row) => (
                  <tr key={row.id}>
                    <td data-label="Endereços">{formatCryptoWalletAddresses(row)}</td>
                    <td data-label="Saldos">{formatCryptoWalletBalances(row)}</td>
                    <td data-label="Laranja"><span className="mono">{shortId(row.strawManId)}</span></td>
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
