import { useCallback, useEffect, useState } from 'react';
import { searchOperatorOperations } from '../../api/operator/operations';
import type { OperationDetails } from '../../api/types';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { EmptyState } from '../../components/EmptyState';
import { PaginationBar } from '../../components/ListControls';
import { dedupeOperatorListItems } from '../../features/operations/fetchOperationById';
import { OperationListItem } from '../../features/operations/OperationListItem';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function OperatorOperationsPage() {
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchOperatorOperations({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    const rawItems = result.data?.items ?? [];
    setTotalItems(result.data?.total ?? 0);
    setItems(dedupeOperatorListItems(rawItems));
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, query);
  }, [currentPage, query, load]);

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  return (
    <OpsWorkspace
      lead="Operações vinculadas à sua conta via equipes. Abra uma operação para ver suas equipes e repasses."
      searchId="opSearch"
      searchLabel="Buscar nas minhas operações"
      searchPlaceholder="Nome, ID ou descrição…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={() => void handleSearch()}
      onRefresh={() => void load(currentPage, query)}
      totalItems={totalItems}
      totalLabel={`${totalItems} alocação(ões)`}
      footer={totalItems > 0 ? (
        <PaginationBar
          currentPage={currentPage}
          totalPages={totalPages}
          onPrev={() => setCurrentPage((p) => Math.max(1, p - 1))}
          onNext={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
        />
      ) : undefined}
    >
      {items.length === 0 ? (
        <EmptyState
          title="Nenhuma operação encontrada"
          message="Você ainda não está alocado em nenhuma operação ou o filtro não retornou resultados."
        />
      ) : (
        <div className="ops-list">
          {items.map((op) => (
            <OperationListItem key={op.id} operation={op} scope="operator" />
          ))}
        </div>
      )}
    </OpsWorkspace>
  );
}
