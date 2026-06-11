import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  addGatewayCredential,
  addOperator,
  addStrawMan,
  createOperation,
  deleteOperation,
  disableManualGatewayCredentials,
  enableManualGatewayCredentials,
  removeGatewayCredential,
  removeOperator,
  removeStrawMan,
  searchOperations,
} from '../api/operations';
import type { GatewayCredentialPickerRow, OperationRow } from '../api/types';
import { AccountPickerModal } from '../components/AccountPickerModal';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { Feedback } from '../components/Feedback';
import { GatewayCredentialPickerModal } from '../components/GatewayCredentialPickerModal';
import { formatDateTime, shortId } from '../utils/format';

const PAGE_SIZE = 20;

export function OperationsPage() {
  const [createName, setCreateName] = useState('');
  const [createDescription, setCreateDescription] = useState('');
  const [createInitialOperatorIds, setCreateInitialOperatorIds] = useState<string[]>([]);
  const [accountLabels, setAccountLabels] = useState<Record<string, string>>({});
  const [gatewayCredentialLabels, setGatewayCredentialLabels] = useState<Record<string, string>>({});

  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [feedback, setFeedback] = useState('');
  const [feedbackIsError, setFeedbackIsError] = useState(false);
  const [items, setItems] = useState<OperationRow[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteOperationId, setDeleteOperationId] = useState('');

  const [manageModalOpen, setManageModalOpen] = useState(false);
  const [manageOperation, setManageOperation] = useState<OperationRow | null>(null);

  const [initialPickerOpen, setInitialPickerOpen] = useState(false);
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);
  const [gatewayCredentialPickerOpen, setGatewayCredentialPickerOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);
  const [gatewayCredBusy, setGatewayCredBusy] = useState(false);

  const [operatorPickPreview, setOperatorPickPreview] = useState('');
  const [strawPickPreview, setStrawPickPreview] = useState('');

  const initialDisabledSet = useMemo(() => new Set(createInitialOperatorIds), [createInitialOperatorIds]);
  const operatorPickerDisabledSet = useMemo(
    () => (manageOperation ? new Set(manageOperation.operatorIds) : undefined),
    [manageOperation],
  );
  const strawPickerDisabledSet = useMemo(
    () => (manageOperation ? new Set(manageOperation.strawManIds) : undefined),
    [manageOperation],
  );
  const gatewayCredentialPickerDisabledSet = useMemo(
    () => (manageOperation ? new Set(manageOperation.gatewayCredentialsIds) : undefined),
    [manageOperation],
  );

  const labelFor = useCallback(
    (id: string) => (accountLabels[id] ? `${accountLabels[id]} · ${id}` : id),
    [accountLabels],
  );

  const gatewayCredentialDisplay = useCallback(
    (credId: string) => (gatewayCredentialLabels[credId] ? `${gatewayCredentialLabels[credId]} · ${credId}` : credId),
    [gatewayCredentialLabels],
  );

  function syncLabelsFromOperation(op: OperationRow) {
    setAccountLabels((prev) => {
      const next = { ...prev };
      for (const id of [...op.operatorIds, ...op.strawManIds]) {
        next[id] = next[id] ?? shortId(id, 12);
      }
      return next;
    });
  }

  function pruneGatewayCredentialLabels(op: OperationRow) {
    const keep = new Set(op.gatewayCredentialsIds);
    setGatewayCredentialLabels((prev) => {
      const next = { ...prev };
      for (const k of Object.keys(next)) {
        if (!keep.has(k)) delete next[k];
      }
      return next;
    });
  }

  const load = useCallback(async (page: number, keyword: string, managedId?: string | null) => {
    const result = await searchOperations({
      limit: PAGE_SIZE,
      offset: (page - 1) * PAGE_SIZE,
      keyword: keyword.trim() || null,
    });
    if (!result.ok) {
      setFeedbackIsError(true);
      setFeedback(result.error);
      return;
    }
    setTotalItems(result.data?.total ?? 0);
    const loaded = result.data?.items ?? [];
    setItems(loaded);

    if (managedId) {
      const updated = loaded.find((o) => o.id === managedId);
      if (updated) {
        setManageOperation(updated);
        syncLabelsFromOperation(updated);
        pruneGatewayCredentialLabels(updated);
      }
    }
  }, []);

  useEffect(() => {
    void load(currentPage, query, manageModalOpen ? manageOperation?.id : null);
  }, [currentPage, query, load]);

  async function refresh() {
    await load(currentPage, query, manageModalOpen ? manageOperation?.id : null);
  }

  async function handleSearch() {
    setCurrentPage(1);
    setQuery(search);
  }

  async function handleCreate() {
    setCreateBusy(true);
    setFeedback('');
    setFeedbackIsError(false);
    try {
      const result = await createOperation(createName, createDescription, createInitialOperatorIds);
      if (!result.ok) {
        setFeedbackIsError(true);
        setFeedback(result.error);
        return;
      }
      setFeedback('Operação criada com sucesso.');
      setCreateName('');
      setCreateDescription('');
      setCreateInitialOperatorIds([]);
      setCurrentPage(1);
      await load(1, query);
    } finally {
      setCreateBusy(false);
    }
  }

  function removeInitialOperator(id: string) {
    setCreateInitialOperatorIds((prev) => prev.filter((x) => x !== id));
    setAccountLabels((prev) => {
      const next = { ...prev };
      delete next[id];
      return next;
    });
  }

  function openManageModal(operationId: string) {
    const op = items.find((o) => o.id === operationId);
    if (!op) return;
    setManageOperation(op);
    setManageModalOpen(true);
    setOperatorPickPreview('');
    setStrawPickPreview('');
    syncLabelsFromOperation(op);
    pruneGatewayCredentialLabels(op);
  }

  function closeManageModal() {
    setManageModalOpen(false);
    setManageOperation(null);
    setOperatorPickPreview('');
    setStrawPickPreview('');
    setGatewayCredentialPickerOpen(false);
  }

  async function confirmDelete() {
    setDeleteDialogOpen(false);
    if (!deleteOperationId) return;
    const result = await deleteOperation(deleteOperationId);
    if (!result.ok) {
      setFeedbackIsError(true);
      setFeedback(result.error);
      return;
    }
    setFeedback('Operação excluída com sucesso.');
    if (manageOperation?.id === deleteOperationId) closeManageModal();
    setDeleteOperationId('');
    setCurrentPage(1);
    await load(1, query);
  }

  async function runManageAction(
    action: () => Promise<{ ok: false; error: string } | { ok: true; data?: unknown }>,
    successMessage: string,
  ) {
    if (!manageOperation) return;
    const result = await action();
    setFeedbackIsError(!result.ok);
    setFeedback(result.ok ? successMessage : result.error);
    await load(currentPage, search, manageOperation.id);
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <h1>Gestão de operações</h1>
          <p className="muted page-lead">Operadores, laranjas e, opcionalmente, <strong>lista manual de credenciais</strong> de gateway (Frendz, SigiloPay ou Wintech) para PIX — configurável ao gerenciar cada operação.</p>
        </div>
      </section>

      <section className="card ops-card ops-create">
        <div className="card-title-row">
          <h2>Criar nova operação</h2>
          <span className="post-badge" title="Endpoint">POST /api/operations</span>
        </div>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="opName">Nome da operação</label>
            <input id="opName" className="nexus-input" value={createName} onChange={(e) => setCreateName(e.target.value)} placeholder="Ex.: Gateway Phoenix" />
          </div>
          <div className="field span-2">
            <label htmlFor="opDesc">Descrição</label>
            <textarea id="opDesc" className="nexus-input" rows={2} value={createDescription} onChange={(e) => setCreateDescription(e.target.value)} placeholder="Contexto e escopo da operação" />
          </div>
          <div className="field span-2">
            <label>Operadores iniciais <span className="muted small">opcional</span></label>
            <div className="chips-wrap">
              {createInitialOperatorIds.map((id) => (
                <span key={id} className="chip">
                  {labelFor(id)}
                  <button type="button" className="chip-remove" onClick={() => removeInitialOperator(id)} aria-label="Remover">×</button>
                </span>
              ))}
              <button type="button" className="btn btn-ghost btn-small" onClick={() => setInitialPickerOpen(true)}>+ Adicionar conta</button>
            </div>
          </div>
        </div>
        <div className="card-actions">
          <button type="button" className="btn btn-primary" onClick={() => void handleCreate()} disabled={createBusy}>
            {createBusy ? 'Registrando…' : 'Registrar operação'}
          </button>
        </div>
      </section>

      <Feedback message={feedback} isError={feedbackIsError} />

      {manageModalOpen && manageOperation ? (
        <div className="dialog-backdrop dialog-backdrop--modal" onClick={closeManageModal}>
          <div className="dialog-card dialog-card--wide dialog-card--manage-op" onClick={(e) => e.stopPropagation()}>
            <div className="modal-stack manage-op-stack">
              <div className="modal-stack-header manage-op-header">
                <div>
                  <h2 className="manage-ids-title">Gerenciar operação</h2>
                  <p className="muted manage-ids-desc">
                    <strong>{manageOperation.name}</strong>
                    <span className="mono"> · {manageOperation.id}</span>
                  </p>
                </div>
                <button type="button" className="btn btn-ghost btn-small" onClick={closeManageModal}>Fechar</button>
              </div>

              <div className="manage-op-body">
                <div className="manage-op-col manage-op-col--people">
                  <div className="dual-grid manage-ids-grid">
                    <div className="mini-card manage-link">
                      <div className="manage-link-label">
                        <span>Vincular operador</span>
                        <span className="post-badge small">POST</span>
                      </div>
                      <div className="account-select-row">
                        <button type="button" className="account-select-trigger" onClick={() => setOperatorPickerOpen(true)}>
                          {operatorPickPreview || 'Selecionar conta'}
                        </button>
                        <button type="button" className="btn-icon btn-icon-green" onClick={() => setOperatorPickerOpen(true)} title="Selecionar operador">＋</button>
                      </div>
                    </div>
                    <div className="mini-card manage-link">
                      <div className="manage-link-label">
                        <span>Vincular laranja</span>
                        <span className="post-badge small post-badge-warm">POST</span>
                      </div>
                      <div className="account-select-row">
                        <button type="button" className="account-select-trigger" onClick={() => setStrawPickerOpen(true)}>
                          {strawPickPreview || 'Selecionar conta'}
                        </button>
                        <button type="button" className="btn-icon btn-icon-warm" onClick={() => setStrawPickerOpen(true)} title="Selecionar laranja">＋</button>
                      </div>
                    </div>
                  </div>

                  <div className="dual-grid manage-op-lists-grid">
                    <section className="mini-card manage-op-list-card">
                      <h3>Operadores</h3>
                      {manageOperation.operatorIds.length === 0 ? (
                        <p className="muted">Nenhum operador vinculado.</p>
                      ) : (
                        <ul className="chip-list">
                          {manageOperation.operatorIds.map((id) => (
                            <li key={id}>
                              <span>{labelFor(id)}</span>
                              <button type="button" className="btn btn-danger btn-small" onClick={() => void runManageAction(() => removeOperator(manageOperation.id, id), 'Operador removido com sucesso.')}>Remover</button>
                            </li>
                          ))}
                        </ul>
                      )}
                    </section>
                    <section className="mini-card manage-op-list-card">
                      <h3>Laranjas</h3>
                      {manageOperation.strawManIds.length === 0 ? (
                        <p className="muted">Nenhum laranja vinculado.</p>
                      ) : (
                        <ul className="chip-list">
                          {manageOperation.strawManIds.map((id) => (
                            <li key={id}>
                              <span>{labelFor(id)}</span>
                              <button type="button" className="btn btn-danger btn-small" onClick={() => void runManageAction(() => removeStrawMan(manageOperation.id, id), 'Laranja removido com sucesso.')}>Remover</button>
                            </li>
                          ))}
                        </ul>
                      )}
                    </section>
                  </div>
                </div>

                <div className="manage-op-col manage-op-col--billing">
                  <section className="mini-card manage-gateway-card">
                    <h3>Credenciais de cobrança (gateways)</h3>
                    <p className="muted small manage-gateway-lead">
                      Com <strong>seleção manual</strong>, só os IDs Frendz, SigiloPay ou Wintech listados entram na rotação de PIX.
                      Caso contrário, usam-se credenciais elegíveis pelos <strong>laranjas</strong> da operação.
                    </p>
                    {!manageOperation.manuallySetGatewayCredentials ? (
                      <button
                        type="button"
                        className="btn btn-primary btn-small"
                        disabled={gatewayCredBusy}
                        onClick={() => {
                          setGatewayCredBusy(true);
                          void enableManualGatewayCredentials(manageOperation.id)
                            .then((r) => {
                              setFeedbackIsError(!r.ok);
                              setFeedback(r.ok ? 'Seleção manual de credenciais ativada.' : r.error);
                              return load(currentPage, query, manageOperation.id);
                            })
                            .finally(() => setGatewayCredBusy(false));
                        }}
                      >
                        Ativar seleção manual de credenciais
                      </button>
                    ) : (
                      <>
                        <div className="manage-gateway-actions">
                          <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            disabled={gatewayCredBusy}
                            onClick={() => {
                              setGatewayCredBusy(true);
                              void disableManualGatewayCredentials(manageOperation.id)
                                .then((r) => {
                                  setFeedbackIsError(!r.ok);
                                  setFeedback(r.ok ? 'Seleção por laranjas restaurada; lista de credenciais limpa.' : r.error);
                                  return load(currentPage, query, manageOperation.id);
                                })
                                .finally(() => setGatewayCredBusy(false));
                            }}
                          >
                            Voltar à seleção por laranjas (limpa a lista)
                          </button>
                        </div>
                        <div className="field manage-gateway-add-row">
                          <label>Adicionar credencial</label>
                          <div className="account-select-row">
                            <button type="button" className="account-select-trigger" disabled={gatewayCredBusy} onClick={() => setGatewayCredentialPickerOpen(true)}>
                              Selecionar credencial
                            </button>
                            <button type="button" className="btn-icon btn-icon-green" disabled={gatewayCredBusy} onClick={() => setGatewayCredentialPickerOpen(true)} title="Selecionar credencial">＋</button>
                          </div>
                        </div>
                        {manageOperation.gatewayCredentialsIds.length === 0 ? (
                          <p className="muted manage-gateway-empty">Nenhuma credencial na lista — o orquestrador não terá credenciais para esta operação até você incluir ao menos uma.</p>
                        ) : (
                          <ul className="chip-list manage-gateway-chip-list">
                            {manageOperation.gatewayCredentialsIds.map((credId) => (
                              <li key={credId}>
                                <span>{gatewayCredentialDisplay(credId)}</span>
                                <button type="button" className="btn btn-danger btn-small" onClick={() => void runManageAction(() => removeGatewayCredential(manageOperation.id, credId), 'Credencial removida.')}>Remover</button>
                              </li>
                            ))}
                          </ul>
                        )}
                      </>
                    )}
                  </section>
                </div>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      <section className="card ops-card">
        <div className="toolbar">
          <div className="field grow">
            <label htmlFor="opSearch">Buscar operações</label>
            <input id="opSearch" className="nexus-input" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Nome, ID ou descrição…" />
          </div>
          <button type="button" className="btn btn-ghost" onClick={() => void handleSearch()}>Buscar</button>
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Atualizar</button>
        </div>

        {items.length === 0 ? (
          <EmptyState title="Nenhuma operação encontrada" message="Crie uma operação acima ou ajuste o filtro." />
        ) : (
          <>
            <div className="table-wrap">
              <table className="responsive-data ops-table ops-table-desktop">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>Descrição</th>
                    <th>Operadores</th>
                    <th>Laranjas</th>
                    <th>Cobrança</th>
                    <th>Atualizado</th>
                    <th className="th-actions" scope="col">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((op) => (
                    <tr key={op.id} className={manageModalOpen && manageOperation?.id === op.id ? 'is-selected' : undefined}>
                      <td data-label="Nome">
                        <div className="cell-title">{op.name}</div>
                        <div className="cell-sub mono">{op.id}</div>
                      </td>
                      <td data-label="Descrição">{op.description?.trim() ? op.description : '—'}</td>
                      <td data-label="Operadores"><span className="count-pill">{op.operatorIds.length}</span></td>
                      <td data-label="Laranjas"><span className="count-pill count-pill-warm">{op.strawManIds.length}</span></td>
                      <td data-label="Cobrança">
                        {op.manuallySetGatewayCredentials ? (
                          <span
                            className={op.gatewayCredentialsIds.length === 0 ? 'count-pill count-pill-warn' : 'count-pill'}
                            title="Lista fixa de IDs de credencial Frendz, SigiloPay ou Wintech"
                          >
                            {op.gatewayCredentialsIds.length === 0 ? 'Manual · 0 cred.' : `Manual · ${op.gatewayCredentialsIds.length} cred.`}
                          </span>
                        ) : (
                          <span className="count-pill count-pill-warm" title="Credenciais elegíveis pelos laranjas desta operação">Por laranjas</span>
                        )}
                      </td>
                      <td data-label="Atualizado" className="muted">{formatDateTime(op.updatedAt)}</td>
                      <td className="cell-actions" data-label="Ações">
                        <div className="row-actions">
                          <button type="button" className="icon-action" title="Gerenciar" aria-label="Gerenciar operação" onClick={() => openManageModal(op.id)}>✎</button>
                          <button type="button" className="icon-action icon-action-danger" title="Excluir" aria-label="Excluir operação" onClick={() => { setDeleteOperationId(op.id); setDeleteDialogOpen(true); }}>🗑</button>
                        </div>
                      </td>
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

      <AccountPickerModal
        open={initialPickerOpen}
        onClose={() => setInitialPickerOpen(false)}
        title="Operadores iniciais"
        subtitle="Contas elegíveis para compor a operação ao criar."
        disabledAccountIds={initialDisabledSet}
        onSelected={(row) => {
          if (createInitialOperatorIds.includes(row.id)) return;
          setCreateInitialOperatorIds((prev) => [...prev, row.id]);
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
        }}
      />

      <AccountPickerModal
        open={operatorPickerOpen}
        onClose={() => setOperatorPickerOpen(false)}
        title="Vincular operador"
        subtitle="A mesma conta pode ser operador e laranja; aqui só contam IDs já como operador."
        disabledBadgeText="Já é operador"
        disabledAccountIds={operatorPickerDisabledSet}
        onSelected={(row) => {
          setOperatorPickPreview(`${row.username} (${row.id})`);
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
          if (!manageOperation) return;
          void runManageAction(
            () => addOperator(manageOperation.id, row.id),
            `Operador vinculado à operação ${manageOperation.id}.`,
          );
        }}
      />

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        title="Vincular laranja"
        subtitle="A mesma conta pode ser operador e laranja; aqui só contam IDs já como laranja."
        disabledBadgeText="Já é laranja"
        disabledAccountIds={strawPickerDisabledSet}
        onSelected={(row) => {
          setStrawPickPreview(`${row.username} (${row.id})`);
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
          if (!manageOperation) return;
          void runManageAction(
            () => addStrawMan(manageOperation.id, row.id),
            `Laranja vinculado à operação ${manageOperation.id}.`,
          );
        }}
      />

      <GatewayCredentialPickerModal
        open={gatewayCredentialPickerOpen}
        onClose={() => setGatewayCredentialPickerOpen(false)}
        title="Selecionar credencial"
        subtitle="Apenas credenciais habilitadas. Escolha Frendz, SigiloPay ou Wintech e busque por nome ou ID."
        disabledCredentialIds={gatewayCredentialPickerDisabledSet}
        onSelected={(row: GatewayCredentialPickerRow) => {
          setGatewayCredentialLabels((prev) => ({ ...prev, [row.id]: `${row.name} · ${row.gatewayLabel}` }));
          if (!manageOperation) return;
          setGatewayCredBusy(true);
          void addGatewayCredential(manageOperation.id, row.id)
            .then((r) => {
              setFeedbackIsError(!r.ok);
              setFeedback(r.ok ? 'Credencial adicionada à operação.' : r.error);
              if (!r.ok) {
                setGatewayCredentialLabels((prev) => {
                  const next = { ...prev };
                  delete next[row.id];
                  return next;
                });
              }
              return load(currentPage, query, manageOperation.id);
            })
            .finally(() => setGatewayCredBusy(false));
        }}
      />

      <ConfirmDialog
        open={deleteDialogOpen}
        title="Confirmar exclusão"
        message="Tem certeza que deseja excluir esta operação?"
        onCancel={() => { setDeleteDialogOpen(false); setDeleteOperationId(''); }}
        onConfirm={() => void confirmDelete()}
      />
    </>
  );
}
