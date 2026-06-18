import { useCallback, useEffect, useState } from 'react';
import { searchTeamLeaderLedTeams } from '../../api/teamLeader/operations';
import type { OperationWithLedTeamsDetails } from '../../api/types';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { EmptyState } from '../../components/EmptyState';
import { PaginationBar } from '../../components/ListControls';
import { OperationListItem } from '../../features/operations/OperationListItem';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function TeamLeaderOperationsPage() {
  const { notifyError } = useNotifications();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationWithLedTeamsDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchTeamLeaderLedTeams({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    setTotalItems(result.data?.total ?? 0);
    setItems(result.data?.items ?? []);
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, query);
  }, [currentPage, query, load]);

  async function refresh() {
    await load(currentPage, query);
  }

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  return (
    <OpsWorkspace
      lead="Operações agrupadas com as equipes que você lidera. Gerencie operadores e regras de repasse."
      searchId="teamLeaderSearch"
      searchLabel="Buscar operações"
      searchPlaceholder="Nome, ID ou descrição…"
      searchValue={search}
      onSearchChange={setSearch}
      onSearch={() => void handleSearch()}
      onRefresh={() => void refresh()}
      totalItems={totalItems}
      totalLabel={`${totalItems} operação(ões)`}
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
          title="Nenhuma equipe liderada"
          message="Você ainda não lidera nenhuma equipe ou o filtro não retornou resultados."
        />
      ) : (
        <div className="ops-list">
          {items.map((op) => (
            <OperationListItem key={op.id} operation={op} scope="team-leader" />
          ))}
        </div>
      )}
    </OpsWorkspace>
  );
}
