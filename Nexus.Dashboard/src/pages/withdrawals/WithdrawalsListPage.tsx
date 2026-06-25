import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { searchAdministratorStrawMenPicker } from '../../api/accountPickerSources';
import { searchTransfers } from '../../api/transfers';
import type { TransferRow, TransferType } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { EmptyState } from '../../components/EmptyState';
import { StatusPill } from '../../components/finance/StatusPill';
import { PaginationBar } from '../../components/ListControls';
import { formatMoney, transferTypeLabel } from '../../utils/financeLabels';
import { formatTransferDestinationSummary, formatTransferOriginSummary } from '../../utils/transferDisplay';
import { formatUtc } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function WithdrawalsListPage() {
  const navigate = useNavigate();
  const { notifyError } = useNotifications();
  const [strawManId, setStrawManId] = useState('');
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [typeFilter, setTypeFilter] = useState<'' | TransferType>('');
  const [appliedStrawManId, setAppliedStrawManId] = useState('');
  const [appliedType, setAppliedType] = useState<'' | TransferType>('');
  const [rows, setRows] = useState<TransferRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, filterStrawManId: string, type: '' | TransferType) => {
    const result = await searchTransfers({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      strawManId: filterStrawManId.trim() || null,
      type: type || null,
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
    void load(currentPage, appliedStrawManId, appliedType);
  }, [currentPage, appliedStrawManId, appliedType, load]);

  function applyFilters() {
    setCurrentPage(1);
    setAppliedStrawManId(strawManId);
    setAppliedType(typeFilter);
  }

  function clearStrawFilter() {
    setStrawManId('');
    setStrawLabel(null);
    setCurrentPage(1);
    setAppliedStrawManId('');
  }

  return (
    <>
      <OpsWorkspace
        kicker="Financeiro"
        title="Transferências"
        lead="Saques liquidam pagamentos; movimentações e repasses seguem a partir do detalhe de cada cadeia."
        searchId="transferStrawFilter"
        searchLabel="Laranja"
        searchPlaceholder="Todos os laranjas"
        searchValue={strawLabel ?? ''}
        onSearchChange={() => {}}
        onSearch={() => setStrawPickerOpen(true)}
        onRefresh={() => void load(currentPage, appliedStrawManId, appliedType)}
        totalItems={totalItems}
        totalLabel={`${totalItems} transferência(s)`}
        onCreate={() => navigate('/dashboard/transfers/new')}
        createLabel="Novo saque"
        footer={totalItems > 0 ? (
          <PaginationBar
            currentPage={currentPage}
            totalPages={totalPages}
            onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
            onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
          />
        ) : undefined}
      >
        <div className="ops-workspace__filters finance-hub-filters">
          <div className="field">
            <label htmlFor="transferType">Tipo</label>
            <select
              id="transferType"
              className="nexus-input"
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value as '' | TransferType)}
            >
              <option value="">Todos</option>
              <option value="Withdrawal">Saque</option>
              <option value="Movement">Movimentação</option>
              <option value="Payout">Repasse</option>
            </select>
          </div>
          <button type="button" className="btn btn-secondary btn-sm" onClick={applyFilters}>
            Aplicar filtros
          </button>
          {appliedStrawManId ? (
            <button type="button" className="btn btn-ghost btn-sm" onClick={clearStrawFilter}>
              Limpar laranja
            </button>
          ) : null}
        </div>

        <nav className="finance-hub" aria-label="Atalhos financeiros">
          <Link className="finance-hub__card" to="/dashboard/transfers/bank-accounts">
            <span className="finance-hub__card-kicker">PIX</span>
            <strong className="finance-hub__card-title">Contas bancárias</strong>
            <span className="finance-hub__card-hint muted small">Destinos em reais</span>
          </Link>
          <Link className="finance-hub__card" to="/dashboard/transfers/crypto-wallets">
            <span className="finance-hub__card-kicker">On-chain</span>
            <strong className="finance-hub__card-title">Carteiras crypto</strong>
            <span className="finance-hub__card-hint muted small">Endereços por rede</span>
          </Link>
          <Link className="finance-hub__card finance-hub__card--accent" to="/dashboard/transfers/new">
            <span className="finance-hub__card-kicker">Início</span>
            <strong className="finance-hub__card-title">Novo saque</strong>
            <span className="finance-hub__card-hint muted small">Liquidar pagamentos</span>
          </Link>
        </nav>

        {rows.length === 0 ? (
          <EmptyState
            title="Nenhuma transferência encontrada"
            message="Registre um saque ou ajuste os filtros."
          />
        ) : (
          <div className="table-wrap table-top-gap">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th>Tipo</th>
                  <th>Origem</th>
                  <th>Destino</th>
                  <th>Valor</th>
                  <th>Pagamentos</th>
                  <th>Criado em</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td data-label="Tipo">
                      <StatusPill
                        label={transferTypeLabel(row.type)}
                        tone={row.type === 'Withdrawal' ? 'info' : row.type === 'Movement' ? 'warn' : 'success'}
                      />
                    </td>
                    <td data-label="Origem" className="muted small">{formatTransferOriginSummary(row)}</td>
                    <td data-label="Destino" className="muted small">{formatTransferDestinationSummary(row)}</td>
                    <td data-label="Valor"><strong>{formatMoney(row.sourceAmount)}</strong></td>
                    <td data-label="Pagamentos">{row.paymentIds.length || '—'}</td>
                    <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                    <td data-label="Detalhe">
                      <Link className="btn btn-primary btn-sm" to={`/dashboard/transfers/${row.id}`}>
                        Abrir
                      </Link>
                    </td>
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
        title="Filtrar por laranja"
        subtitle="Mostra apenas transferências deste titular."
        onSelected={(row) => {
          setStrawManId(row.id);
          setStrawLabel(`@${row.username}`);
          setStrawPickerOpen(false);
          setCurrentPage(1);
          setAppliedStrawManId(row.id);
          setAppliedType(typeFilter);
        }}
      />
    </>
  );
}
