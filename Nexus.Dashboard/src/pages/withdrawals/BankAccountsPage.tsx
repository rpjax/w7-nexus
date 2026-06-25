import { useCallback, useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { createBankAccount, searchBankAccounts } from '../../api/bankAccounts';
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
  ownerId?: string;
  strawManId?: string;
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
  const [ownerId, setOwnerId] = useState(() => locationState?.ownerId ?? locationState?.strawManId ?? '');
  const [strawLabel, setStrawLabel] = useState<string | null>(() => locationState?.strawLabel ?? null);
  const [rows, setRows] = useState<BankAccountRow[]>([]);
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
    return rows.filter((row) => bankAccountSearchText(row).includes(term));
  }, [rows, filter]);

  const load = useCallback(async (page: number, filterOwnerId: string) => {
    setLoading(true);
    try {
      const result = await searchBankAccounts({
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

  function handleLabelUpdated(updated: BankAccountRow) {
    setRows((prev) => prev.map((row) => (row.id === updated.id ? updated : row)));
    notifySuccess('Apelido atualizado.');
  }

  async function handleCreate(payload: BankAccountCreatePayload) {
    if (!ownerId.trim()) {
      notifyError('Selecione o laranja antes de cadastrar.');
      return;
    }
    setBusy(true);
    try {
      const result = await createBankAccount({
        ownerId: ownerId.trim(),
        ...payload,
      });
      if (!result.ok) {
        notifyError(result.error);
        return;
      }
      notifySuccess('Conta bancária cadastrada.');
      setCreateOpen(false);
      setCurrentPage(1);
      await load(1, ownerId);

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

  const heroCount = hasOwner
    ? `${totalItems} conta${totalItems === 1 ? '' : 's'}`
    : 'Escolha um laranja';

  const heroHint = hasOwner
    ? `Destinos PIX de ${strawLabel ?? 'laranja selecionado'}.`
    : 'Contas bancárias são cadastradas por laranja e usadas em transferências PIX.';

  return (
    <div className="ops-page bank-accounts-page">
      <PageHeading
        kicker="Financeiro"
        title="Contas bancárias"
        subtitle="Contas de destino PIX vinculadas a contas laranja."
        backLink={{ to: '/dashboard/transfers', label: 'Transferências' }}
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
          {!hasOwner ? (
            <section className="pix-section bank-section">
              <div className="bank-onboarding">
                <div className="bank-section__head-text">
                  <span className="bank-section__kicker">Começar</span>
                  <h2 id="bank-accounts-title" className="bank-section-title">Selecione o laranja</h2>
                  <p className="bank-section-desc muted small">
                    Cada laranja pode ter várias contas de destino. Escolha qual titular deseja gerenciar.
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
            <section className="pix-section bank-managed-section bank-section">
              <div className="bank-context-bar">
                <div className="bank-context-bar__main">
                  <span className="bank-context-bar__avatar" aria-hidden="true">
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

              <div className="bank-section__head bank-accounts-section__head">
                  <div className="bank-section__head-text">
                    <span className="bank-section__kicker">Contas</span>
                    <h2 className="bank-section-title">Cadastradas</h2>
                    <p className="bank-section-desc muted small">
                      {loading ? 'Carregando…' : `${totalItems} conta(s) neste laranja.`}
                    </p>
                  </div>
                  <button type="button" className="btn btn-primary btn-sm bank-section-create" onClick={() => setCreateOpen(true)}>
                    Nova conta
                  </button>
                </div>

                <div className="bank-section__body">
                  <div className="bank-list-toolbar">
                    <input
                      className="nexus-input bank-list-toolbar__search"
                      value={filter}
                      onChange={(e) => setFilter(e.target.value)}
                      placeholder="Buscar por banco, agência ou apelido…"
                      aria-label="Filtrar contas"
                    />
                    <IconButton
                      icon="refresh"
                      label="Atualizar lista"
                      onClick={() => void load(currentPage, ownerId)}
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
        searchAccounts={searchAdministratorStrawMenPicker}
        title="Conta laranja"
        onSelected={(row) => {
          setOwnerId(row.id);
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
