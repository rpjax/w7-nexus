import { useEffect, useState } from 'react';
import { flowById, endpointById } from '../../features/api-docs/catalog';
import { MethodBadge } from './MethodBadge';
import { ApiDocsEndpointDetail } from './ApiDocsEndpointDetail';
import type { ApiDocsView } from '../../features/api-docs/types';

type ApiDocsFlowViewProps = {
  flowId: string;
  onNavigate: (view: ApiDocsView) => void;
};

export function ApiDocsFlowView({ flowId, onNavigate }: ApiDocsFlowViewProps) {
  const flow = flowById.get(flowId);
  const [activeStep, setActiveStep] = useState(0);
  const [showTechnical, setShowTechnical] = useState(false);

  useEffect(() => {
    setActiveStep(0);
    setShowTechnical(false);
  }, [flowId]);

  if (!flow) {
    return (
      <div className="api-docs-empty">
        <p>Fluxo não encontrado.</p>
      </div>
    );
  }

  const step = flow.steps[activeStep];
  const endpoint = step?.endpointId ? endpointById.get(step.endpointId) : undefined;
  const progress = ((activeStep + 1) / flow.steps.length) * 100;

  return (
    <article className="api-flow-page">
      <header className={`api-flow-page__hero api-flow-page__hero--${flow.accent}`}>
        <div className="api-flow-page__hero-top">
          <span className={`api-flow-dot api-flow-dot--${flow.accent}`} aria-hidden="true" />
          <span className="api-flow-page__badge">Fluxo guiado · ~{flow.estimatedMinutes} min</span>
        </div>
        <h2 className="api-flow-page__title">{flow.title}</h2>
        <p className="api-flow-page__lead">{flow.description}</p>

        <div className="api-flow-page__meta">
          <div className="api-flow-meta-card">
            <span className="api-flow-meta-card__label">Para quem</span>
            <span className="api-flow-meta-card__value">{flow.audience}</span>
          </div>
          <div className="api-flow-meta-card">
            <span className="api-flow-meta-card__label">Pré-requisitos</span>
            <ul className="api-flow-meta-card__list">
              {flow.prerequisites.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </div>
          <div className="api-flow-meta-card api-flow-meta-card--outcome">
            <span className="api-flow-meta-card__label">Resultado esperado</span>
            <span className="api-flow-meta-card__value">{flow.outcome}</span>
          </div>
        </div>
      </header>

      <div className="api-flow-progress" aria-label={`Passo ${activeStep + 1} de ${flow.steps.length}`}>
        <div className="api-flow-progress__bar">
          <div className="api-flow-progress__fill" style={{ width: `${progress}%` }} />
        </div>
        <span className="api-flow-progress__label">
          Passo {activeStep + 1} de {flow.steps.length}
        </span>
      </div>

      <nav className="api-flow-tabs" aria-label="Passos do fluxo">
        {flow.steps.map((s, index) => (
          <button
            key={s.title}
            type="button"
            className={`api-flow-tab${index === activeStep ? ' is-active' : ''}${index < activeStep ? ' is-done' : ''}`}
            onClick={() => {
              setActiveStep(index);
              setShowTechnical(false);
            }}
            aria-current={index === activeStep ? 'step' : undefined}
          >
            <span className="api-flow-tab__num">{index + 1}</span>
            <span className="api-flow-tab__label">{s.title}</span>
          </button>
        ))}
      </nav>

      {step ? (
        <section className="api-flow-step-panel">
          <header className="api-flow-step-panel__header">
            <h3>{step.title}</h3>
            <p className="api-flow-step-panel__summary">{step.summary}</p>
          </header>

          <div className="api-flow-step-panel__body">
            <div className="api-prose-block">
              <h4>O que acontece</h4>
              {step.narrative.split('\n\n').map((paragraph) => (
                <p key={paragraph.slice(0, 40)}>{paragraph}</p>
              ))}
            </div>

            <div className="api-callout api-callout--why">
              <h4>Por que este passo importa</h4>
              <p>{step.why}</p>
            </div>

            <div className="api-callout api-callout--outcome">
              <h4>O que você terá ao concluir</h4>
              <p>{step.outcome}</p>
            </div>

            {step.tip ? (
              <div className="api-callout api-callout--tip">
                <h4>Dica prática</h4>
                <p>{step.tip}</p>
              </div>
            ) : null}

            {step.pitfalls && step.pitfalls.length > 0 ? (
              <div className="api-callout api-callout--warning">
                <h4>Armadilhas comuns</h4>
                <ul>
                  {step.pitfalls.map((pitfall) => (
                    <li key={pitfall}>{pitfall}</li>
                  ))}
                </ul>
              </div>
            ) : null}
          </div>

          {endpoint ? (
            <div className="api-flow-technical">
              <button
                type="button"
                className="api-flow-technical__toggle"
                aria-expanded={showTechnical}
                onClick={() => setShowTechnical((v) => !v)}
              >
                <span>Referência técnica do endpoint</span>
                <MethodBadge method={endpoint.method} compact />
                <code>{endpoint.path}</code>
                <svg
                  className={`api-flow-technical__chevron${showTechnical ? ' is-open' : ''}`}
                  viewBox="0 0 16 16"
                  width="14"
                  height="14"
                  aria-hidden="true"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.75"
                >
                  <path d="M4 6l4 4 4-4" />
                </svg>
              </button>
              {showTechnical ? (
                <div className="api-flow-technical__panel">
                  <ApiDocsEndpointDetail endpoint={endpoint} embedded />
                </div>
              ) : null}
            </div>
          ) : null}

          <footer className="api-flow-step-panel__footer">
            <button
              type="button"
              className="btn"
              disabled={activeStep === 0}
              onClick={() => {
                setActiveStep((s) => s - 1);
                setShowTechnical(false);
              }}
            >
              ← Anterior
            </button>
            {activeStep < flow.steps.length - 1 ? (
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => {
                  setActiveStep((s) => s + 1);
                  setShowTechnical(false);
                }}
              >
                Próximo passo →
              </button>
            ) : (
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => onNavigate({ kind: 'overview' })}
              >
                Concluir fluxo
              </button>
            )}
          </footer>
        </section>
      ) : null}
    </article>
  );
}
