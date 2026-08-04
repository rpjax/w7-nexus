import { useEffect, useMemo, useRef, useState } from 'react';
import { ReleaseSourcePanel } from './ReleaseSourcePanel';
import {
  BUMP_OPTIONS,
  formatVersion,
  listSkippedVersions,
  parseVersion,
  resolveBumpFromBase,
  sortReleaseOptions,
  validateReleaseVersion,
  versionModeLabel,
  type VersionBump,
  type VersionMode,
  type VersionTuple,
} from '../../features/scripts/semanticVersion';
import { countSourceLines, formatScriptFileSize, getSourceCodeByteSize } from '../../features/scripts/readScriptFile';
import {
  Sheet,
  SheetContent,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { channelToneClass } from '@/lib/channel-tones';
import { cn } from '@/lib/utils';

export type PublishReleaseOption = {
  id: string;
  version: string;
};

type PublishReleaseDrawerProps = {
  open: boolean;
  busy: boolean;
  scriptName: string;
  releases: PublishReleaseOption[];
  onClose: () => void;
  onSubmit: (payload: {
    sourceCode: string;
    major?: number;
    minor?: number;
    patch?: number;
  }) => void;
};

type Step = 'version' | 'code' | 'review';

const STEPS: { id: Step; label: string }[] = [
  { id: 'version', label: 'Versão' },
  { id: 'code', label: 'Código' },
  { id: 'review', label: 'Revisão' },
];

function tupleFromFields(major: number, minor: number, patch: number): VersionTuple {
  return [major, minor, patch];
}

function seedFromBase(
  baseVersion: string | null,
  mode: VersionMode,
  existingVersions: readonly string[],
): VersionTuple {
  if (mode === 'manual') {
    return resolveBumpFromBase(baseVersion, 'patch', existingVersions);
  }
  return resolveBumpFromBase(baseVersion, mode, existingVersions);
}

export function PublishReleaseDrawer({
  open,
  busy,
  scriptName,
  releases,
  onClose,
  onSubmit,
}: PublishReleaseDrawerProps) {
  const [step, setStep] = useState<Step>('version');
  const [versionMode, setVersionMode] = useState<VersionMode>('patch');
  const [baseReleaseId, setBaseReleaseId] = useState<string | null>(null);
  const [major, setMajor] = useState(0);
  const [minor, setMinor] = useState(0);
  const [patch, setPatch] = useState(1);
  const [semverText, setSemverText] = useState('0.0.1');
  const [sourceCode, setSourceCode] = useState('');
  const [sourceFileName, setSourceFileName] = useState<string | null>(null);
  const [sourceOrigin, setSourceOrigin] = useState<'file' | 'editor' | null>(null);
  const wasOpenRef = useRef(false);

  const sortedReleases = useMemo(
    () => sortReleaseOptions(releases),
    [releases],
  );

  const latestReleaseId = sortedReleases[0]?.id ?? null;

  const existingVersions = useMemo(
    () => sortedReleases.map((release) => release.version),
    [sortedReleases],
  );

  const baseVersion = useMemo(() => {
    if (!baseReleaseId) return null;
    return sortedReleases.find((release) => release.id === baseReleaseId)?.version ?? null;
  }, [baseReleaseId, sortedReleases]);

  const bumpPreviews = useMemo(
    () =>
      BUMP_OPTIONS.map((option) => {
        const skipped = listSkippedVersions(baseVersion, option.id, existingVersions);
        return {
          ...option,
          next: resolveBumpFromBase(baseVersion, option.id, existingVersions),
          skipped,
        };
      }),
    [baseVersion, existingVersions],
  );

  const resolvedTuple = useMemo<VersionTuple>(() => {
    if (versionMode === 'manual') {
      return tupleFromFields(major, minor, patch);
    }
    return resolveBumpFromBase(baseVersion, versionMode, existingVersions);
  }, [versionMode, major, minor, patch, baseVersion, existingVersions]);

  const resolvedVersion = formatVersion(resolvedTuple);

  const versionValidation = useMemo(
    () => validateReleaseVersion(resolvedTuple, existingVersions),
    [resolvedTuple, existingVersions],
  );

  const semverTextValid = parseVersion(semverText.trim()) !== null;
  const versionStepBlocked = !versionValidation.ok || (versionMode === 'manual' && !semverTextValid);

  useEffect(() => {
    if (!open) {
      wasOpenRef.current = false;
      setStep('version');
      setVersionMode('patch');
      setSourceCode('');
      setSourceFileName(null);
      setSourceOrigin(null);
      return;
    }

    if (wasOpenRef.current) return;
    wasOpenRef.current = true;

    const defaultBaseId = sortedReleases[0]?.id ?? null;
    const defaultBaseVersion = sortedReleases[0]?.version ?? null;
    setBaseReleaseId(defaultBaseId);
    setVersionMode('patch');
    const next = seedFromBase(defaultBaseVersion, 'patch', existingVersions);
    setMajor(next[0]);
    setMinor(next[1]);
    setPatch(next[2]);
    setSemverText(formatVersion(next));
  }, [open, sortedReleases, existingVersions]);

  useEffect(() => {
    if (!open || !wasOpenRef.current || versionMode === 'manual') return;
    const next = resolveBumpFromBase(baseVersion, versionMode, existingVersions);
    setMajor(next[0]);
    setMinor(next[1]);
    setPatch(next[2]);
    setSemverText(formatVersion(next));
  }, [open, baseVersion, versionMode, existingVersions]);

  useEffect(() => {
    if (!open || step !== 'code') return;

    function handleKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 's') {
        event.preventDefault();
      }
    }

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [open, step]);

  function applyTuple(next: VersionTuple) {
    setMajor(next[0]);
    setMinor(next[1]);
    setPatch(next[2]);
    setSemverText(formatVersion(next));
  }

  function selectBump(level: VersionBump) {
    setVersionMode(level);
    applyTuple(resolveBumpFromBase(baseVersion, level, existingVersions));
  }

  function selectManual() {
    const seed = versionMode === 'manual'
      ? tupleFromFields(major, minor, patch)
      : resolveBumpFromBase(baseVersion, versionMode, existingVersions);
    setVersionMode('manual');
    applyTuple(seed);
  }

  function applyQuickBump(level: VersionBump) {
    setVersionMode('manual');
    applyTuple(resolveBumpFromBase(baseVersion, level, existingVersions));
  }

  function handleBaseReleaseChange(releaseId: string) {
    setBaseReleaseId(releaseId || null);
    if (versionMode !== 'manual') {
      const nextBase = sortedReleases.find((release) => release.id === releaseId)?.version ?? null;
      applyTuple(resolveBumpFromBase(nextBase, versionMode, existingVersions));
    }
  }

  function handleSemverTextChange(value: string) {
    setSemverText(value);
    const parsed = parseVersion(value.trim());
    if (parsed) applyTuple(parsed);
  }

  function adjustField(field: 'major' | 'minor' | 'patch', delta: number) {
    const next: VersionTuple = [major, minor, patch];
    if (field === 'major') next[0] = Math.max(0, major + delta);
    if (field === 'minor') next[1] = Math.max(0, minor + delta);
    if (field === 'patch') next[2] = Math.max(0, patch + delta);
    applyTuple(next);
  }

  function handlePublish() {
    const [m, n, p] = resolvedTuple;
    onSubmit({ sourceCode, major: m, minor: n, patch: p });
  }

  const stepIndex = STEPS.findIndex((item) => item.id === step);
  const isLatestBase = baseReleaseId !== null && baseReleaseId === latestReleaseId;

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="right"
        className={cn(
          'flex w-full flex-col gap-0 overflow-y-auto p-0',
          step === 'code' ? 'sm:max-w-2xl' : 'sm:max-w-lg',
        )}
      >
        <SheetHeader className="border-b border-border/50 px-4 py-4">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">
            Nova release · {scriptName}
          </p>
          <SheetTitle>Nova release</SheetTitle>
          <ol className="mt-3 flex gap-2" aria-label="Progresso">
            {STEPS.map((item, index) => (
              <li
                key={item.id}
                className={cn(
                  'flex items-center gap-1.5 rounded-md px-2 py-1 text-xs',
                  index === stepIndex && 'bg-warning/15 text-warning',
                  index < stepIndex && 'text-muted-foreground',
                  index > stepIndex && 'text-muted-foreground/60',
                )}
                aria-current={index === stepIndex ? 'step' : undefined}
              >
                <span className="font-mono">{index + 1}</span>
                <span>{item.label}</span>
              </li>
            ))}
          </ol>
        </SheetHeader>

        <div className="flex-1 px-4 py-4">
          {step === 'version' ? (
            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between gap-2">
                  <Label htmlFor="publish-base-release-select">Release de referência</Label>
                  {isLatestBase ? (
                    <Badge variant="outline" className="text-xs font-normal">Mais recente</Badge>
                  ) : baseVersion ? (
                    <Badge variant="secondary" className="text-xs font-normal">Linha paralela</Badge>
                  ) : null}
                </div>

                <Select
                  value={baseReleaseId ?? undefined}
                  onValueChange={handleBaseReleaseChange}
                  disabled={sortedReleases.length === 0}
                >
                  <SelectTrigger id="publish-base-release-select" className="w-full">
                    <SelectValue placeholder="Primeiro release deste script" />
                  </SelectTrigger>
                  <SelectContent>
                    {sortedReleases.map((release) => (
                      <SelectItem key={release.id} value={release.id}>
                        v{release.version}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <div>
                  {sortedReleases.length === 0 ? (
                    <p className="text-xs text-muted-foreground">
                      Sem releases anteriores — o incremento parte de 0.0.0.
                    </p>
                  ) : baseVersion ? (
                    <>
                      <p className="text-sm">
                        Incremento a partir de{' '}
                        <span className="font-mono text-warning">v{baseVersion}</span>
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {existingVersions.length === 1
                          ? '1 versão já publicada'
                          : `${existingVersions.length} versões já publicadas`}
                      </p>
                    </>
                  ) : (
                    <p className="text-xs text-muted-foreground">
                      Selecione a linha de versão que deseja continuar.
                    </p>
                  )}
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Incremento semver
                </p>
                <RadioGroup
                  value={versionMode}
                  onValueChange={(value) => {
                    if (value === 'manual') selectManual();
                    else selectBump(value as VersionBump);
                  }}
                  aria-label="Incremento semver"
                  className="gap-2"
                >
                  {bumpPreviews.map((option) => (
                    <Label
                      key={option.id}
                      htmlFor={`bump-${option.id}`}
                      className={cn(
                        'flex cursor-pointer gap-3 rounded-lg border p-3 transition-colors',
                        versionMode === option.id
                          ? 'border-warning/40 bg-warning/8'
                          : 'border-border/50 hover:bg-muted/30',
                      )}
                    >
                      <RadioGroupItem value={option.id} id={`bump-${option.id}`} className="mt-1" />
                      <span className="flex min-w-0 flex-col gap-1">
                        <span className="flex flex-wrap items-center gap-2">
                          <strong className="text-sm">{option.title}</strong>
                          {option.recommended ? (
                            <Badge variant="outline" className="text-[0.65rem] font-normal">
                              Recomendado
                            </Badge>
                          ) : null}
                        </span>
                        <span className="text-xs text-muted-foreground">{option.hint}</span>
                        <span className="font-mono text-sm">
                          {baseVersion ? (
                            <>
                              <span className="text-muted-foreground">{baseVersion}</span>
                              <span className="mx-1 text-muted-foreground" aria-hidden="true">→</span>
                            </>
                          ) : null}
                          <span className="text-warning">{formatVersion(option.next)}</span>
                        </span>
                        {option.skipped.length > 0 ? (
                          <span className="text-xs text-warning">
                            Pula {option.skipped.map((v) => `v${v}`).join(', ')} — próximo slot livre.
                          </span>
                        ) : null}
                      </span>
                    </Label>
                  ))}

                  <Label
                    htmlFor="bump-manual"
                    className={cn(
                      'flex cursor-pointer gap-3 rounded-lg border p-3 transition-colors',
                      versionMode === 'manual'
                        ? 'border-warning/40 bg-warning/8'
                        : 'border-border/50 hover:bg-muted/30',
                    )}
                  >
                    <RadioGroupItem value="manual" id="bump-manual" className="mt-1" />
                    <span className="flex flex-col gap-1">
                      <strong className="text-sm">Versão manual</strong>
                      <span className="text-xs text-muted-foreground">
                        Informe major.minor.patch ou use os atalhos a partir da referência.
                      </span>
                    </span>
                  </Label>
                </RadioGroup>
              </div>

              {versionMode === 'manual' ? (
                <div className="flex flex-col gap-3 rounded-lg border border-border/50 bg-muted/20 p-3" aria-label="Versão manual">
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="publish-semver-text">Versão</Label>
                    <Input
                      id="publish-semver-text"
                      type="text"
                      inputMode="decimal"
                      className="font-mono"
                      value={semverText}
                      onChange={(e) => handleSemverTextChange(e.target.value)}
                      placeholder="0.0.1"
                      aria-invalid={!semverTextValid || !versionValidation.ok}
                    />
                  </div>

                  <div className="grid grid-cols-3 gap-2">
                    {(['major', 'minor', 'patch'] as const).map((field) => (
                      <div key={field} className="flex flex-col gap-1">
                        <Label className="text-xs capitalize">{field}</Label>
                        <div className="flex items-center gap-0.5">
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            aria-label={`Diminuir ${field}`}
                            onClick={() => adjustField(field, -1)}
                          >
                            −
                          </Button>
                          <Input
                            type="number"
                            min={0}
                            className="text-center"
                            value={field === 'major' ? major : field === 'minor' ? minor : patch}
                            onChange={(e) => {
                              const value = Math.max(0, Number(e.target.value) || 0);
                              applyTuple([
                                field === 'major' ? value : major,
                                field === 'minor' ? value : minor,
                                field === 'patch' ? value : patch,
                              ]);
                            }}
                          />
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            aria-label={`Aumentar ${field}`}
                            onClick={() => adjustField(field, 1)}
                          >
                            +
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>

                  {baseVersion ? (
                    <div className="flex flex-col gap-2">
                      <span className="text-xs text-muted-foreground">Atalhos a partir de {baseVersion}:</span>
                      <div className="flex flex-wrap gap-1">
                        {BUMP_OPTIONS.map((option) => (
                          <Button
                            key={option.id}
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => applyQuickBump(option.id)}
                          >
                            {option.id} →{' '}
                            <span className="font-mono">
                              {formatVersion(resolveBumpFromBase(baseVersion, option.id, existingVersions))}
                            </span>
                          </Button>
                        ))}
                      </div>
                    </div>
                  ) : null}

                  {!versionValidation.ok ? (
                    <p className="text-sm text-destructive" role="alert">
                      {versionValidation.message}
                    </p>
                  ) : null}
                </div>
              ) : null}
            </div>
          ) : null}

          {step === 'code' ? (
            <ReleaseSourcePanel
              versionLabel={resolvedVersion}
              value={sourceCode}
              fileName={sourceFileName}
              origin={sourceOrigin}
              onChange={setSourceCode}
              onFileNameChange={setSourceFileName}
              onOriginChange={setSourceOrigin}
            />
          ) : null}

          {step === 'review' ? (
            <div className="flex flex-col gap-3">
              <dl className="grid gap-3 sm:grid-cols-2">
                <ReviewItem label="Versão" value={resolvedVersion} mono />
                <ReviewItem label="Semver" value={versionModeLabel(versionMode)} />
                <ReviewItem label="Referência" value={baseVersion ?? '—'} mono />
                <ReviewItem label="Linhas" value={countSourceLines(sourceCode).toLocaleString('pt-BR')} />
                <ReviewItem
                  label="Origem"
                  value={sourceOrigin === 'file' && sourceFileName ? sourceFileName : sourceOrigin === 'editor' ? 'Editor' : '—'}
                />
              </dl>
              <p className="text-xs text-muted-foreground">
                Tamanho: <span className="font-mono">{formatScriptFileSize(getSourceCodeByteSize(sourceCode))}</span>
                {' '}({getSourceCodeByteSize(sourceCode).toLocaleString('pt-BR')} bytes)
              </p>
              {baseVersion ? (
                <p className="text-xs text-muted-foreground">
                  {baseVersion} → <span className="font-mono">{resolvedVersion}</span>
                </p>
              ) : null}
              <p className="text-xs text-muted-foreground">O hash SHA-256 será calculado pelo servidor após publicar.</p>
              <pre className="max-h-40 overflow-auto rounded-lg border border-border/50 bg-muted p-3 font-mono text-xs">
                {sourceCode.slice(0, 400)}{sourceCode.length > 400 ? '…' : ''}
              </pre>
            </div>
          ) : null}
        </div>

        <div className="mt-auto border-t border-border/50">
          {step === 'version' ? (
            <div className="flex items-center justify-between gap-3 px-4 py-3" aria-live="polite">
              <div className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">Nova versão</span>
                {baseVersion ? (
                  <span className="text-xs text-muted-foreground">
                    {baseVersion} → {resolvedVersion}
                  </span>
                ) : null}
              </div>
              <span className="font-mono text-lg font-semibold text-warning">{resolvedVersion}</span>
            </div>
          ) : null}

          <SheetFooter className="flex-row justify-end gap-2 px-4 pb-4">
            {step !== 'version' ? (
              <Button
                type="button"
                variant="outline"
                disabled={busy}
                onClick={() => setStep(step === 'review' ? 'code' : 'version')}
              >
                Voltar
              </Button>
            ) : (
              <Button type="button" variant="outline" onClick={onClose} disabled={busy}>
                Cancelar
              </Button>
            )}

            {step === 'review' ? (
              <Button
                type="button"
                variant="secondary"
                className={channelToneClass('accent', 'md')}
                disabled={busy || !sourceCode.trim()}
                onClick={handlePublish}
              >
                {busy ? 'Publicando…' : 'Publicar release'}
              </Button>
            ) : (
              <Button
                type="button"
                variant="secondary"
                className={channelToneClass('accent', 'md')}
                disabled={(step === 'code' && !sourceCode.trim()) || (step === 'version' && versionStepBlocked)}
                onClick={() => setStep(step === 'version' ? 'code' : 'review')}
              >
                {step === 'version' ? `Continuar · v${resolvedVersion}` : 'Continuar'}
              </Button>
            )}
          </SheetFooter>
        </div>
      </SheetContent>
    </Sheet>
  );
}

function ReviewItem({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className={cn('text-sm', mono && 'font-mono')}>{value}</dd>
    </div>
  );
}
