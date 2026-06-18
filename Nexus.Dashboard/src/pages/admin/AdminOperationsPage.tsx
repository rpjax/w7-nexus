import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createAdministratorOperation, searchAdministratorOperations } from '../../api/administrator/operations';
import type { OperationDetails } from '../../api/types';
import { CreateOperationModal } from '../../components/admin/CreateOperationModal';
import { OpsWorkspace } from '../../components/admin/OpsWorkspace';
import { EmptyState } from '../../components/EmptyState';
import { PaginationBar } from '../../components/ListControls';
import { OperationListItem } from '../../features/operations/OperationListItem';
import { detailPath } from '../../features/operations/operationPaths';
import { useOperationScopeActions } from '../../features/operations/useOperationScopeActions';
import { useNotifications } from '../../notifications/NotificationContext';

const PAGE_SIZE = 20;

export function AdminOperationsPage() {
  const navigate = useNavigate();
  const { notifyError, notifySuccess } = useNotifications();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<OperationDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const load = useCallback(async (page: number, keyword: string) => {
    const result = await searchAdministratorOperations({
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

  const { requestDeleteOperation, modals } = useOperationScopeActions({
    scope: 'global-admin',
    mode: 'list',
    onMutated: refresh,
    onOperationDeleted: refresh,
  });

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  async function handleCreate(name: string, description: string | null) {
    setCreateBusy(true);
    try {
      const result = await createAdministratorOperation(name, description);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess('Operação registrada no sistema.');
      setCreateModalOpen(false);
      if (result.data?.id) {
        navigate(detailPath('global-admin', result.data.id));
        return;
      }
      setCurrentPage(1);
      setQuery('');
      setSearch('');
      await load(1, '');
    } finally {
      setCreateBusy(false);
    }
  }

  return (
    <>
      <OpsWorkspace
        className="admin-surface"
        kicker="Administração"
        kickerVariant="admin"
        title="Todas as operações"
        lead="Gestão completa do repositório: administradores, equipes, operadores, repasses e configuração de gateway."
        searchId="adminOpSearch"
        searchLabel="Buscar no sistema"
        searchPlaceholder="Nome, ID ou descrição…"
        searchValue={search}
        onSearchChange={setSearch}
        onSearch={() => void handleSearch()}
        onRefresh={() => void refresh()}
        totalItems={totalItems}
        totalLabel={`${totalItems} registro(s) no repositório`}
        onCreate={() => setCreateModalOpen(true)}
        createLabel="Nova operação"
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
            message="Registre uma operação ou ajuste o filtro de busca."
          />
        ) : (
          <div className="ops-list">
            {items.map((op) => (
              <OperationListItem
                key={op.id}
                operation={op}
                scope="global-admin"
                onDelete={requestDeleteOperation}
              />
            ))}
          </div>
        )}
      </OpsWorkspace>

      <CreateOperationModal
        open={createModalOpen}
        busy={createBusy}
        onClose={() => setCreateModalOpen(false)}
        onSubmit={(name, description) => void handleCreate(name, description)}
      />

      {modals}
    </>
  );
}
