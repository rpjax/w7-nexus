type StatCardProps = {
  label: string;
  value: string;
  caption?: string;
  tone?: 'info' | 'success' | 'danger' | 'warn';
};

export function StatCard({ label, value, caption, tone }: StatCardProps) {
  return (
    <section className="stat-card">
      <p className="stat-label">{label}</p>
      <p className="stat-value">
        {tone ? <span className={`status-dot tone-${tone}`} aria-hidden="true" /> : null}
        {value}
      </p>
      {caption ? <p className="stat-caption">{caption}</p> : null}
    </section>
  );
}
