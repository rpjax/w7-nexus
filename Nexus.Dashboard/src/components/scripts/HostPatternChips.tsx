type HostPatternChipsProps = {
  patterns: string[];
  max?: number;
};

export function HostPatternChips({ patterns, max = 4 }: HostPatternChipsProps) {
  if (patterns.length === 0) return null;

  const visible = patterns.slice(0, max);
  const hidden = patterns.length - visible.length;

  return (
    <div className="scripts-chips">
      {visible.map((pattern) => (
        <span key={pattern} className="scripts-chip scripts-chip--host">{pattern}</span>
      ))}
      {hidden > 0 ? <span className="scripts-chip scripts-chip--muted">+{hidden}</span> : null}
    </div>
  );
}
