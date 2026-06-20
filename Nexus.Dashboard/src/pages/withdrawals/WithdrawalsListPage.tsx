import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { searchWithdrawals } from '../../api/withdrawals';
import type { WithdrawalRow, WithdrawalType } from '../../api/types';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { EmptyState } from '../../components/EmptyState';
import { StatusPill } from '../../components/finance/StatusPill';
import { PaginationBar } from '../../components/ListControls';
import { formatMoney, withdrawalTypeLabel } from '../../utils/financeLabels';
import { formatUtc, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function WithdrawalsListPage() {
  const navigate = useNavigate();
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState<'' | WithdrawalType>('');
  const [appliedType, setAppliedType] = useState<'' | WithdrawalType>('');
  const [rows, setRows] = useState<WithdrawalRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string, type: '' | WithdrawalType) => {
    const result = await searchWithdrawals({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      operationId: keyword.trim() || null,
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
      title="Saques"
      lead="Liquidação de pagamentos via PIX ou crypto, com custos e comprovantes registrados."
      searchId="withdrawSearch"
      searchLabel="Filtrar por operação (ID)"
      searchPlaceholder="ID da operação…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={handleSearch}
      onRefresh={() => void refresh()}
      totalItems={totalItems}
      totalLabel={`${totalItems} saque(s)`}
      onCreate={() => navigate('/dashboard/withdrawals/new')}
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
          <label htmlFor="withdrawType">Tipo</label>
          <select
            id="withdrawType"
            className="nexus-input"
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value as '' | WithdrawalType)}
          >
            <option value="">Todos</option>
            <option value="Pix">PIX</option>
            <option value="Crypto">Crypto</option>
          </select>
        </div>
        <Link className="btn btn-ghost" to="/dashboard/withdrawals/bank-accounts">Contas bancárias</Link>
        <Link className="btn btn-ghost" to="/dashboard/withdrawals/crypto-wallets">Carteiras crypto</Link>
      </div>

      {rows.length === 0 ? (
        <EmptyState
          title="Nenhum saque encontrado"
          message="Registre um saque ou ajuste os filtros."
        />
      ) : (
        <div className="table-wrap table-top-gap">
          <table className="responsive-data ops-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Tipo</th>
                <th>Operação</th>
                <th>Laranja</th>
                <th>Pagamentos</th>
                <th>Total</th>
                <th>Custo</th>
                <th>Líquido</th>
                <th>Criado em</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}>
                  <td data-label="ID"><span className="mono">{shortId(row.id)}</span></td>
                  <td data-label="Tipo">
                    <StatusPill label={withdrawalTypeLabel(row.type)} tone={row.type === 'Pix' ? 'info' : 'warn'} />
                  </td>
                  <td data-label="Operação"><span className="mono">{shortId(row.operationId)}</span></td>
                  <td data-label="Laranja"><span className="mono">{shortId(row.strawManAccountId)}</span></td>
                  <td data-label="Pagamentos">{row.paymentIds.length}</td>
                  <td data-label="Total">{formatMoney(row.paymentsTotalAmount)}</td>
                  <td data-label="Custo">{formatMoney(row.costAmount)}</td>
                  <td data-label="Líquido">{formatMoney(row.netAmount)}</td>
                  <td data-label="Criado em" className="muted small">{formatUtc(row.createdAt)}</td>
                  <td data-label="Detalhe">
                    <Link className="btn btn-ghost btn-sm" to={`/dashboard/withdrawals/${row.id}`}>Ver</Link>
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
