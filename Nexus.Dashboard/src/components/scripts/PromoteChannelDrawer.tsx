import type { ChannelSummary, ReleaseSummary } from '../../api/scripts/types';
import { channelToneClass } from '@/lib/channel-tones';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';

type PromoteChannelDrawerProps = {
  open: boolean;
  busy: boolean;
  scriptName: string;
  hostPatterns: string[];
  channel: ChannelSummary | null;
  releases: ReleaseSummary[];
  selectedReleaseId: string;
  onSelectRelease: (releaseId: string) => void;
  onClose: () => void;
  onConfirm: () => void;
};

export function PromoteChannelDrawer({
  open,
  busy,
  scriptName,
  hostPatterns,
  channel,
  releases,
  selectedReleaseId,
  onSelectRelease,
  onClose,
  onConfirm,
}: PromoteChannelDrawerProps) {
  if (!channel) return null;

  const selected = releases.find((r) => r.id === selectedReleaseId);
  const beforeVersion = channel.version ?? '—';
  const afterVersion = selected?.version ?? '—';

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent side="right" className="flex w-full flex-col gap-0 overflow-y-auto sm:max-w-md">
        <SheetHeader>
          <p className="text-xs uppercase tracking-wide text-muted-foreground">
            Promover · {scriptName}
          </p>
          <SheetTitle>{channel.displayName}</SheetTitle>
          <SheetDescription className="sr-only">
            Promover release no canal {channel.displayName}
          </SheetDescription>
        </SheetHeader>

        <div className="flex flex-1 flex-col gap-4 px-4">
          <div className="flex items-center justify-center gap-3">
            <Card className="flex-1 py-3">
              <CardContent className="flex flex-col gap-1 px-4">
                <span className="text-xs text-muted-foreground">Atual</span>
                <strong className="font-mono text-sm">{beforeVersion}</strong>
              </CardContent>
            </Card>
            <span className="text-muted-foreground" aria-hidden="true">→</span>
            <Card className={cn('flex-1 py-3', channelToneClass('accent', 'md'))}>
              <CardContent className="flex flex-col gap-1 px-4">
                <span className="text-xs text-muted-foreground">Novo</span>
                <strong className="font-mono text-sm text-warning">{afterVersion}</strong>
              </CardContent>
            </Card>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="promoteReleaseSelect">Release</Label>
            <Select value={selectedReleaseId} onValueChange={onSelectRelease}>
              <SelectTrigger id="promoteReleaseSelect" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {releases.map((release) => (
                  <SelectItem key={release.id} value={release.id}>
                    {release.version}{release.isDeprecated ? ' (deprecated)' : ''}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="rounded-lg border border-border/50 bg-muted/20 p-3">
            <h4 className="text-sm font-medium">Impacto</h4>
            {hostPatterns.length > 0 ? (
              <ul className="mt-2 flex flex-col gap-1">
                {hostPatterns.map((host) => (
                  <li key={host}>
                    <code className="font-mono text-xs">{host}</code>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-2 text-xs text-muted-foreground">
                Este script só é resolvido por nome — promoção afeta <code className="font-mono">?name={scriptName}</code>.
              </p>
            )}
          </div>

          <div className={cn('rounded-lg border p-3', channelToneClass('staging', 'md'))}>
            <strong className="text-sm">Cache invalidado</strong>
            <p className="mt-1 text-xs text-muted-foreground">
              O ScriptCache (L1, TTL ~60s) será limpo. Clientes podem ver a versão anterior por até 1 minuto.
            </p>
          </div>
        </div>

        <SheetFooter className="flex-row justify-end gap-2 border-t border-border/50">
          <Button type="button" variant="outline" onClick={onClose} disabled={busy}>
            Cancelar
          </Button>
          <Button
            type="button"
            variant="secondary"
            className={channelToneClass('accent', 'md')}
            onClick={onConfirm}
            disabled={busy || !selectedReleaseId}
          >
            {busy ? 'Promovendo…' : 'Confirmar promoção'}
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
