type ResolutionModeBadgeProps = {
  hostPatterns: string[];
};

export function ResolutionModeBadge({ hostPatterns }: ResolutionModeBadgeProps) {
  const byHost = hostPatterns.length > 0;

  return (
    <span className={`scripts-badge ${byHost ? 'scripts-badge--host' : 'scripts-badge--name'}`}>
      {byHost ? 'Por host' : 'Somente por nome'}
    </span>
  );
}
