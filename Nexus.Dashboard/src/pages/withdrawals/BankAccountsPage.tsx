import { useCallback, useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { searchOpAdminStrawMenPicker } from '../../api/accountPickerSources';
import { createBankAccount, searchBankAccounts } from '../../api/withdrawals';
import type { BankAccountRow } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { BankAccountCard } from '../../components/finance/BankAccountCard';
import { BankAccountCreateModal } from '../../components/finance/BankAccountCreateModal';
import type { BankAccountCreatePayload } from '../../components/finance/BankAccountCreateModal';
import { PixEntityField } from '../../components/finance/PixEntityField';
import { EmptyState } from '../../components/EmptyState';
import { IconButton } from '../../components/IconButton';
import { PaginationBar } from '../../components/ListControls';
import { PageHeading } from '../../layouts/PageHeading';
import { bankAccountSearchText } from '../../utils/bankAccountDisplay';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

type BankAccountsLocationState = {
  strawManAccountId?: string;
  strawLabel?: string;
  openCreate?: boolean;
  returnTo?: string;
};

export function BankAccountsPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as BankAccountsLocationState | null;
  const { notifyError, notifySuccess } = useNotifications();

  const [returnTo] = useState(() => locationState?.returnTo ?? null);
  const [strawManAccountId, setStrawManAccountId] = useState(() => locationState?.strawManAccountId ?? '');
  const [strawLabel, setStrawLabel] = useState<string | null>(() => locationState?.strawLabel ?? null);
  const [rows, setRows] = useState<BankAccountRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(
    () => Boolean(locationState?.openCreate && locationState?.strawManAccountId),
  );
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState('');

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);
  const hasStraw = Boolean(strawManAccountId.trim());

  const filteredRows = useMemo(() => {
    const term = filter.trim().toLowerCase();
    if (!term) return rows;
    return rows.filter((row) => bankAccountSearchText(row).includes(term));
  }, [rows, filter]);

  const load = useCallback(async (page: number, strawId: string) => {
    setLoading(true);
    try {
      const result = await searchBankAccounts({
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
    } finally {
      setLoading(false);
    }
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, strawManAccountId);
  }, [currentPage, strawManAccountId, load]);

  function clearStraw() {
    setStrawManAccountId('');
    setStrawLabel(null);
    setCreateOpen(false);
    setCurrentPage(1);
    setFilter('');
  }

  function handleLabelUpdated(updated: BankAccountRow) {
    setRows((prev) => prev.map((row) => (row.id === updated.id ? updated : row)));
    notifySuccess('Apelido atualizado.');
  }

  async function handleCreate(payload: BankAccountCreatePayload) {
    if (!strawManAccountId.trim()) {
      notifyError('Selecione o laranja antes de cadastrar.');
      return;
    }
    setBusy(true);
    try {
      const result = await createBankAccount({
        strawManAccountId: strawManAccountId.trim(),
        ...payload,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Conta bancária cadastrada.');
      setCreateOpen(false);
      setCurrentPage(1);
      await load(1, strawManAccountId);

      if (returnTo && result.data) {
        navigate(returnTo, {
          replace: true,
          state: {
            bankAccount: result.data,
          },
        });
      }
    } finally {
      setBusy(false);
    }
  }

  const heroCount = hasStraw
    ? `${totalItems} conta${totalItems === 1 ? '' : 's'}`
    : 'Escolha um laranja';

  const heroHint = hasStraw
    ? `Destinos PIX de ${strawLabel ?? 'laranja selecionado'}.`
    : 'Contas bancárias são cadastradas por laranja e usadas em saques PIX.';

  return (
    <div className="ops-page bank-accounts-page">
      <PageHeading
        kicker="Financeiro"
        title="Contas bancárias"
        subtitle="Contas de destino PIX vinculadas a contas laranja."
        backLink={{ to: '/dashboard/withdrawals', label: 'Saques' }}
      />

      <section className="pix-workspace bank-workspace" aria-labelledby="bank-accounts-title">
        <header className="pix-workspace__hero bank-workspace__hero">
          <div className="pix-workspace__hero-main">
            <span className="bank-workspace__badge">Destino PIX</span>
            <p className="bank-workspace__count" aria-live="polite">{heroCount}</p>
            <p className="pix-workspace__hero-hint muted small">{heroHint}</p>
          </div>
          <div className="pix-workspace__hero-mark bank-workspace__mark" aria-hidden="true">
            <span className="bank-workspace__mark-icon">BANCO</span>
          </div>
        </header>

        <div className="pix-workspace__divider" aria-hidden="true" />

        <div className="pix-workspace__body">
          {!hasStraw ? (
            <section className="admin-op-section pix-section">
              <div className="bank-onboarding">
                <div className="admin-op-section__head-text">
                  <span className="admin-op-section__kicker">Começar</span>
                  <h2 id="bank-accounts-title" className="admin-op-section-title">Selecione o laranja</h2>
                  <p className="admin-op-section-desc muted small">
                    Cada laranja pode ter várias contas de destino. Escolha qual contexto deseja gerenciar.
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
                <p className="bank-onboarding__hint muted small">
                  Depois de selecionar, você verá as contas cadastradas e poderá adicionar novas.
                </p>
              </div>
            </section>
          ) : (
            <section className="admin-op-section pix-section bank-managed-section">
              <div className="bank-context-bar">
                <div className="bank-context-bar__main">
                  <span className="bank-context-bar__avatar" aria-hidden="true">
                    {(strawLabel ?? '?')[0]?.toUpperCase()}
                  </span>
                  <div className="bank-context-bar__text">
                    <span className="bank-context-bar__kicker">Laranja ativo</span>
                    <strong className="bank-context-bar__name">{strawLabel ?? strawManAccountId}</strong>
                  </div>
                </div>
                <div className="bank-context-bar__actions">
                  <button type="button" className="bank-context-bar__change" onClick={() => setStrawPickerOpen(true)}>
                    Trocar
                  </button>
                  <IconButton icon="x" label="Limpar laranja" onClick={clearStraw} />
                </div>
              </div>

              <div className="admin-op-section__head bank-accounts-section__head">
                  <div className="admin-op-section__head-text">
                    <span className="admin-op-section__kicker">Contas</span>
                    <h2 className="admin-op-section-title">Cadastradas</h2>
                    <p className="admin-op-section-desc muted small">
                      {loading ? 'Carregando…' : `${totalItems} conta(s) neste laranja.`}
                    </p>
                  </div>
                  <button type="button" className="btn btn-primary btn-sm bank-section-create" onClick={() => setCreateOpen(true)}>
                    Nova conta
                  </button>
                </div>

                <div className="admin-op-section__body">
                  <div className="bank-list-toolbar">
                    <input
                      className="nexus-input bank-list-toolbar__search"
                      value={filter}
                      onChange={(e) => setFilter(e.target.value)}
                      placeholder="Buscar por banco, chave PIX ou apelido…"
                      aria-label="Filtrar contas"
                    />
                    <IconButton
                      icon="refresh"
                      label="Atualizar lista"
                      onClick={() => void load(currentPage, strawManAccountId)}
                      disabled={loading}
                    />
                  </div>

                  {loading ? (
                    <p className="muted bank-list-loading">Carregando contas…</p>
                  ) : rows.length === 0 ? (
                    <div className="bank-empty-block">
                      <EmptyState
                        title="Nenhuma conta cadastrada"
                        message="Cadastre a primeira conta bancária de destino para este laranja."
                      />
                      <button type="button" className="btn btn-primary" onClick={() => setCreateOpen(true)}>
                        Cadastrar primeira conta
                      </button>
                    </div>
                  ) : filteredRows.length === 0 ? (
                    <EmptyState
                      title="Nenhum resultado"
                      message="Ajuste o filtro ou cadastre uma nova conta."
                    />
                  ) : (
                    <ul className="bank-account-list">
                      {filteredRows.map((row) => (
                        <BankAccountCard
                          key={row.id}
                          row={row}
                          onLabelUpdated={handleLabelUpdated}
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
        searchAccounts={searchOpAdminStrawMenPicker}
        title="Conta laranja"
        onSelected={(row) => {
          setStrawManAccountId(row.id);
          setStrawLabel(row.username);
          setCurrentPage(1);
          setFilter('');
        }}
      />

      <BankAccountCreateModal
        open={createOpen}
        busy={busy}
        strawLabel={strawLabel}
        onClose={() => setCreateOpen(false)}
        onSubmit={(payload) => void handleCreate(payload)}
      />
    </div>
  );
}
