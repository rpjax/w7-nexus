import type { OperationDetails } from '../../api/types';
import { formatDateTime, shortId } from '../../utils/format';

type AdminOperationCardProps = {
  operation: OperationDetails;
  accountLabels: Record<string, string>;
  actionBusy: boolean;
  onAssignAdministrator: (operationId: string) => void;
  onRemoveAdministrator: (operationId: string, administratorId: string) => void;
  onDelete: (operationId: string) => void;
};

function adminLabel(id: string, accountLabels: Record<string, string>): string {
  return accountLabels[id] ? `${accountLabels[id]} · ${shortId(id, 10)}` : shortId(id, 18);
}

export function AdminOperationCard({
  operation,
  accountLabels,
  actionBusy,
  onAssignAdministrator,
  onRemoveAdministrator,
  onDelete,
}: AdminOperationCardProps) {
  const adminCount = operation.administratorIds.length;
  const description = operation.description?.trim();

  async function copyId() {
    try {
      await navigator.clipboard.writeText(operation.id);
    } catch {
      // ignore clipboard errors
    }
  }

  return (
    <article className="admin-op-card">
      <header className="admin-op-card-header">
        <div className="admin-op-card-heading">
          <h3 className="admin-op-card-title">{operation.name}</h3>
          <p className="admin-op-card-id">
            <span className="mono" title={operation.id}>{operation.id}</span>
            <button type="button" className="btn btn-ghost btn-small admin-op-copy-id" onClick={() => void copyId()}>
              Copiar ID
            </button>
          </p>
        </div>
        <span className={`count-pill ${adminCount === 0 ? 'count-pill-warn' : ''}`}>
          {adminCount === 0 ? 'Sem admins' : `${adminCount} admin${adminCount === 1 ? '' : 's'}`}
        </span>
      </header>

      <div className="admin-op-card-body">
        <div className="admin-op-meta-grid">
          <div className="admin-op-meta-item">
            <span className="admin-op-meta-label">Descrição</span>
            <p className="admin-op-meta-value">{description || 'Sem descrição cadastrada.'}</p>
          </div>
          <div className="admin-op-meta-item">
            <span className="admin-op-meta-label">Criada em</span>
            <p className="admin-op-meta-value">{formatDateTime(operation.createdAt)}</p>
          </div>
          <div className="admin-op-meta-item">
            <span className="admin-op-meta-label">Atualizada em</span>
            <p className="admin-op-meta-value">{formatDateTime(operation.updatedAt)}</p>
          </div>
        </div>

        <section className="admin-op-section">
          <div className="admin-op-section-head">
            <h4>Administradores da operação</h4>
            <p className="muted small">Contas autorizadas a gerenciar equipes e configuração desta operação.</p>
          </div>

          {adminCount === 0 ? (
            <p className="muted small admin-op-empty">Nenhum administrador vinculado.</p>
          ) : (
            <ul className="chip-list admin-op-admin-list">
              {operation.administratorIds.map((adminId) => (
                <li key={adminId}>
                  <span>{adminLabel(adminId, accountLabels)}</span>
                  <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    disabled={actionBusy}
                    onClick={() => onRemoveAdministrator(operation.id, adminId)}
                  >
                    Remover
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <footer className="admin-op-card-actions">
        <button
          type="button"
          className="btn btn-primary btn-small"
          disabled={actionBusy}
          onClick={() => onAssignAdministrator(operation.id)}
        >
          Vincular administrador
        </button>
        <button
          type="button"
          className="btn btn-danger btn-small"
          disabled={actionBusy}
          onClick={() => onDelete(operation.id)}
        >
          Excluir operação
        </button>
      </footer>
    </article>
  );
}
