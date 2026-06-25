import { useCallback, useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { createCryptoWallet, searchCryptoWallets } from '../../api/cryptoWallets';
import type { CryptoWalletRow } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { CryptoWalletCard } from '../../components/finance/CryptoWalletCard';
import { CryptoWalletCreateModal } from '../../components/finance/CryptoWalletCreateModal';
import type { CryptoWalletCreatePayload } from '../../components/finance/CryptoWalletCreateModal';
import { PixEntityField } from '../../components/finance/PixEntityField';
import { EmptyState } from '../../components/EmptyState';
import { IconButton } from '../../components/IconButton';
import { PaginationBar } from '../../components/ListControls';
import { PageHeading } from '../../layouts/PageHeading';
import { cryptoWalletSearchText } from '../../utils/cryptoWalletDisplay';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

type CryptoWalletsLocationState = {
  ownerId?: string;
  strawManId?: string;
  strawLabel?: string;
  openCreate?: boolean;
  returnTo?: string;
};

export function CryptoWalletsPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as CryptoWalletsLocationState | null;
  const { notifyError, notifySuccess } = useNotifications();

  const [returnTo] = useState(() => locationState?.returnTo ?? null);
  const [ownerId, setOwnerId] = useState(() => locationState?.ownerId ?? locationState?.strawManId ?? '');
  const [strawLabel, setStrawLabel] = useState<string | null>(() => locationState?.strawLabel ?? null);
  const [rows, setRows] = useState<CryptoWalletRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(
    () => Boolean(locationState?.openCreate && (locationState?.ownerId ?? locationState?.strawManId)),
  );
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);
  const hasOwner = Boolean(ownerId.trim());

  const filteredRows = useMemo(() => {
    const term = filter.trim().toLowerCase();
    if (!term) return rows;
    return rows.filter((row) => cryptoWalletSearchText(row).includes(term));
  }, [rows, filter]);

  const load = useCallback(async (page: number, filterOwnerId: string) => {
    setLoading(true);
    try {
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
    } finally {
      setLoading(false);
    }
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, ownerId);
  }, [currentPage, ownerId, load]);

  function clearOwner() {
    setOwnerId('');
    setStrawLabel(null);
    setCreateOpen(false);
    setCurrentPage(1);
    setFilter('');
  }

  function handleWalletUpdated(updated: CryptoWalletRow) {
    setRows((prev) => prev.map((row) => (row.id === updated.id ? updated : row)));
    notifySuccess('Carteira atualizada.');
  }

  async function handleCreate(payload: CryptoWalletCreatePayload) {
    if (!ownerId.trim()) {
      notifyError('Selecione o laranja antes de cadastrar.');
      return;
    }
    setBusy(true);
    try {
      const result = await createCryptoWallet({
        ownerId: ownerId.trim(),
        addresses: [{ namespace: payload.namespace, address: payload.address, memo: payload.memo }],
        label: payload.label,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Carteira crypto cadastrada.');
      setCreateOpen(false);
      setCurrentPage(1);
      await load(1, ownerId);

      if (returnTo && result.data) {
        navigate(returnTo, {
          replace: true,
          state: { cryptoWallet: result.data },
        });
      }
    } finally {
      setBusy(false);
    }
  }

  const heroCount = hasOwner
    ? `${totalItems} carteira${totalItems === 1 ? '' : 's'}`
    : 'Escolha um laranja';

  const heroHint = hasOwner
    ? `Destinos on-chain de ${strawLabel ?? 'laranja selecionado'}.`
    : 'Carteiras são cadastradas por laranja e usadas em saques e movimentações crypto.';

  return (
    <div className="ops-page crypto-wallets-page">
      <PageHeading
        kicker="Financeiro"
        title="Carteiras crypto"
        subtitle="Endereços on-chain vinculados a contas laranja."
        backLink={{ to: '/dashboard/transfers', label: 'Transferências' }}
      />

      <section className="pix-workspace crypto-workspace" aria-labelledby="crypto-wallets-title">
        <header className="pix-workspace__hero crypto-workspace__hero">
          <div className="pix-workspace__hero-main">
            <span className="crypto-workspace__badge">Destino on-chain</span>
            <p className="crypto-workspace__count" aria-live="polite">{heroCount}</p>
            <p className="pix-workspace__hero-hint muted small">{heroHint}</p>
          </div>
          <div className="pix-workspace__hero-mark crypto-workspace__mark" aria-hidden="true">
            <span className="crypto-workspace__mark-icon">WEB3</span>
          </div>
        </header>

        <div className="pix-workspace__divider" aria-hidden="true" />

        <div className="pix-workspace__body">
          {!hasOwner ? (
            <section className="pix-section crypto-section">
              <div className="crypto-onboarding">
                <div className="bank-section__head-text">
                  <span className="bank-section__kicker">Começar</span>
                  <h2 id="crypto-wallets-title" className="bank-section-title">Selecione o laranja</h2>
                  <p className="bank-section-desc muted small">
                    Cada laranja pode ter várias carteiras. Escolha qual titular deseja gerenciar.
                  </p>
                </div>
                <PixEntityField
                  label="Laranja"
                  emptyLabel="Selecionar laranja"
                  name={strawLabel}
                  id={null}
                  onPick={() => setStrawPickerOpen(true)}
                  accent="warm"
                />
              </div>
            </section>
          ) : (
            <section className="pix-section crypto-managed-section crypto-section">
              <div className="bank-context-bar">
                <div className="bank-context-bar__main">
                  <span className="bank-context-bar__avatar crypto-context-bar__avatar" aria-hidden="true">
                    {(strawLabel ?? '?')[0]?.toUpperCase()}
                  </span>
                  <div className="bank-context-bar__text">
                    <span className="bank-context-bar__kicker">Laranja ativo</span>
                    <strong className="bank-context-bar__name">{strawLabel ?? ownerId}</strong>
                  </div>
                </div>
                <div className="bank-context-bar__actions">
                  <button type="button" className="bank-context-bar__change" onClick={() => setStrawPickerOpen(true)}>
                    Trocar
                  </button>
                  <IconButton icon="x" label="Limpar laranja" onClick={clearOwner} />
                </div>
              </div>

              <div className="bank-section__head">
                <div className="bank-section__head-text">
                  <span className="bank-section__kicker">Carteiras</span>
                  <h2 className="bank-section-title">Cadastradas</h2>
                  <p className="bank-section-desc muted small">
                    {loading ? 'Carregando…' : `${totalItems} carteira(s) neste laranja.`}
                  </p>
                </div>
                <button type="button" className="btn btn-primary btn-sm bank-section-create" onClick={() => setCreateOpen(true)}>
                  Nova carteira
                </button>
              </div>

              <div className="bank-section__body">
                <div className="bank-list-toolbar">
                  <input
                    className="nexus-input bank-list-toolbar__search"
                    value={filter}
                    onChange={(e) => setFilter(e.target.value)}
                    placeholder="Buscar por apelido ou endereço…"
                    aria-label="Filtrar carteiras"
                  />
                  <IconButton
                    icon="refresh"
                    label="Atualizar lista"
                    onClick={() => void load(currentPage, ownerId)}
                    disabled={loading}
                  />
                </div>

                {loading ? (
                  <p className="muted bank-list-loading">Carregando carteiras…</p>
                ) : rows.length === 0 ? (
                  <div className="bank-empty-block">
                    <EmptyState
                      title="Nenhuma carteira cadastrada"
                      message="Cadastre a primeira carteira de destino para este laranja."
                    />
                    <button type="button" className="btn btn-primary" onClick={() => setCreateOpen(true)}>
                      Cadastrar primeira carteira
                    </button>
                  </div>
                ) : filteredRows.length === 0 ? (
                  <EmptyState title="Nenhum resultado" message="Ajuste o filtro ou cadastre uma nova carteira." />
                ) : (
                  <ul className="crypto-wallet-list">
                    {filteredRows.map((row) => (
                      <CryptoWalletCard
                        key={row.id}
                        row={row}
                        onUpdated={handleWalletUpdated}
                        onError={notifyError}
                      />
                    ))}
                  </ul>
                )}

                {totalItems > PAGE_SIZE ? (
                  <PaginationBar
                    currentPage={currentPage}
                    totalPages={totalPages}
                    onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  />
                ) : null}
              </div>
            </section>
          )}
        </div>
      </section>

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        onSelected={(row) => {
          setOwnerId(row.id);
          setStrawLabel(row.username);
          setCurrentPage(1);
          setFilter('');
        }}
      />

      <CryptoWalletCreateModal
        open={createOpen}
        busy={busy}
        strawLabel={strawLabel}
        onClose={() => setCreateOpen(false)}
        onSubmit={(payload) => void handleCreate(payload)}
      />
    </div>
  );
}
