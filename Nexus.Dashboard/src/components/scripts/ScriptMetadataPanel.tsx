import { useState } from 'react';
import { HostPatternEditor } from './HostPatternEditor';
import { ResolutionModeBadge } from './ResolutionModeBadge';

type ScriptMetadataPanelProps = {
  scriptName: string;
  description: string;
  priority: number;
  hostPatterns: string[];
  saving: boolean;
  isDirty: boolean;
  onDescriptionChange: (value: string) => void;
  onPriorityChange: (value: number) => void;
  onHostPatternsChange: (patterns: string[]) => void;
  onSave: () => void;
};

const DESCRIPTION_COLLAPSE_THRESHOLD = 220;

export function ScriptMetadataPanel({
  scriptName,
  description,
  priority,
  hostPatterns,
  saving,
  isDirty,
  onDescriptionChange,
  onPriorityChange,
  onHostPatternsChange,
  onSave,
}: ScriptMetadataPanelProps) {
  const hasHosts = hostPatterns.length > 0;
  const [descExpanded, setDescExpanded] = useState(false);
  const canCollapseDescription = description.length > DESCRIPTION_COLLAPSE_THRESHOLD;

  return (
    <section className="scripts-panel scripts-panel--metadata">
      <header className="scripts-panel__head scripts-metadata-panel__head">
        <h2>Metadados</h2>
        {isDirty || saving ? (
          <div className="scripts-metadata-panel__head-actions">
            {isDirty ? <span className="scripts-panel__dirty">Não salvo</span> : null}
            <button
              type="button"
              className="btn btn-scripts-accent btn-sm"
              disabled={saving || !isDirty}
              onClick={onSave}
            >
              {saving ? 'Salvando…' : 'Salvar'}
            </button>
          </div>
        ) : null}
      </header>

      <div className="scripts-panel__body scripts-metadata-panel__body">
        <section className="scripts-metadata-block">
          <div className="scripts-metadata-field__label-row">
            <label htmlFor="studioDesc">Descrição</label>
            {canCollapseDescription ? (
              <button
                type="button"
                className="scripts-metadata-field__toggle"
                onClick={() => setDescExpanded((open) => !open)}
              >
                {descExpanded ? 'Recolher' : 'Ver tudo'}
              </button>
            ) : null}
          </div>
          <textarea
            id="studioDesc"
            className={[
              'nexus-input',
              'scripts-studio-input',
              'scripts-studio-desc-input',
              canCollapseDescription && !descExpanded ? 'is-collapsed' : '',
            ].filter(Boolean).join(' ')}
            rows={descExpanded ? 6 : 3}
            value={description}
            placeholder="Descreva o papel deste script no ecossistema"
            onChange={(e) => onDescriptionChange(e.target.value)}
            onFocus={() => setDescExpanded(true)}
          />
        </section>

        <section className="scripts-metadata-block scripts-metadata-block--priority">
          <div className="scripts-metadata-priority">
            <div>
              <label htmlFor="studioPriority" className="scripts-metadata-priority__label">
                Prioridade
              </label>
              <p className="scripts-metadata-panel__hint muted small">
                {hasHosts
                  ? 'Menor número = injeta primeiro quando o host casa com vários scripts.'
                  : 'Relevante depois que hosts forem configurados abaixo.'}
              </p>
            </div>
            <div className="scripts-priority-stepper scripts-studio-priority">
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                disabled={saving}
                onClick={() => onPriorityChange(Math.max(0, priority - 1))}
                aria-label="Diminuir prioridade"
              >
                −
              </button>
              <input
                id="studioPriority"
                type="number"
                min={0}
                className="nexus-input scripts-priority-stepper__input scripts-studio-input"
                value={priority}
                onChange={(e) => onPriorityChange(Number(e.target.value))}
              />
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                disabled={saving}
                onClick={() => onPriorityChange(priority + 1)}
                aria-label="Aumentar prioridade"
              >
                +
              </button>
            </div>
          </div>
        </section>

        <section className="scripts-metadata-block scripts-metadata-resolution" aria-labelledby="metadata-resolution-title">
          <div className="scripts-metadata-resolution__head">
            <div>
              <h3 id="metadata-resolution-title" className="scripts-metadata-resolution__title">
                Como clientes obtêm o script
              </h3>
              <p className="scripts-metadata-panel__hint muted small">
                Todo script responde por nome. Hosts adicionais habilitam injeção automática por domínio.
              </p>
            </div>
            <ResolutionModeBadge hostPatterns={hostPatterns} />
          </div>

          <div className="scripts-metadata-resolution__modes">
            <div className="scripts-metadata-resolution__mode scripts-metadata-resolution__mode--active">
              <p className="scripts-metadata-resolution__mode-label">Por nome</p>
              <p className="scripts-metadata-resolution__mode-desc muted small">
                Sempre disponível — o cliente pede o script pelo identificador.
              </p>
              <code className="scripts-metadata-resolution__endpoint mono">
                GET /scripts?name={scriptName}
              </code>
            </div>

            <div className={`scripts-metadata-resolution__mode ${hasHosts ? 'scripts-metadata-resolution__mode--active' : ''}`}>
              <p className="scripts-metadata-resolution__mode-label">
                Por host
                <span className="muted"> — opcional</span>
              </p>
              <p className="scripts-metadata-resolution__mode-desc muted small">
                {hasHosts
                  ? 'Clientes nestes domínios recebem o script sem passar o nome.'
                  : 'Adicione um domínio (ex.: *.olx.com.br) para ativar lookup por host.'}
              </p>
              <HostPatternEditor
                patterns={hostPatterns}
                onChange={onHostPatternsChange}
                disabled={saving}
                compactEmpty
                hideHint={false}
                placeholder="*.seudominio.com.br"
              />
              {hasHosts ? (
                <p className="scripts-metadata-resolution__endpoint-hint muted small">
                  Endpoint: <code className="mono">GET /scripts?host=…&channel=prod</code>
                </p>
              ) : null}
            </div>
          </div>
        </section>
      </div>
    </section>
  );
}
