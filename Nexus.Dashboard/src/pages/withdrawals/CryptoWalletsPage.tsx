import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { createCryptoWallet, searchCryptoWallets } from '../../api/cryptoWallets';
import type { CryptoWalletRow } from '../../api/types';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { PixEntityField } from '../../components/finance/PixEntityField';
import { EmptyState } from '../../components/EmptyState';
import { IconButton } from '../../components/IconButton';
import { PaginationBar } from '../../components/ListControls';
import { ADDRESS_NAMESPACE_OPTIONS } from '../../utils/financeLabels';
import { formatCryptoWalletAddresses, formatCryptoWalletBalances } from '../../utils/cryptoWalletDisplay';
import { formatUtc, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function CryptoWalletsPage() {
  const { notifyError, notifySuccess } = useNotifications();
  const [ownerId, setOwnerId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [rows, setRows] = useState<CryptoWalletRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [busy, setBusy] = useState(false);

  const [namespace, setNamespace] = useState(1);
  const [address, setAddress] = useState('');
  const [memo, setMemo] = useState('');
  const [label, setLabel] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);
  const canSubmit = useMemo(
    () => Boolean(ownerId.trim() && address.trim()),
    [ownerId, address],
  );

  const load = useCallback(async (page: number, filterOwnerId: string) => {
    const result = await searchCryptoWallets({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      ownerId: filterOwnerId.trim() || null,
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
    void load(currentPage, ownerId);
  }, [currentPage, ownerId, load]);

  async function handleCreate() {
    if (!canSubmit) return;
    setBusy(true);
    try {
      const result = await createCryptoWallet({
        ownerId: ownerId.trim(),
        addresses: [{ namespace, address: address.trim(), memo: memo.trim() || null }],
        label: label.trim() || null,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Carteira crypto cadastrada.');
      setShowForm(false);
      setCurrentPage(1);
      await load(1, ownerId);
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <OpsWorkspace
        kicker="Financeiro"
        title="Carteiras crypto"
        lead="Cadastre endereços por namespace (EVM, Tron, Bitcoin…). Cada saldo carrega rede e ativo específicos."
        searchId="cryptoWalletStraw"
        searchLabel="Filtrar por laranja"
        searchPlaceholder="Selecione o laranja abaixo…"
        searchValue={strawLabel ?? ''}
        onSearchChange={() => {}}
        onSearch={() => setStrawPickerOpen(true)}
        onRefresh={() => void load(currentPage, ownerId)}
        totalItems={totalItems}
        totalLabel={`${totalItems} carteira(s)`}
        onCreate={() => {
          setShowForm((open) => {
            const next = !open;
            if (next && !ownerId.trim()) {
              setStrawPickerOpen(true);
            }
            return next;
          });
        }}
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
            {ownerId ? (
              <IconButton icon="x" label="Limpar laranja" onClick={() => { setOwnerId(''); setStrawLabel(null); setCurrentPage(1); }} />
            ) : null}
          </div>
          <Link className="btn btn-ghost" to="/dashboard/transfers">Voltar às transferências</Link>
        </div>

        {showForm ? (
          <section className="card ops-card inset-card">
            <h2 className="section-title">Cadastrar carteira</h2>
            <p className="muted small form-hint">
              Informe ao menos um endereço por namespace. Saldos aparecem após transferências creditarem valores nesta carteira.
            </p>
            <div className="form-grid form-grid-wide">
              <div className="field span-2">
                <PixEntityField
                  label="Laranja titular"
                  hint="Conta laranja dona desta carteira."
                  emptyLabel="Selecionar laranja"
                  name={strawLabel}
                  id={ownerId || null}
                  accent="warm"
                  onPick={() => setStrawPickerOpen(true)}
                  onClear={() => {
                    setOwnerId('');
                    setStrawLabel(null);
                    setCurrentPage(1);
                  }}
                />
              </div>
              <div className="field span-2">
                <label htmlFor="namespace">Namespace</label>
                <select id="namespace" className="nexus-input" value={namespace} onChange={(e) => setNamespace(Number(e.target.value))}>
                  {ADDRESS_NAMESPACE_OPTIONS.map((opt) => (
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
              <button
                type="button"
                className="btn btn-primary"
                disabled={busy || !canSubmit}
                onClick={() => void handleCreate()}
              >
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
                  <th>Endereços</th>
                  <th>Saldos</th>
                  <th>Label</th>
                  <th>Atualizado</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                    <td data-label="Dono"><span className="mono">{shortId(row.ownerId)}</span></td>
                    <td data-label="Endereços">{formatCryptoWalletAddresses(row)}</td>
                    <td data-label="Saldos">{formatCryptoWalletBalances(row)}</td>
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
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        onSelected={(row) => {
          setOwnerId(row.id);
          setStrawLabel(`${row.username} (${shortId(row.id)})`);
          setCurrentPage(1);
        }}
      />
    </>
  );
}
