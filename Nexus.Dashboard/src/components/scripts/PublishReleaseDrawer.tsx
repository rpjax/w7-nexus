import { useEffect, useMemo, useState } from 'react';
import { ReleaseSourcePanel } from './ReleaseSourcePanel';
import {
  BUMP_OPTIONS,
  bumpVersion,
  formatVersion,
  parseVersion,
  validateReleaseVersion,
  versionModeLabel,
  type VersionBump,
  type VersionMode,
  type VersionTuple,
} from '../../features/scripts/semanticVersion';
import { countSourceLines, formatScriptFileSize, getSourceCodeByteSize } from '../../features/scripts/readScriptFile';

type PublishReleaseDrawerProps = {
  open: boolean;
  busy: boolean;
  scriptName: string;
  latestVersion: string | null;
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

export function PublishReleaseDrawer({
  open,
  busy,
  scriptName,
  latestVersion,
  onClose,
  onSubmit,
}: PublishReleaseDrawerProps) {
  const [step, setStep] = useState<Step>('version');
  const [versionMode, setVersionMode] = useState<VersionMode>('patch');
  const [major, setMajor] = useState(0);
  const [minor, setMinor] = useState(0);
  const [patch, setPatch] = useState(1);
  const [semverText, setSemverText] = useState('0.0.1');
  const [sourceCode, setSourceCode] = useState('');
  const [sourceFileName, setSourceFileName] = useState<string | null>(null);
  const [sourceOrigin, setSourceOrigin] = useState<'file' | 'editor' | null>(null);

  const bumpPreviews = useMemo(
    () =>
      BUMP_OPTIONS.map((option) => ({
        ...option,
        next: bumpVersion(latestVersion, option.id),
      })),
    [latestVersion],
  );

  const resolvedTuple = useMemo<VersionTuple>(() => {
    if (versionMode === 'manual') {
      return tupleFromFields(major, minor, patch);
    }

    return bumpVersion(latestVersion, versionMode);
  }, [versionMode, major, minor, patch, latestVersion]);

  const resolvedVersion = formatVersion(resolvedTuple);

  const versionValidation = useMemo(
    () => validateReleaseVersion(resolvedTuple, latestVersion),
    [resolvedTuple, latestVersion],
  );

  const semverTextValid = parseVersion(semverText.trim()) !== null;
  const versionStepBlocked = versionMode === 'manual' && (!versionValidation.ok || !semverTextValid);

  useEffect(() => {
    if (!open) {
      setStep('version');
      setVersionMode('patch');
      setSourceCode('');
      setSourceFileName(null);
      setSourceOrigin(null);
      return;
    }

    const next = bumpVersion(latestVersion, 'patch');
    setMajor(next[0]);
    setMinor(next[1]);
    setPatch(next[2]);
    setSemverText(formatVersion(next));
  }, [open, latestVersion]);

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
    applyTuple(bumpVersion(latestVersion, level));
  }

  function selectManual() {
    const seed = versionMode === 'manual'
      ? tupleFromFields(major, minor, patch)
      : bumpVersion(latestVersion, versionMode);
    setVersionMode('manual');
    applyTuple(seed);
  }

  function applyQuickBump(level: VersionBump) {
    setVersionMode('manual');
    applyTuple(bumpVersion(latestVersion, level));
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
              <p className="scripts-publish-version__lead scripts-publish-version__context muted small">
                {latestVersion
                  ? <>Último release: <span className="mono">{latestVersion}</span></>
                  : 'Primeiro release deste script — escolha o ponto de partida semver.'}
              </p>

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
                          {latestVersion ? (
                            <>
                              <span className="mono scripts-version-option__from">{latestVersion}</span>
                              <span className="scripts-version-option__arrow" aria-hidden="true">→</span>
                            </>
                          ) : null}
                          <span className="mono">{formatVersion(option.next)}</span>
                        </span>
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
                      Informe major.minor.patch ou use os atalhos abaixo.
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

                  <div className="scripts-version-quick">
                    <span className="muted small">Atalhos a partir do último:</span>
                    <div className="scripts-version-quick__actions">
                      {BUMP_OPTIONS.map((option) => (
                        <button
                          key={option.id}
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => applyQuickBump(option.id)}
                        >
                          {option.id} → <span className="mono">{formatVersion(bumpVersion(latestVersion, option.id))}</span>
                        </button>
                      ))}
                    </div>
                  </div>

                  {!versionValidation.ok ? (
                    <p className="scripts-version-feedback scripts-version-feedback--error" role="alert">
                      {versionValidation.message}
                    </p>
                  ) : (
                    <p className="scripts-version-feedback muted small">
                      Próxima versão: <span className="mono">{resolvedVersion}</span>
                    </p>
                  )}
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
              {latestVersion ? (
                <p className="scripts-publish-review__delta muted small">
                  {latestVersion} → <span className="mono">{resolvedVersion}</span>
                </p>
              ) : null}
              <p className="muted small">O hash SHA-256 será calculado pelo servidor após publicar.</p>
              <pre className="scripts-publish-review__preview">{sourceCode.slice(0, 400)}{sourceCode.length > 400 ? '…' : ''}</pre>
            </div>
          ) : null}
        </div>

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
              Continuar
            </button>
          )}
        </footer>
      </aside>
    </div>
  );
}
