import { useCallback, useEffect, useMemo, useState } from 'react';
import { searchOperatorOperations } from '../../api/operator/operations';
import type { OperationDetails } from '../../api/types';
import { EmptyState } from '../../components/EmptyState';
import { useNotifications } from '../../notifications/NotificationContext';
import { shortId } from '../../utils/format';

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
    setTotalItems(result.data?.total ?? 0);
    setItems(result.data?.items ?? []);
  }, [notifyError]);

  useEffect(() => {
    void load(currentPage, query);
  }, [currentPage, query, load]);

  const filteredHint = useMemo(() => {
    if (!query.trim()) return 'Operações em que você está alocado como operador.';
    return `Resultados para “${query.trim()}” nas suas operações.`;
  }, [query]);

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <p className="page-kicker">Painel do operador</p>
          <h1>Minhas operações</h1>
          <p className="muted page-lead">
            Operações vinculadas à sua conta via equipes. Use esta visão para escolher contexto em pagamentos e fluxos operacionais.
          </p>
        </div>
      </section>

      <section className="card ops-card">
        <div className="toolbar">
          <div className="field grow">
            <label htmlFor="opSearch">Buscar nas minhas operações</label>
            <input
              id="opSearch"
              className="nexus-input"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Nome, ID ou descrição…"
            />
          </div>
          <button type="button" className="btn btn-ghost" onClick={() => void handleSearch()}>Buscar</button>
          <button type="button" className="btn btn-ghost" onClick={() => void load(currentPage, query)}>Atualizar</button>
        </div>

        <div className="card-title-row">
          <div className="card-title-group">
            <h2 className="section-title">Operações alocadas</h2>
            <span className="post-badge">POST /api/operator/operations/search</span>
          </div>
          <span className="muted small">{filteredHint}</span>
        </div>

        {items.length === 0 ? (
          <EmptyState
            title="Nenhuma operação encontrada"
            message="Você ainda não está alocado em nenhuma operação ou o filtro não retornou resultados."
          />
        ) : (
          <>
            <div className="table-wrap table-top-gap">
              <table className="responsive-data ops-table">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>ID</th>
                    <th>Descrição</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((op) => (
                    <tr key={op.id}>
                      <td data-label="Nome"><strong>{op.name}</strong></td>
                      <td data-label="ID"><span className="mono">{shortId(op.id)}</span></td>
                      <td data-label="Descrição">{op.description?.trim() ? op.description : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalItems > 0 ? (
              <div className="pagination">
                <button type="button" className="btn btn-ghost" onClick={() => setCurrentPage((p) => Math.max(1, p - 1))} disabled={currentPage <= 1}>Anterior</button>
                <span className="muted">Página {currentPage} de {totalPages}</span>
                <button type="button" className="btn btn-ghost" onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))} disabled={currentPage >= totalPages}>Próxima</button>
              </div>
            ) : null}
          </>
        )}
      </section>
    </>
  );
}
