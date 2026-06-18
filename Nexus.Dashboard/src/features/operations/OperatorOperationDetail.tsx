import type { OperationDetails } from '../../api/types';
import { formatDateTime, shortId } from '../../utils/format';

type OperatorOperationDetailProps = {
  operation: OperationDetails;
};

function formatPercent(value: number): string {
  const rounded = Math.round(value * 100) / 100;
  return Number.isInteger(rounded) ? `${rounded}%` : `${rounded.toFixed(1)}%`;
}

export function OperatorOperationDetail({ operation }: OperatorOperationDetailProps) {
  const description = operation.description?.trim();

  return (
    <article className="admin-op-card admin-op-card--operator admin-op-card--detail">
      <section className="admin-op-section">
        <div className="admin-op-section__head">
          <div>
            <span className="admin-op-section__kicker">Operação</span>
            <h3 className="admin-op-card-title">{operation.name}</h3>
            <p className="admin-op-section-desc muted small">
              {description || 'Sem descrição cadastrada.'}
            </p>
          </div>
        </div>
        <div className="admin-op-section__body">
          <dl className="admin-op-identity__facts">
            <div className="admin-op-fact">
              <dt>ID</dt>
              <dd className="mono admin-op-fact-id-text" title={operation.id}>{shortId(operation.id, 24)}</dd>
            </div>
            <div className="admin-op-fact">
              <dt>Criada</dt>
              <dd>{formatDateTime(operation.createdAt)}</dd>
            </div>
            <div className="admin-op-fact">
              <dt>Atualizada</dt>
              <dd>{formatDateTime(operation.updatedAt)}</dd>
            </div>
          </dl>
        </div>
      </section>

      <section className="admin-op-section">
        <div className="admin-op-section__head">
          <div>
            <h4 className="admin-op-section-title">Suas equipes</h4>
            <p className="admin-op-section-desc muted small">
              Equipes em que você está alocado nesta operação.
            </p>
          </div>
        </div>
        <div className="admin-op-section__body">
          {operation.teams.length === 0 ? (
            <p className="admin-op-empty muted small">Nenhuma equipe vinculada.</p>
          ) : (
            <ul className="admin-op-operator-list">
              {operation.teams.map((team) => {
                const teamRow = team as typeof team & { profitShareRule?: { cuts: { accountId: string; username: string; percentage: number }[] } };
                const cuts = teamRow.profitShareRule?.cuts ?? [];
                return (
                  <li key={team.id} className="admin-op-operator-card">
                    <div className="admin-op-operator-card__head">
                      <div className="admin-op-operator-card__meta">
                        <span className="admin-op-person-name">{team.name}</span>
                        <span className="admin-op-person-id mono" title={team.id}>
                          Equipe · {shortId(team.id, 18)}
                        </span>
                      </div>
                    </div>
                    <div className="admin-op-operator-card__repasse">
                      <span className="admin-op-operator-card__repasse-label">Repasse</span>
                      {cuts.length === 0 ? (
                        <p className="admin-op-operator-card__repasse-empty muted small">
                          Sem repasse configurado para você nesta equipe.
                        </p>
                      ) : (
                        <ul className="admin-op-profit-cuts">
                          {cuts.map((cut) => (
                            <li key={`${cut.accountId}-${cut.percentage}`}>
                              <span className="admin-op-profit-cut-name">{cut.username || shortId(cut.accountId, 18)}</span>
                              <span className="admin-op-profit-cut-pct">{formatPercent(cut.percentage)}</span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      </section>
    </article>
  );
}
