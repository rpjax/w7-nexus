import type { ChannelSummary, ReleaseSummary } from '../../api/scripts/types';

type ReleaseStudioOverviewProps = {
  releases: ReleaseSummary[];
  channels: ChannelSummary[];
};

export function ReleaseStudioOverview({ releases, channels }: ReleaseStudioOverviewProps) {
  const latest = releases[0] ?? null;
  const deprecatedCount = releases.filter((release) => release.isDeprecated).length;
  const promotedCount = releases.filter((release) => release.promotedChannelRouteValues.length > 0).length;
  const prod = channels.find((channel) => channel.routeValue === 'prod');

  return (
    <div className="flex flex-wrap gap-3 rounded-lg border border-border/60 bg-card/40 px-4 py-3">
      <OverviewItem label="Releases" value={String(releases.length)} />
      <OverviewItem label="Mais recente" value={latest?.version ?? '—'} mono />
      <OverviewItem label="Prod" value={prod?.version ?? 'sem release'} mono />
      <OverviewItem label="Em canal" value={String(promotedCount)} />
      <OverviewItem label="Deprecated" value={String(deprecatedCount)} />
    </div>
  );
}

function OverviewItem({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex min-w-[5.5rem] flex-col gap-0.5">
      <span className="text-[0.72rem] uppercase tracking-wide text-muted-foreground">{label}</span>
      <strong className={mono ? 'font-mono text-sm' : 'text-sm'}>{value}</strong>
    </div>
  );
}
