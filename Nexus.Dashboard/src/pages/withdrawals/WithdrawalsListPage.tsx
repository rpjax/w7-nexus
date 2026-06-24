import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { searchTransfers } from '../../api/transfers';
import type { TransferRow, TransferType } from '../../api/types';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { EmptyState } from '../../components/EmptyState';
import { StatusPill } from '../../components/finance/StatusPill';
import { PaginationBar } from '../../components/ListControls';
import { formatMoney, transferTypeLabel } from '../../utils/financeLabels';
import { formatUtc, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function WithdrawalsListPage() {
  const navigate = useNavigate();
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState<'' | TransferType>('');
  const [appliedType, setAppliedType] = useState<'' | TransferType>('');
  const [rows, setRows] = useState<TransferRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string, type: '' | TransferType) => {
    const result = await searchTransfers({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      strawManId: keyword.trim() || null,
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
    void load(currentPage, query, appliedType);
  }, [currentPage, query, appliedType, load]);

  async function refresh() {
    await load(currentPage, query, appliedType);
  }

  function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
    setAppliedType(typeFilter);
  }

  return (
    <OpsWorkspace
      kicker="Financeiro"
      title="Transferências"
      lead="Saques, movimentações e repasses entre contas bancárias, carteiras crypto e participantes."
      searchId="transferSearch"
      searchLabel="Filtrar por laranja (ID)"
      searchPlaceholder="ID do laranja…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={handleSearch}
      onRefresh={() => void refresh()}
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
      <div className="ops-workspace__filters">
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
        <Link className="btn btn-ghost" to="/dashboard/transfers/bank-accounts">Contas bancárias</Link>
        <Link className="btn btn-ghost" to="/dashboard/transfers/crypto-wallets">Carteiras crypto</Link>
        <Link className="btn btn-ghost" to="/dashboard/transfers/movement">Movimentação</Link>
        <Link className="btn btn-ghost" to="/dashboard/transfers/payout">Repasse</Link>
      </div>

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
                <th>ID</th>
                <th>Tipo</th>
                <th>Laranja</th>
                <th>Pagamentos</th>
                <th>Valor</th>
                <th>Criado em</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}>
                  <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                  <td data-label="Tipo">
                    <StatusPill
                      label={transferTypeLabel(row.type)}
                      tone={row.type === 'Withdrawal' ? 'info' : row.type === 'Movement' ? 'warn' : 'success'}
                    />
                  </td>
                  <td data-label="Laranja"><span className="mono">{shortId(row.strawManId)}</span></td>
                  <td data-label="Pagamentos">{row.paymentIds.length}</td>
                  <td data-label="Valor">{formatMoney(row.sourceAmount)}</td>
                  <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                  <td data-label="Detalhe">
                    <Link className="btn btn-ghost btn-sm" to={`/dashboard/transfers/${row.id}`}>Ver</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </OpsWorkspace>
  );
}
