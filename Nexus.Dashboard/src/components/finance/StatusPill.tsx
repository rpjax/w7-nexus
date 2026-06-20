type StatusPillProps = {
  label: string;
  tone?: 'info' | 'success' | 'warn' | 'danger';
};

export function StatusPill({ label, tone = 'info' }: StatusPillProps) {
  const toneClass = tone === 'success'
    ? 'count-pill'
    : tone === 'warn'
      ? 'count-pill-warn'
      : tone === 'danger'
        ? 'count-pill-warn'
        : 'count-pill-warm';
  return <span className={`count-pill ${toneClass}`.trim()}>{label}</span>;
}
