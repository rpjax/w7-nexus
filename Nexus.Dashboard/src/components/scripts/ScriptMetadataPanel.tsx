import { useState } from 'react';
import { channelToneClass } from '@/lib/channel-tones';
import { Button } from '@/components/ui/button';
import { Card, CardAction, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
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
    <Card className="border-border/60">
      <CardHeader className="border-b border-border/50">
        <CardTitle>Metadados</CardTitle>
        {isDirty || saving ? (
          <CardAction className="flex items-center gap-2">
            {isDirty ? (
              <Badge variant="warning" className="font-normal">
                Não salvo
              </Badge>
            ) : null}
            <Button
              type="button"
              size="sm"
              variant="secondary"
              className={channelToneClass('accent', 'md')}
              disabled={saving || !isDirty}
              onClick={onSave}
            >
              {saving ? 'Salvando…' : 'Salvar'}
            </Button>
          </CardAction>
        ) : null}
      </CardHeader>

      <CardContent className="flex flex-col gap-5 pt-4">
        <section className="flex flex-col gap-2">
          <div className="flex items-center justify-between gap-2">
            <Label htmlFor="studioDesc">Descrição</Label>
            {canCollapseDescription ? (
              <Button
                type="button"
                variant="link"
                size="sm"
                className="h-auto px-0 text-xs"
                onClick={() => setDescExpanded((open) => !open)}
              >
                {descExpanded ? 'Recolher' : 'Ver tudo'}
              </Button>
            ) : null}
          </div>
          <Textarea
            id="studioDesc"
            className={cn(!descExpanded && canCollapseDescription && 'max-h-24')}
            rows={descExpanded ? 6 : 3}
            value={description}
            placeholder="Descreva o papel deste script no ecossistema"
            onChange={(e) => onDescriptionChange(e.target.value)}
            onFocus={() => setDescExpanded(true)}
          />
        </section>

        <section className="flex flex-col gap-2">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <Label htmlFor="studioPriority">Prioridade</Label>
              <p className="mt-1 text-xs text-muted-foreground">
                {hasHosts
                  ? 'Menor número = injeta primeiro quando o host casa com vários scripts.'
                  : 'Relevante depois que hosts forem configurados abaixo.'}
              </p>
            </div>
            <div className="flex items-center gap-1">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={saving}
                onClick={() => onPriorityChange(Math.max(0, priority - 1))}
                aria-label="Diminuir prioridade"
              >
                −
              </Button>
              <Input
                id="studioPriority"
                type="number"
                min={0}
                className="w-20 text-center"
                value={priority}
                onChange={(e) => onPriorityChange(Number(e.target.value))}
              />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={saving}
                onClick={() => onPriorityChange(priority + 1)}
                aria-label="Aumentar prioridade"
              >
                +
              </Button>
            </div>
          </div>
        </section>

        <section className="flex flex-col gap-4" aria-labelledby="metadata-resolution-title">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div>
              <h3 id="metadata-resolution-title" className="text-sm font-medium">
                Como clientes obtêm o script
              </h3>
              <p className="mt-1 text-xs text-muted-foreground">
                Todo script responde por nome. Hosts adicionais habilitam injeção automática por domínio.
              </p>
            </div>
            <ResolutionModeBadge hostPatterns={hostPatterns} />
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className={cn('rounded-lg border p-3', channelToneClass('accent', 'md'))}>
              <p className="text-sm font-medium">Por nome</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Sempre disponível — o cliente pede o script pelo identificador.
              </p>
              <code className="mt-2 block font-mono text-xs">
                GET /scripts?name={scriptName}
              </code>
            </div>

            <div
              className={cn(
                'rounded-lg border p-3',
                hasHosts
                  ? channelToneClass('development', 'md')
                  : 'border-border/50 bg-muted/20',
              )}
            >
              <p className="text-sm font-medium">
                Por host
                <span className="font-normal text-muted-foreground"> — opcional</span>
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                {hasHosts
                  ? 'Clientes nestes domínios recebem o script sem passar o nome.'
                  : 'Adicione um domínio (ex.: *.olx.com.br) para ativar lookup por host.'}
              </p>
              <div className="mt-3">
                <HostPatternEditor
                  patterns={hostPatterns}
                  onChange={onHostPatternsChange}
                  disabled={saving}
                  compactEmpty
                  hideHint={false}
                  placeholder="*.seudominio.com.br"
                />
              </div>
              {hasHosts ? (
                <p className="mt-2 text-xs text-muted-foreground">
                  Endpoint: <code className="font-mono">GET /scripts?host=…&channel=prod</code>
                </p>
              ) : null}
            </div>
          </div>
        </section>
      </CardContent>
    </Card>
  );
}
