import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Card, CardContent } from '@/components/ui/card';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { cn } from '@/lib/utils';
import { flowById, endpointById } from '../../features/api-docs/catalog';
import { MethodBadge } from './MethodBadge';
import { ApiDocsEndpointDetail } from './ApiDocsEndpointDetail';
import type { ApiDocsView } from '../../features/api-docs/types';

type ApiDocsFlowViewProps = {
  flowId: string;
  onNavigate: (view: ApiDocsView) => void;
};

const accentHero: Record<string, string> = {
  blue: 'border-primary/30 bg-primary/5',
  green: 'border-success/30 bg-success/5',
  amber: 'border-warning/30 bg-warning/5',
  violet: 'border-purple-400/30 bg-purple-400/5',
  rose: 'border-rose-400/30 bg-rose-400/5',
};

const accentDot: Record<string, string> = {
  blue: 'bg-primary',
  green: 'bg-success',
  amber: 'bg-warning',
  violet: 'bg-purple-400',
  rose: 'bg-rose-400',
};

const calloutStyles: Record<string, string> = {
  why: 'border-primary/20 bg-primary/5',
  outcome: 'border-success/20 bg-success/5',
  tip: 'border-purple-400/20 bg-purple-400/5',
  warning: 'border-warning/25 bg-warning/5',
};

export function ApiDocsFlowView({ flowId, onNavigate }: ApiDocsFlowViewProps) {
  const flow = flowById.get(flowId);
  const [activeStep, setActiveStep] = useState(0);

  useEffect(() => {
    setActiveStep(0);
  }, [flowId]);

  if (!flow) {
    return (
      <div className="flex flex-col items-center gap-3 px-4 py-12 text-muted-foreground">
        <p>Fluxo não encontrado.</p>
      </div>
    );
  }

  const step = flow.steps[activeStep];
  const endpoint = step?.endpointId ? endpointById.get(step.endpointId) : undefined;
  const progress = ((activeStep + 1) / flow.steps.length) * 100;

  return (
    <article>
      <header className={cn('mb-4 rounded-xl border p-5', accentHero[flow.accent] ?? accentHero.blue)}>
        <div className="mb-2 flex items-center gap-2">
          <span
            className={cn('size-2 shrink-0 rounded-full', accentDot[flow.accent] ?? accentDot.blue)}
            aria-hidden="true"
          />
          <span className="text-[0.72rem] text-muted-foreground">
            Fluxo guiado · ~{flow.estimatedMinutes} min
          </span>
        </div>
        <h2 className="mb-1.5 text-[clamp(1.25rem,3.5vw,1.6rem)] font-bold">{flow.title}</h2>
        <p className="mb-4 text-[0.9rem] leading-relaxed text-muted-foreground">{flow.description}</p>

        <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-3">
          <Card className="border-border bg-background/50 shadow-none">
            <CardContent className="pt-4">
              <span className="mb-1 block text-[0.68rem] font-semibold uppercase tracking-wide text-muted-foreground">
                Para quem
              </span>
              <span className="text-[0.84rem] leading-snug">{flow.audience}</span>
            </CardContent>
          </Card>
          <Card className="border-border bg-background/50 shadow-none">
            <CardContent className="pt-4">
              <span className="mb-1 block text-[0.68rem] font-semibold uppercase tracking-wide text-muted-foreground">
                Pré-requisitos
              </span>
              <ul className="m-0 list-disc pl-4 text-[0.82rem] leading-snug text-muted-foreground">
                {flow.prerequisites.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </CardContent>
          </Card>
          <Card className="border-success/25 bg-background/50 shadow-none">
            <CardContent className="pt-4">
              <span className="mb-1 block text-[0.68rem] font-semibold uppercase tracking-wide text-muted-foreground">
                Resultado esperado
              </span>
              <span className="text-[0.84rem] leading-snug">{flow.outcome}</span>
            </CardContent>
          </Card>
        </div>
      </header>

      <div className="mb-3.5" aria-label={`Passo ${activeStep + 1} de ${flow.steps.length}`}>
        <Progress value={progress} className="mb-1.5 h-1" />
        <span className="text-xs text-muted-foreground">
          Passo {activeStep + 1} de {flow.steps.length}
        </span>
      </div>

      <ToggleGroup
        type="single"
        value={String(activeStep)}
        onValueChange={(value) => {
          if (value) setActiveStep(Number(value));
        }}
        variant="outline"
        size="sm"
        className="mb-4 flex flex-wrap justify-start gap-1.5"
        aria-label="Passos do fluxo"
      >
        {flow.steps.map((s, index) => (
          <ToggleGroupItem
            key={s.title}
            value={String(index)}
            className={cn(
              'max-w-full gap-1.5 rounded-full px-2.5 py-1.5 text-[0.78rem] data-[state=on]:border-primary/40 data-[state=on]:bg-primary/15',
              index < activeStep && 'border-success/30 text-success',
            )}
            aria-current={index === activeStep ? 'step' : undefined}
          >
            <span className="text-[0.72rem] font-bold">{index + 1}</span>
            <span className="truncate">{s.title}</span>
          </ToggleGroupItem>
        ))}
      </ToggleGroup>

      {step ? (
        <section className="overflow-hidden rounded-xl border border-border bg-card/35">
          <header className="border-b border-border bg-background/40 px-4 py-4">
            <h3 className="mb-1 text-[1.15rem] font-semibold">{step.title}</h3>
            <p className="m-0 text-[0.88rem] text-primary">{step.summary}</p>
          </header>

          <div className="flex flex-col gap-3.5 px-4 py-4">
            <div>
              <h4 className="mb-1.5 text-[0.78rem] font-semibold uppercase tracking-wide text-muted-foreground">
                O que acontece
              </h4>
              {step.narrative.split('\n\n').map((paragraph) => (
                <p key={paragraph.slice(0, 40)} className="mb-2.5 text-[0.9rem] leading-relaxed last:mb-0">
                  {paragraph}
                </p>
              ))}
            </div>

            <Accordion type="multiple" defaultValue={['why', 'outcome', 'tip', 'pitfalls']}>
              <AccordionItem value="why" className="rounded-lg border px-3">
                <AccordionTrigger className={cn('py-3 hover:no-underline', calloutStyles.why)}>
                  <span className="text-[0.78rem] font-semibold uppercase tracking-wide text-muted-foreground">
                    Por que este passo importa
                  </span>
                </AccordionTrigger>
                <AccordionContent className="pb-3">
                  <p className="m-0 text-[0.86rem] leading-relaxed">{step.why}</p>
                </AccordionContent>
              </AccordionItem>

              <AccordionItem value="outcome" className="mt-2 rounded-lg border px-3">
                <AccordionTrigger className={cn('py-3 hover:no-underline', calloutStyles.outcome)}>
                  <span className="text-[0.78rem] font-semibold uppercase tracking-wide text-muted-foreground">
                    O que você terá ao concluir
                  </span>
                </AccordionTrigger>
                <AccordionContent className="pb-3">
                  <p className="m-0 text-[0.86rem] leading-relaxed">{step.outcome}</p>
                </AccordionContent>
              </AccordionItem>

              {step.tip ? (
                <AccordionItem value="tip" className="mt-2 rounded-lg border px-3">
                  <AccordionTrigger className={cn('py-3 hover:no-underline', calloutStyles.tip)}>
                    <span className="text-[0.78rem] font-semibold uppercase tracking-wide text-muted-foreground">
                      Dica prática
                    </span>
                  </AccordionTrigger>
                  <AccordionContent className="pb-3">
                    <p className="m-0 text-[0.86rem] leading-relaxed">{step.tip}</p>
                  </AccordionContent>
                </AccordionItem>
              ) : null}

              {step.pitfalls && step.pitfalls.length > 0 ? (
                <AccordionItem value="pitfalls" className="mt-2 rounded-lg border px-3">
                  <AccordionTrigger className={cn('py-3 hover:no-underline', calloutStyles.warning)}>
                    <span className="text-[0.78rem] font-semibold uppercase tracking-wide text-muted-foreground">
                      Armadilhas comuns
                    </span>
                  </AccordionTrigger>
                  <AccordionContent className="pb-3">
                    <ul className="m-0 list-disc pl-4 text-[0.86rem] leading-relaxed">
                      {step.pitfalls.map((pitfall) => (
                        <li key={pitfall}>{pitfall}</li>
                      ))}
                    </ul>
                  </AccordionContent>
                </AccordionItem>
              ) : null}
            </Accordion>
          </div>

          {endpoint ? (
            <Accordion type="single" collapsible className="border-t border-border">
              <AccordionItem value="technical" className="border-none">
                <AccordionTrigger className="flex-wrap gap-2 bg-background/35 px-4 py-3 text-[0.84rem] font-medium hover:no-underline">
                  <span>Referência técnica do endpoint</span>
                  <MethodBadge method={endpoint.method} compact />
                  <code className="max-w-full truncate text-[0.75rem] text-muted-foreground">{endpoint.path}</code>
                </AccordionTrigger>
                <AccordionContent className="border-t border-border px-4 pb-4">
                  <ApiDocsEndpointDetail endpoint={endpoint} embedded />
                </AccordionContent>
              </AccordionItem>
            </Accordion>
          ) : null}

          <footer className="flex flex-wrap justify-between gap-2 border-t border-border bg-background/30 px-4 py-3.5">
            <Button
              type="button"
              variant="outline"
              disabled={activeStep === 0}
              onClick={() => setActiveStep((s) => s - 1)}
            >
              ← Anterior
            </Button>
            {activeStep < flow.steps.length - 1 ? (
              <Button
                type="button"
                onClick={() => setActiveStep((s) => s + 1)}
              >
                Próximo passo →
              </Button>
            ) : (
              <Button type="button" onClick={() => onNavigate({ kind: 'overview' })}>
                Concluir fluxo
              </Button>
            )}
          </footer>
        </section>
      ) : null}
    </article>
  );
}
