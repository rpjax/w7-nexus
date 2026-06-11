import { useEffect, useMemo, useState } from 'react';
import {
  addKeyPairCredential,
  addTokenCredential,
  deleteCredential,
  searchGatewayCredentials,
  setCredentialEnabled,
  updateKeyPairCredential,
  updateTokenCredential,
} from '../api/gateways';
import type { GatewayPrefix, KeyPairCredential, TokenCredential } from '../api/types';
import { AccountPickerModal } from '../components/AccountPickerModal';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { Feedback } from '../components/Feedback';
import { maskKey, maskToken, shortId } from '../utils/format';

type Variant = GatewayPrefix;

type GatewayConfig = {
  title: string;
  lead: string;
  mode: 'token' | 'keypair';
  addPlaceholder: string;
};

const CONFIG: Record<Variant, GatewayConfig> = {
  frendz: {
    title: 'Frendz',
    lead: 'Credenciais de API com token mascarado; opcionalmente vincule um laranja (conta) à credencial.',
    mode: 'token',
    addPlaceholder: 'Ex.: token dev',
  },
  sigilopay: {
    title: 'SigiloPay',
    lead: 'Credenciais SigiloPay (chave pública e secreta); opcionalmente vincule um laranja (conta).',
    mode: 'keypair',
    addPlaceholder: 'Ex.: produção loja A',
  },
  wintech: {
    title: 'Wintech',
    lead: 'API Wintech Pagamentos — chaves públicas/secretas; opcionalmente vincule um laranja (conta).',
    mode: 'keypair',
    addPlaceholder: 'Ex.: produção loja A',
  },
};

type CredentialRow = TokenCredential & KeyPairCredential;

type EditModel = {
  id: string;
  name: string;
  token: string;
  publicKey: string;
  secretKey: string;
  strawManId: string | null;
  enabled: boolean;
};

type GatewayCredentialsPageProps = {
  variant: Variant;
};

export function GatewayCredentialsPage({ variant }: GatewayCredentialsPageProps) {
  const config = CONFIG[variant];
  const [credentials, setCredentials] = useState<CredentialRow[]>([]);
  const [search, setSearch] = useState('');
  const [feedback, setFeedback] = useState('');
  const [feedbackIsError, setFeedbackIsError] = useState(false);
  const [addBusy, setAddBusy] = useState(false);
  const [editBusy, setEditBusy] = useState(false);
  const [enableToggleBusyId, setEnableToggleBusyId] = useState<string | null>(null);
  const [accountLabels, setAccountLabels] = useState<Record<string, string>>({});

  const [addName, setAddName] = useState('');
  const [addToken, setAddToken] = useState('');
  const [addPublicKey, setAddPublicKey] = useState('');
  const [addSecretKey, setAddSecretKey] = useState('');
  const [addEnabled, setAddEnabled] = useState(true);
  const [addStrawManId, setAddStrawManId] = useState<string | null>(null);
  const [addStrawLabel, setAddStrawLabel] = useState<string | null>(null);
  const [addStrawPickerOpen, setAddStrawPickerOpen] = useState(false);

  const [editing, setEditing] = useState<EditModel | null>(null);
  const [editStrawPickerOpen, setEditStrawPickerOpen] = useState(false);
  const [viewing, setViewing] = useState<CredentialRow | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteId, setDeleteId] = useState('');

  const filteredCredentials = useMemo(() => {
    if (!search.trim()) return credentials;
    const term = search.trim().toLowerCase();
    return credentials.filter((c) =>
      c.name.toLowerCase().includes(term)
      || c.id.toLowerCase().includes(term)
      || (c.token?.toLowerCase().includes(term) ?? false)
      || (c.publicKey?.toLowerCase().includes(term) ?? false)
      || (c.secretKey?.toLowerCase().includes(term) ?? false)
      || (c.strawManId?.toLowerCase().includes(term) ?? false),
    );
  }, [credentials, search]);

  async function refresh() {
    setFeedback('');
    setFeedbackIsError(false);
    const result = await searchGatewayCredentials(variant, { limit: 999, offset: 0, keyword: null });
    if (!result.ok) {
      setFeedbackIsError(true);
      setFeedback(result.error);
      return;
    }
    const items = (result.data?.items ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));
    setCredentials(items);
    setAccountLabels((prev) => {
      const next = { ...prev };
      for (const c of items) {
        if (c.strawManId) next[c.strawManId] = next[c.strawManId] ?? shortId(c.strawManId, 16);
      }
      return next;
    });
  }

  useEffect(() => {
    void refresh();
  }, [variant]);

  function formatStraw(strawManId?: string | null) {
    if (!strawManId) return '— genérico';
    return accountLabels[strawManId] ?? strawManId;
  }

  async function handleAdd() {
    setAddBusy(true);
    setFeedback('');
    setFeedbackIsError(false);
    try {
      if (config.mode === 'token') {
        if (!addToken.trim()) {
          setFeedbackIsError(true);
          setFeedback('O token é obrigatório.');
          return;
        }
        const result = await addTokenCredential(variant, {
          name: addName,
          token: addToken,
          strawManId: addStrawManId,
          enabled: addEnabled,
        });
        if (!result.ok) {
          setFeedbackIsError(true);
          setFeedback(result.error);
          return;
        }
      } else {
        if (!addPublicKey.trim() || !addSecretKey.trim()) {
          setFeedbackIsError(true);
          setFeedback('Chave pública e secreta são obrigatórias.');
          return;
        }
        const result = await addKeyPairCredential(variant, {
          name: addName,
          publicKey: addPublicKey,
          secretKey: addSecretKey,
          strawManId: addStrawManId,
          enabled: addEnabled,
        });
        if (!result.ok) {
          setFeedbackIsError(true);
          setFeedback(result.error);
          return;
        }
      }
      setFeedback('Credencial adicionada com sucesso.');
      setAddName('');
      setAddToken('');
      setAddPublicKey('');
      setAddSecretKey('');
      setAddStrawManId(null);
      setAddStrawLabel(null);
      setAddEnabled(true);
      await refresh();
    } finally {
      setAddBusy(false);
    }
  }

  async function handleEnabledToggle(cred: CredentialRow, enabled: boolean) {
    setEnableToggleBusyId(cred.id);
    try {
      const result = await setCredentialEnabled(variant, cred.id, enabled);
      if (!result.ok) {
        setFeedbackIsError(true);
        setFeedback(result.error);
        await refresh();
        return;
      }
      setCredentials((prev) => prev.map((c) => (c.id === cred.id ? { ...c, enabled } : c)));
      setFeedbackIsError(false);
      setFeedback(enabled ? 'Credencial habilitada para cobrança.' : 'Credencial desabilitada (não entra no orquestrador).');
    } finally {
      setEnableToggleBusyId(null);
    }
  }

  function beginEdit(cred: CredentialRow) {
    setEditing({
      id: cred.id,
      name: cred.name,
      token: cred.token ?? '',
      publicKey: cred.publicKey ?? '',
      secretKey: cred.secretKey ?? '',
      strawManId: cred.strawManId ?? null,
      enabled: cred.enabled,
    });
    setFeedback('');
    setFeedbackIsError(false);
  }

  async function handleUpdate() {
    if (!editing) return;
    setEditBusy(true);
    setFeedback('');
    setFeedbackIsError(false);
    try {
      if (config.mode === 'token') {
        if (!editing.token.trim()) {
          setFeedbackIsError(true);
          setFeedback('O token é obrigatório.');
          return;
        }
        const result = await updateTokenCredential(variant, {
          id: editing.id,
          name: editing.name,
          token: editing.token,
          strawManId: editing.strawManId,
          enabled: editing.enabled,
        });
        if (!result.ok) {
          setFeedbackIsError(true);
          setFeedback(result.error);
          return;
        }
      } else {
        if (!editing.publicKey.trim() || !editing.secretKey.trim()) {
          setFeedbackIsError(true);
          setFeedback('Chave pública e secreta são obrigatórias.');
          return;
        }
        const result = await updateKeyPairCredential(variant, {
          id: editing.id,
          name: editing.name,
          publicKey: editing.publicKey,
          secretKey: editing.secretKey,
          strawManId: editing.strawManId,
          enabled: editing.enabled,
        });
        if (!result.ok) {
          setFeedbackIsError(true);
          setFeedback(result.error);
          return;
        }
      }
      setFeedback('Credencial atualizada com sucesso.');
      setEditing(null);
      await refresh();
    } finally {
      setEditBusy(false);
    }
  }

  async function confirmDelete() {
    setDeleteDialogOpen(false);
    if (!deleteId) return;
    setFeedback('');
    setFeedbackIsError(false);
    const result = await deleteCredential(variant, deleteId);
    setFeedbackIsError(!result.ok);
    setFeedback(result.ok ? 'Credencial excluída com sucesso.' : result.error);
    setDeleteId('');
    setEditing(null);
    await refresh();
  }

  const editStrawLabel = editing?.strawManId
    ? (accountLabels[editing.strawManId] ? `${accountLabels[editing.strawManId]} (${editing.strawManId})` : editing.strawManId)
    : null;

  return (
    <>
      <section className="page-header">
        <h1>{config.title}</h1>
        <p className="muted page-lead">{config.lead}</p>
      </section>

      <Feedback message={feedback} isError={feedbackIsError} />

      <section className="card ops-card">
        <div className="card-title-row">
          <h2>Adicionar credencial</h2>
          <span className="post-badge">POST /api/{variant}/credentials</span>
        </div>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="credName">Nome</label>
            <input id="credName" className="nexus-input" value={addName} onChange={(e) => setAddName(e.target.value)} placeholder={config.addPlaceholder} />
          </div>
          {config.mode === 'token' ? (
            <div className="field">
              <label htmlFor="credToken">Token</label>
              <input id="credToken" className="nexus-input" type="password" value={addToken} onChange={(e) => setAddToken(e.target.value)} placeholder="Cole o token" />
            </div>
          ) : (
            <>
              <div className="field">
                <label htmlFor="credPublicKey">Chave pública</label>
                <input id="credPublicKey" className="nexus-input" type="password" value={addPublicKey} onChange={(e) => setAddPublicKey(e.target.value)} placeholder="x-public-key" />
              </div>
              <div className="field">
                <label htmlFor="credSecretKey">Chave secreta</label>
                <input id="credSecretKey" className="nexus-input" type="password" value={addSecretKey} onChange={(e) => setAddSecretKey(e.target.value)} placeholder="x-secret-key" />
              </div>
            </>
          )}
          <div className="field">
            <label className="checkbox-field">
              <input type="checkbox" checked={addEnabled} onChange={(e) => setAddEnabled(e.target.checked)} />
              <span>Habilitada para cobrança</span>
            </label>
            <p className="muted small" style={{ margin: '0.25rem 0 0 1.5rem' }}>Desmarque para manter a credencial cadastrada sem usar no orquestrador.</p>
          </div>
          <div className="field span-2">
            <label>Laranja (conta) <span className="muted small">opcional</span></label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setAddStrawPickerOpen(true)}>
                {addStrawLabel ?? 'Genérico (sem laranja)'}
              </button>
              <button type="button" className="btn-icon btn-icon-warm" onClick={() => setAddStrawPickerOpen(true)} title="Selecionar laranja">＋</button>
              {addStrawManId ? (
                <button type="button" className="btn btn-ghost btn-small" onClick={() => { setAddStrawManId(null); setAddStrawLabel(null); }}>Limpar</button>
              ) : null}
            </div>
          </div>
        </div>
        <div className="card-actions">
          <button type="button" className="btn btn-primary" onClick={() => void handleAdd()} disabled={addBusy}>Adicionar</button>
        </div>
      </section>

      <section className="card ops-card">
        <div className="toolbar toolbar-tight">
          <div className="field grow">
            <label htmlFor="credSearch">Buscar credenciais</label>
            <input id="credSearch" className="nexus-input" value={search} onChange={(e) => setSearch(e.target.value)} placeholder={config.mode === 'token' ? 'Nome, ID ou token…' : 'Nome, ID ou chave…'} />
          </div>
          <button type="button" className="btn btn-ghost" onClick={() => setSearch(search)}>Buscar</button>
          <button type="button" className="btn btn-ghost" onClick={() => void refresh()}>Atualizar</button>
        </div>

        <h2 className="section-title">Credenciais cadastradas</h2>

        {filteredCredentials.length === 0 ? (
          <EmptyState title="Nenhuma credencial encontrada" message="Cadastre uma credencial acima ou ajuste a busca." />
        ) : (
          <div className="table-wrap table-top-gap">
            <table className="responsive-data ops-table">
              <thead>
                <tr>
                  <th>Nome</th>
                  {config.mode === 'token' ? (
                    <th>Token</th>
                  ) : (
                    <>
                      <th>Chave pública</th>
                      <th>Chave secreta</th>
                    </>
                  )}
                  <th>Laranja</th>
                  <th>Ativa</th>
                  <th className="th-actions" scope="col">Ações</th>
                </tr>
              </thead>
              <tbody>
                {filteredCredentials.map((cred) => (
                  <tr key={cred.id}>
                    <td data-label="Nome">{cred.name}</td>
                    {config.mode === 'token' ? (
                      <td data-label="Token"><span className="mono token-mask" title="Mascarado">{maskToken(cred.token ?? '')}</span></td>
                    ) : (
                      <>
                        <td data-label="Chave pública"><span className="mono token-mask" title="Mascarado">{maskKey(cred.publicKey ?? '')}</span></td>
                        <td data-label="Chave secreta"><span className="mono token-mask" title="Mascarado">{maskKey(cred.secretKey ?? '')}</span></td>
                      </>
                    )}
                    <td data-label="Laranja" className="muted">{formatStraw(cred.strawManId)}</td>
                    <td data-label="Ativa">
                      <label className="toggle-inline">
                        <input
                          type="checkbox"
                          checked={cred.enabled}
                          disabled={enableToggleBusyId === cred.id}
                          onChange={(e) => void handleEnabledToggle(cred, e.target.checked)}
                        />
                        <span className="muted small">{cred.enabled ? 'Sim' : 'Não'}</span>
                      </label>
                    </td>
                    <td className="cell-actions" data-label="Ações">
                      <div className="row-actions">
                        <button type="button" className="icon-action" title="Ver credencial" aria-label="Ver credencial" onClick={() => setViewing(cred)}>👁</button>
                        <button type="button" className="icon-action" title="Editar" aria-label="Editar credencial" onClick={() => beginEdit(cred)}>✎</button>
                        <button type="button" className="icon-action icon-action-danger" title="Excluir" aria-label="Excluir credencial" onClick={() => { setDeleteId(cred.id); setDeleteDialogOpen(true); }}>🗑</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {editing ? (
        <div className="dialog-backdrop dialog-backdrop--modal" onClick={() => setEditing(null)}>
          <div className="dialog-card dialog-card--wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal-stack">
              <div className="modal-stack-header">
                <div>
                  <h2 className="manage-ids-title">Editar credencial</h2>
                  <p className="muted small">PUT /api/{variant}/credentials</p>
                </div>
                <button type="button" className="btn btn-ghost btn-small" onClick={() => setEditing(null)}>Fechar</button>
              </div>
              <div className="form-grid">
                <div className="field">
                  <label htmlFor="editCredName">Nome</label>
                  <input id="editCredName" className="nexus-input" value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} />
                </div>
                {config.mode === 'token' ? (
                  <div className="field">
                    <label htmlFor="editCredToken">Token</label>
                    <input id="editCredToken" className="nexus-input" type="password" value={editing.token} onChange={(e) => setEditing({ ...editing, token: e.target.value })} />
                  </div>
                ) : (
                  <>
                    <div className="field">
                      <label htmlFor="editPublicKey">Chave pública</label>
                      <input id="editPublicKey" className="nexus-input" type="password" value={editing.publicKey} onChange={(e) => setEditing({ ...editing, publicKey: e.target.value })} />
                    </div>
                    <div className="field">
                      <label htmlFor="editSecretKey">Chave secreta</label>
                      <input id="editSecretKey" className="nexus-input" type="password" value={editing.secretKey} onChange={(e) => setEditing({ ...editing, secretKey: e.target.value })} />
                    </div>
                  </>
                )}
                <div className="field span-2">
                  <label>Laranja (conta)</label>
                  <div className="account-select-row">
                    <button type="button" className="account-select-trigger" onClick={() => setEditStrawPickerOpen(true)}>
                      {editStrawLabel ?? 'Genérico (sem laranja)'}
                    </button>
                    <button type="button" className="btn-icon btn-icon-warm" onClick={() => setEditStrawPickerOpen(true)} title="Selecionar laranja">＋</button>
                    {editing.strawManId ? (
                      <button type="button" className="btn btn-ghost btn-small" onClick={() => setEditing({ ...editing, strawManId: null })}>Limpar</button>
                    ) : null}
                  </div>
                </div>
                <div className="field span-2">
                  <label className="checkbox-field">
                    <input type="checkbox" checked={editing.enabled} onChange={(e) => setEditing({ ...editing, enabled: e.target.checked })} />
                    <span>Habilitada para cobrança</span>
                  </label>
                </div>
              </div>
              <div className="modal-stack-footer">
                <button type="button" className="btn btn-primary" onClick={() => void handleUpdate()} disabled={editBusy}>Salvar</button>
                <button type="button" className="btn btn-ghost" onClick={() => setEditing(null)}>Cancelar</button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {viewing ? (
        <div className="dialog-backdrop dialog-backdrop--modal" onClick={() => setViewing(null)}>
          <div className="dialog-card dialog-card--wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal-stack">
              <div className="modal-stack-header">
                <h2 className="manage-ids-title">Credencial</h2>
                <button type="button" className="btn btn-ghost btn-small" onClick={() => setViewing(null)}>Fechar</button>
              </div>
              <p className="muted stack-tight"><strong>ID:</strong> <span className="mono">{viewing.id}</span></p>
              <p className="muted stack-tight"><strong>Nome:</strong> {viewing.name}</p>
              <p className="muted stack-tight"><strong>Cobrança:</strong> {viewing.enabled ? 'Habilitada' : 'Desabilitada'}</p>
              {config.mode === 'token' ? (
                <div className="field">
                  <label>Token</label>
                  <textarea readOnly rows={5} className="nexus-input" value={viewing.token ?? ''} />
                </div>
              ) : (
                <>
                  <div className="field">
                    <label>Chave pública</label>
                    <textarea readOnly rows={3} className="nexus-input" value={viewing.publicKey ?? ''} />
                  </div>
                  <div className="field">
                    <label>Chave secreta</label>
                    <textarea readOnly rows={3} className="nexus-input" value={viewing.secretKey ?? ''} />
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      ) : null}

      <AccountPickerModal
        open={addStrawPickerOpen}
        onClose={() => setAddStrawPickerOpen(false)}
        title="Laranja para credencial"
        subtitle="Opcional. Credenciais sem laranja participam como genéricas no filtro de cobrança."
        onSelected={(row) => {
          setAddStrawManId(row.id);
          setAddStrawLabel(`${row.username} (${row.id})`);
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
        }}
      />

      <AccountPickerModal
        open={editStrawPickerOpen}
        onClose={() => setEditStrawPickerOpen(false)}
        title="Laranja para credencial"
        subtitle="Vincule uma conta ou deixe genérico."
        onSelected={(row) => {
          if (!editing) return;
          setEditing({ ...editing, strawManId: row.id });
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
        }}
      />

      <ConfirmDialog
        open={deleteDialogOpen}
        title="Confirmar exclusão"
        message="Tem certeza que deseja excluir esta credencial?"
        onCancel={() => { setDeleteDialogOpen(false); setDeleteId(''); }}
        onConfirm={() => void confirmDelete()}
      />
    </>
  );
}
