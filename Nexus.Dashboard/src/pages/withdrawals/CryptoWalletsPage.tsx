import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchOpAdminStrawMenPicker } from '../../api/accountPickerSources';
import { createCryptoWallet, searchCryptoWallets } from '../../api/withdrawals';
import type { CryptoWalletRow } from '../../api/types';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { EmptyState } from '../../components/EmptyState';
import { IconButton } from '../../components/IconButton';
import { PaginationBar } from '../../components/ListControls';
import { CHAIN_OPTIONS, CRYPTO_ASSET_OPTIONS } from '../../utils/financeLabels';
import { formatUtc, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function CryptoWalletsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [strawManAccountId, setStrawManAccountId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [rows, setRows] = useState<CryptoWalletRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [busy, setBusy] = useState(false);

  const [chain, setChain] = useState(1);
  const [asset, setAsset] = useState(1);
  const [address, setAddress] = useState('');
  const [memo, setMemo] = useState('');
  const [label, setLabel] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, strawId: string) => {
    const result = await searchCryptoWallets({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      strawManAccountId: strawId.trim() || null,
    });
    if (!result.ok) {
      notifyError(result.error);
      setRows([]);
      setTotalItems(0);
      return;
    }
    setRows(result.data?.items ?? []);
    setTotalItems(result.data?.total ?? 0);
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, strawManAccountId);
  }, [currentPage, strawManAccountId, load]);

  async function handleCreate() {
    if (!strawManAccountId.trim()) {
      notifyError('Selecione o laranja antes de cadastrar.');
      return;
    }
    if (!address.trim()) {
      notifyError('Informe o endereço da carteira.');
      return;
    }
    setBusy(true);
    try {
      const result = await createCryptoWallet({
        strawManAccountId: strawManAccountId.trim(),
        chain,
        asset,
        address: address.trim(),
        memo: memo.trim() || null,
        label: label.trim() || null,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Carteira crypto cadastrada.');
      setShowForm(false);
      setCurrentPage(1);
      await load(1, strawManAccountId);
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <OpsWorkspace
        kicker="Financeiro"
        title="Carteiras crypto"
        lead="Endereços de destino para saques em cripto vinculados a laranjas."
        searchId="cryptoWalletStraw"
        searchLabel="Filtrar por laranja"
        searchPlaceholder="Selecione o laranja abaixo…"
        searchValue={strawLabel ?? ''}
        onSearchChange={() => {}}
        onSearch={() => setStrawPickerOpen(true)}
        onRefresh={() => void load(currentPage, strawManAccountId)}
        totalItems={totalItems}
        totalLabel={`${totalItems} carteira(s)`}
        onCreate={() => setShowForm((v) => !v)}
        createLabel={showForm ? 'Fechar formulário' : 'Nova carteira'}
        footer={totalItems > 0 ? (
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
            onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
          />
        ) : undefined}
      >
        <div className="ops-workspace__filters">
          <div className="account-select-row">
            <button type="button" className="account-select-trigger" onClick={() => setStrawPickerOpen(true)}>
              {strawLabel ?? 'Selecionar laranja para filtrar / cadastrar'}
            </button>
            {strawManAccountId ? (
              <IconButton icon="x" label="Limpar laranja" onClick={() => { setStrawManAccountId(''); setStrawLabel(null); setCurrentPage(1); }} />
            ) : null}
          </div>
          <Link className="btn btn-ghost" to="/dashboard/withdrawals">Voltar aos saques</Link>
        </div>

        {showForm ? (
          <section className="card ops-card inset-card">
            <h2 className="section-title">Cadastrar carteira</h2>
            <div className="form-grid form-grid-wide">
              <div className="field">
                <label htmlFor="chain">Rede</label>
                <select id="chain" className="nexus-input" value={chain} onChange={(e) => setChain(Number(e.target.value))}>
                  {CHAIN_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label htmlFor="asset">Ativo</label>
                <select id="asset" className="nexus-input" value={asset} onChange={(e) => setAsset(Number(e.target.value))}>
                  {CRYPTO_ASSET_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="field span-2">
                <label htmlFor="address">Endereço</label>
                <input id="address" className="nexus-input mono" value={address} onChange={(e) => setAddress(e.target.value)} />
              </div>
              <div className="field span-2">
                <label htmlFor="memo">Memo / tag <span className="muted small">opcional</span></label>
                <input id="memo" className="nexus-input" value={memo} onChange={(e) => setMemo(e.target.value)} />
              </div>
              <div className="field span-2">
                <label htmlFor="walletLabel">Label <span className="muted small">opcional</span></label>
                <input id="walletLabel" className="nexus-input" value={label} onChange={(e) => setLabel(e.target.value)} />
              </div>
            </div>
            <div className="card-actions">
              <button type="button" className="btn btn-primary" disabled={busy} onClick={() => void handleCreate()}>
                {busy ? 'Salvando…' : 'Cadastrar carteira'}
              </button>
            </div>
          </section>
        ) : null}

        {rows.length === 0 ? (
          <EmptyState title="Nenhuma carteira encontrada" message="Selecione um laranja ou cadastre uma nova carteira." />
        ) : (
          <div className="table-wrap table-top-gap">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Laranja</th>
                  <th>Rede</th>
                  <th>Ativo</th>
                  <th>Endereço</th>
                  <th>Label</th>
                  <th>Atualizado</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                    <td data-label="Laranja"><span className="mono">{shortId(row.strawManAccountId)}</span></td>
                    <td data-label="Rede">{row.chain}</td>
                    <td data-label="Ativo">{row.asset}</td>
                    <td data-label="Endereço"><span className="mono token-mask" title={row.address}>{shortId(row.address, 24)}</span></td>
                    <td data-label="Label">{row.label ?? '—'}</td>
                    <td data-label="Atualizado" className="muted small">{formatUtc(row.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </OpsWorkspace>

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        searchAccounts={searchOpAdminStrawMenPicker}
        title="Conta laranja"
        onSelected={(row) => {
          setStrawManAccountId(row.id);
          setStrawLabel(`${row.username} (${shortId(row.id)})`);
          setCurrentPage(1);
        }}
      />
    </>
  );
}
