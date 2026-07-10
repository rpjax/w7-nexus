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

  if (!open) return null;

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
  const isCompactStep = step === 'version' || step === 'review';
  const isLatestBase = baseReleaseId !== null && baseReleaseId === latestReleaseId;

  return (
    <div
      className={`scripts-drawer-backdrop ${isCompactStep ? 'scripts-drawer-backdrop--center' : ''}`}
      onClick={onClose}
    >
      <aside
        className={`scripts-drawer scripts-drawer--publish ${isCompactStep ? 'scripts-drawer--compact' : 'scripts-drawer--code'} scripts-drawer--step-${step}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="publish-release-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="scripts-drawer__header">
          <div className="scripts-drawer__header-main">
            <p className="scripts-drawer__kicker">Nova release · {scriptName}</p>
            <h3 id="publish-release-title">Nova release</h3>
            <ol className="scripts-drawer__steps" aria-label="Progresso">
              {STEPS.map((item, index) => (
                <li
                  key={item.id}
                  className={`scripts-drawer__step ${index === stepIndex ? 'is-current' : ''} ${index < stepIndex ? 'is-done' : ''}`}
                  aria-current={index === stepIndex ? 'step' : undefined}
                >
                  <span className="scripts-drawer__step-index">{index + 1}</span>
                  <span className="scripts-drawer__step-label">{item.label}</span>
                </li>
              ))}
            </ol>
          </div>
          <button type="button" className="account-picker-close scripts-drawer__close" onClick={onClose} aria-label="Fechar">
            <span aria-hidden="true">×</span>
          </button>
        </header>

        <div className="scripts-drawer__body">
          {step === 'version' ? (
            <div className="scripts-publish-version">
              <div className="scripts-publish-base">
                <div className="scripts-publish-base__title-row">
                  <label className="scripts-publish-base__label" htmlFor="publish-base-release-select">
                    Release de referência
                  </label>
                  {isLatestBase ? (
                    <span className="scripts-publish-base__tag">Mais recente</span>
                  ) : baseVersion ? (
                    <span className="scripts-publish-base__tag scripts-publish-base__tag--branch">Linha paralela</span>
                  ) : null}
                </div>

                <select
                  id="publish-base-release-select"
                  className="nexus-input scripts-studio-input scripts-publish-base__select"
                  value={baseReleaseId ?? ''}
                  onChange={(e) => handleBaseReleaseChange(e.target.value)}
                  disabled={sortedReleases.length === 0}
                >
                  {sortedReleases.length === 0 ? (
                    <option value="">Primeiro release deste script</option>
                  ) : (
                    sortedReleases.map((release) => (
                      <option key={release.id} value={release.id}>
                        v{release.version}
                      </option>
                    ))
                  )}
                </select>

                <div className="scripts-publish-base__meta">
                  {sortedReleases.length === 0 ? (
                    <p className="scripts-publish-base__hint muted small">
                      Sem releases anteriores — o incremento parte de 0.0.0.
                    </p>
                  ) : baseVersion ? (
                    <>
                      <p className="scripts-publish-base__hint">
                        Incremento a partir de{' '}
                        <span className="mono scripts-publish-base__ref">v{baseVersion}</span>
                      </p>
                      <p className="scripts-publish-base__count muted small">
                        {existingVersions.length === 1
                          ? '1 versão já publicada'
                          : `${existingVersions.length} versões já publicadas`}
                      </p>
                    </>
                  ) : (
                    <p className="scripts-publish-base__hint muted small">
                      Selecione a linha de versão que deseja continuar.
                    </p>
                  )}
                </div>
              </div>

              <div className="scripts-version-options">
                <p className="scripts-version-options__heading">Incremento automático</p>
                <div role="radiogroup" aria-label="Incremento semver" className="scripts-version-options__list">
                  {bumpPreviews.map((option) => (
                    <button
                      key={option.id}
                      type="button"
                      role="radio"
                      aria-checked={versionMode === option.id}
                      className={`scripts-version-option ${versionMode === option.id ? 'is-selected' : ''}`}
                      onClick={() => selectBump(option.id)}
                    >
                      <span className="scripts-version-option__marker" aria-hidden="true" />
                      <span className="scripts-version-option__content">
                        <span className="scripts-version-option__title-row">
                          <strong>{option.title}</strong>
                          {option.recommended ? (
                            <span className="scripts-version-option__badge">Recomendado</span>
                          ) : null}
                        </span>
                        <span className="scripts-version-option__hint muted small">{option.hint}</span>
                        <span className="scripts-version-option__value">
                          {baseVersion ? (
                            <>
                              <span className="mono scripts-version-option__from">{baseVersion}</span>
                              <span className="scripts-version-option__arrow" aria-hidden="true">→</span>
                            </>
                          ) : null}
                          <span className="mono scripts-version-option__to">{formatVersion(option.next)}</span>
                        </span>
                        {option.skipped.length > 0 ? (
                          <span className="scripts-version-option__skip-note">
                            Pula {option.skipped.map((v) => `v${v}`).join(', ')} — próximo slot livre.
                          </span>
                        ) : null}
                      </span>
                    </button>
                  ))}
                </div>

                <p className="scripts-version-options__heading">Personalizado</p>
                <button
                  type="button"
                  role="radio"
                  aria-checked={versionMode === 'manual'}
                  className={`scripts-version-option ${versionMode === 'manual' ? 'is-selected' : ''}`}
                  onClick={selectManual}
                >
                  <span className="scripts-version-option__marker" aria-hidden="true" />
                  <span className="scripts-version-option__content">
                    <strong>Versão manual</strong>
                    <span className="scripts-version-option__hint muted small">
                      Informe major.minor.patch ou use os atalhos a partir da referência.
                    </span>
                  </span>
                </button>
              </div>

              {versionMode === 'manual' ? (
                <div className="scripts-version-manual" aria-label="Versão manual">
                  <label className="scripts-version-manual__combined field">
                    <span>Versão</span>
                    <input
                      type="text"
                      inputMode="decimal"
                      className="nexus-input scripts-studio-input mono"
                      value={semverText}
                      onChange={(e) => handleSemverTextChange(e.target.value)}
                      placeholder="0.0.1"
                      aria-invalid={!semverTextValid || !versionValidation.ok}
                    />
                  </label>

                  <div className="scripts-version-inputs">
                    {(['major', 'minor', 'patch'] as const).map((field) => (
                      <label key={field} className="scripts-version-inputs__field">
                        <span>{field}</span>
                        <div className="scripts-version-stepper">
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            aria-label={`Diminuir ${field}`}
                            onClick={() => adjustField(field, -1)}
                          >
                            −
                          </button>
                          <input
                            type="number"
                            min={0}
                            className="nexus-input scripts-studio-input"
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
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            aria-label={`Aumentar ${field}`}
                            onClick={() => adjustField(field, 1)}
                          >
                            +
                          </button>
                        </div>
                      </label>
                    ))}
                  </div>

                  {baseVersion ? (
                    <div className="scripts-version-quick">
                      <span className="muted small">Atalhos a partir de {baseVersion}:</span>
                      <div className="scripts-version-quick__actions">
                        {BUMP_OPTIONS.map((option) => (
                          <button
                            key={option.id}
                            type="button"
                            className="btn btn-ghost btn-sm"
                            onClick={() => applyQuickBump(option.id)}
                          >
                            {option.id} →{' '}
                            <span className="mono">
                              {formatVersion(resolveBumpFromBase(baseVersion, option.id, existingVersions))}
                            </span>
                          </button>
                        ))}
                      </div>
                    </div>
                  ) : null}

                  {!versionValidation.ok ? (
                    <p className="scripts-version-feedback scripts-version-feedback--error" role="alert">
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
            <div className="scripts-publish-review">
              <dl className="scripts-publish-review__meta">
                <div>
                  <dt>Versão</dt>
                  <dd className="mono">{resolvedVersion}</dd>
                </div>
                <div>
                  <dt>Semver</dt>
                  <dd>{versionModeLabel(versionMode)}</dd>
                </div>
                <div>
                  <dt>Referência</dt>
                  <dd className="mono">{baseVersion ?? '—'}</dd>
                </div>
                <div>
                  <dt>Linhas</dt>
                  <dd>{countSourceLines(sourceCode).toLocaleString('pt-BR')}</dd>
                </div>
                <div>
                  <dt>Origem</dt>
                  <dd>{sourceOrigin === 'file' && sourceFileName ? sourceFileName : sourceOrigin === 'editor' ? 'Editor' : '—'}</dd>
                </div>
              </dl>
              <p className="scripts-publish-review__size muted small">
                Tamanho: <span className="mono">{formatScriptFileSize(getSourceCodeByteSize(sourceCode))}</span>
                {' '}({getSourceCodeByteSize(sourceCode).toLocaleString('pt-BR')} bytes)
              </p>
              {baseVersion ? (
                <p className="scripts-publish-review__delta muted small">
                  {baseVersion} → <span className="mono">{resolvedVersion}</span>
                </p>
              ) : null}
              <p className="muted small">O hash SHA-256 será calculado pelo servidor após publicar.</p>
              <pre className="scripts-publish-review__preview">{sourceCode.slice(0, 400)}{sourceCode.length > 400 ? '…' : ''}</pre>
            </div>
          ) : null}
        </div>

        <div className="scripts-drawer__footer-stack">
          {step === 'version' ? (
            <div className="scripts-publish-summary" aria-live="polite">
              <div className="scripts-publish-summary__label">
                <span className="muted small">Nova versão</span>
                {baseVersion ? (
                  <span className="scripts-publish-summary__delta muted small">
                    {baseVersion} → {resolvedVersion}
                  </span>
                ) : null}
              </div>
              <span className="scripts-publish-summary__version mono">{resolvedVersion}</span>
            </div>
          ) : null}

          <footer className="scripts-drawer__footer">
          {step !== 'version' ? (
            <button
              type="button"
              className="btn btn-scripts-outline"
              disabled={busy}
              onClick={() => setStep(step === 'review' ? 'code' : 'version')}
            >
              Voltar
            </button>
          ) : (
            <button type="button" className="btn btn-scripts-outline" onClick={onClose} disabled={busy}>
              Cancelar
            </button>
          )}

          {step === 'review' ? (
            <button
              type="button"
              className="btn btn-scripts-accent"
              disabled={busy || !sourceCode.trim()}
              onClick={handlePublish}
            >
              {busy ? 'Publicando…' : 'Publicar release'}
            </button>
          ) : (
            <button
              type="button"
              className="btn btn-scripts-accent"
              disabled={(step === 'code' && !sourceCode.trim()) || (step === 'version' && versionStepBlocked)}
              onClick={() => setStep(step === 'version' ? 'code' : 'review')}
            >
              {step === 'version' ? `Continuar · v${resolvedVersion}` : 'Continuar'}
            </button>
          )}
        </footer>
        </div>
      </aside>
    </div>
  );
}
