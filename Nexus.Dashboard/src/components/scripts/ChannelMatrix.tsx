import type { ChannelSummary, ReleaseSummary } from '../../api/scripts/types';
import { truncateHash } from '../../features/scripts/formatRelativeTime';
import { channelToneClass } from '@/lib/channel-tones';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { cn } from '@/lib/utils';

type ChannelMatrixProps = {
  channels: ChannelSummary[];
  releases?: ReleaseSummary[];
  promotionSelections?: Record<string, string>;
  onPromotionSelect?: (channelRouteValue: string, releaseId: string) => void;
  onPromote?: (channel: ChannelSummary) => void;
  /** Denser rows for the studio overview panel. */
  compact?: boolean;
};

const CHANNEL_BORDER: Record<string, string> = {
  prod: 'border-l-success',
  staging: 'border-l-warning',
  development: 'border-l-primary',
};

const CHANNEL_DOT: Record<string, string> = {
  prod: 'bg-success',
  staging: 'bg-warning',
  development: 'bg-primary',
};

const DEFAULT_CHANNEL_ORDER = ['prod', 'staging', 'development'];

function orderChannels(channels: ChannelSummary[]): ChannelSummary[] {
  return [
    ...DEFAULT_CHANNEL_ORDER.flatMap((route) => channels.filter((channel) => channel.routeValue === route)),
    ...channels.filter((channel) => !DEFAULT_CHANNEL_ORDER.includes(channel.routeValue)),
  ];
}

function channelBorderClass(routeValue: string) {
  return CHANNEL_BORDER[routeValue] ?? 'border-l-muted-foreground/40';
}

function channelDotClass(routeValue: string) {
  return CHANNEL_DOT[routeValue] ?? 'bg-muted-foreground/50';
}

function ChannelDot({ routeValue }: { routeValue: string }) {
  return (
    <span
      className={cn('size-2 shrink-0 rounded-full', channelDotClass(routeValue))}
      aria-hidden="true"
    />
  );
}

function DeprecatedBadge() {
  return (
    <Badge variant="destructive" className="text-[0.65rem] font-normal">
      Deprecated
    </Badge>
  );
}

function ChannelIdentity({ channel, compact }: { channel: ChannelSummary; compact: boolean }) {
  return (
    <div className={cn('flex flex-col gap-0.5', compact && 'gap-1')}>
      <div className="flex items-center gap-2">
        <ChannelDot routeValue={channel.routeValue} />
        <strong className={cn('text-sm', compact && 'text-sm')}>{channel.displayName}</strong>
      </div>
      <span className="font-mono text-xs text-muted-foreground">{channel.routeValue}</span>
    </div>
  );
}

function CurrentRelease({ channel, compact }: { channel: ChannelSummary; compact: boolean }) {
  if (!channel.version) {
    return (
      <span className="text-sm text-muted-foreground" aria-label="Sem release">
        Sem release
      </span>
    );
  }

  return (
    <div className={cn('flex flex-col gap-0.5', compact && 'gap-1')}>
      <div className="flex flex-wrap items-center gap-1.5">
        <span className="font-mono text-sm">{channel.version}</span>
        {channel.isDeprecated ? <DeprecatedBadge /> : null}
      </div>
      {channel.hash ? (
        <span className="font-mono text-xs text-muted-foreground" title={channel.hash}>
          {truncateHash(channel.hash)}
        </span>
      ) : null}
    </div>
  );
}

export function ChannelMatrix({
  channels,
  releases,
  promotionSelections,
  onPromotionSelect,
  onPromote,
  compact = false,
}: ChannelMatrixProps) {
  const promotionMode = Boolean(releases?.length && onPromotionSelect);
  const canPromote = Boolean(onPromote && (releases?.length ?? 0) > 0);
  const orderedChannels = orderChannels(channels);

  if (promotionMode) {
    return (
      <div className="flex flex-col gap-3">
        {orderedChannels.map((channel) => {
          const selectedReleaseId = promotionSelections?.[channel.routeValue] ?? releases?.[0]?.id ?? '';
          const selectedRelease = releases!.find((release) => release.id === selectedReleaseId) ?? null;
          const isCurrentSelection = Boolean(
            channel.currentReleaseId
            && selectedReleaseId
            && channel.currentReleaseId === selectedReleaseId,
          );

          return (
            <Card
              key={channel.routeValue}
              className={cn('border-l-4 py-3', channelBorderClass(channel.routeValue))}
            >
              <CardHeader className="flex-row items-center justify-between space-y-0 px-4 pb-2">
                <div className="flex items-center gap-2">
                  <ChannelDot routeValue={channel.routeValue} />
                  <span className="font-mono text-xs text-muted-foreground">{channel.routeValue}</span>
                  <span className="text-sm font-medium">{channel.displayName}</span>
                </div>
                {isCurrentSelection ? (
                  <Badge variant="outline" className="text-xs font-normal">
                    Atual
                  </Badge>
                ) : null}
              </CardHeader>

              <CardContent className="flex flex-col gap-3 px-4 pt-0">
                <div>
                  <span className="text-xs text-muted-foreground">Em produção no canal</span>
                  <div className="mt-1 flex flex-wrap items-center gap-1.5">
                    {channel.version ? (
                      <>
                        <span className="font-mono text-sm">{channel.version}</span>
                        {channel.hash ? (
                          <span className="font-mono text-xs text-muted-foreground" title={channel.hash}>
                            · {truncateHash(channel.hash)}
                          </span>
                        ) : null}
                      </>
                    ) : (
                      <span className="text-sm text-muted-foreground">Nenhum release</span>
                    )}
                  </div>
                </div>

                <div className="flex flex-col gap-1.5">
                  <Label htmlFor={`promo-${channel.routeValue}`} className="text-xs text-muted-foreground">
                    Promover para
                  </Label>
                  <div className="flex flex-wrap items-center gap-2">
                    <Select
                      value={selectedReleaseId}
                      onValueChange={(value) => onPromotionSelect?.(channel.routeValue, value)}
                    >
                      <SelectTrigger id={`promo-${channel.routeValue}`} className="min-w-[10rem] flex-1">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {releases!.map((release) => (
                          <SelectItem key={release.id} value={release.id}>
                            {release.version}{release.isDeprecated ? ' (deprecated)' : ''}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {isCurrentSelection ? (
                      <span className="text-xs text-muted-foreground">Já nesta versão</span>
                    ) : (
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        className={cn(channelToneClass('accent', 'md'))}
                        disabled={!canPromote || !selectedReleaseId}
                        title={
                          !canPromote
                            ? 'Publique um release antes de promover'
                            : `Promover ${selectedRelease?.version ?? 'release'} em ${channel.displayName}`
                        }
                        onClick={() => onPromote!(channel)}
                      >
                        Promover
                      </Button>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}

        {!canPromote ? (
          <p className="text-xs text-muted-foreground">
            Publique ao menos um release para habilitar promoções entre canais.
          </p>
        ) : null}
      </div>
    );
  }

  return (
    <Table>
      {!compact ? (
        <TableHeader>
          <TableRow>
            <TableHead>Canal</TableHead>
            <TableHead>Release promovido</TableHead>
            {onPromote ? <TableHead className="text-right">Ação</TableHead> : null}
          </TableRow>
        </TableHeader>
      ) : null}
      <TableBody>
        {orderedChannels.map((channel) => {
          const selectedReleaseId = releases?.[0]?.id ?? '';
          const isOnLatestRelease = Boolean(
            channel.currentReleaseId
            && selectedReleaseId
            && channel.currentReleaseId === selectedReleaseId,
          );
          const showCurrentStatus = Boolean(onPromote && isOnLatestRelease);
          const showPromoteAction = Boolean(onPromote && !showCurrentStatus);

          return (
            <TableRow
              key={channel.routeValue}
              className={cn('border-l-4', channelBorderClass(channel.routeValue), compact && 'h-auto')}
            >
              <TableCell className={cn(compact && 'py-2.5')}>
                <ChannelIdentity channel={channel} compact={compact} />
              </TableCell>

              <TableCell className={cn(compact && 'py-2.5')}>
                <CurrentRelease channel={channel} compact={compact} />
              </TableCell>

              {onPromote ? (
                <TableCell className={cn('text-right', compact && 'py-2.5')}>
                  {showCurrentStatus ? (
                    <Badge variant="outline" className="text-xs font-normal">
                      Atual
                    </Badge>
                  ) : null}

                  {showPromoteAction ? (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={!canPromote}
                      title={
                        !canPromote
                          ? 'Publique um release antes de promover'
                          : channel.version
                            ? `Trocar release em ${channel.displayName}`
                            : `Promover release em ${channel.displayName}`
                      }
                      onClick={() => onPromote!(channel)}
                    >
                      {compact ? 'Promover' : 'Promover →'}
                    </Button>
                  ) : null}
                </TableCell>
              ) : null}
            </TableRow>
          );
        })}
      </TableBody>
      {onPromote && !canPromote ? (
        <caption className="mt-2 text-left text-xs text-muted-foreground">
          Publique ao menos um release para habilitar promoções entre canais.
        </caption>
      ) : null}
    </Table>
  );
}
