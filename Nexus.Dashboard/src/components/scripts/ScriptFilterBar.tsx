import type { ChannelFilter, ResolutionModeFilter } from '../../api/scripts/types';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

type ScriptFilterBarProps = {
  mode: ResolutionModeFilter;
  channel: ChannelFilter;
  visibleCount: number;
  totalCount: number;
  onModeChange: (mode: ResolutionModeFilter) => void;
  onChannelChange: (channel: ChannelFilter) => void;
};

export function ScriptFilterBar({
  mode,
  channel,
  visibleCount,
  totalCount,
  onModeChange,
  onChannelChange,
}: ScriptFilterBarProps) {
  const countLabel = visibleCount === totalCount
    ? `${totalCount} script${totalCount === 1 ? '' : 's'}`
    : `${visibleCount} de ${totalCount} scripts`;

  return (
    <div className="border-b border-border/60 bg-muted/20 px-3.5 py-3.5">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="scripts-mode-filter" className="text-[0.72rem] uppercase tracking-wide text-muted-foreground">
            Modo
          </Label>
          <Select value={mode} onValueChange={(value) => onModeChange(value as ResolutionModeFilter)}>
            <SelectTrigger id="scripts-mode-filter" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="host">Por host</SelectItem>
              <SelectItem value="name-only">Somente por nome</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="scripts-channel-filter" className="text-[0.72rem] uppercase tracking-wide text-muted-foreground">
            Canal
          </Label>
          <Select value={channel} onValueChange={(value) => onChannelChange(value as ChannelFilter)}>
            <SelectTrigger id="scripts-channel-filter" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="prod">Com release em prod</SelectItem>
              <SelectItem value="staging">Com release em staging</SelectItem>
              <SelectItem value="development">Com release em dev</SelectItem>
              <SelectItem value="missing-prod">Sem release em prod</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="col-span-full flex items-center justify-between gap-3 border-t border-border/50 pt-2">
          <span className="text-[0.72rem] uppercase tracking-wide text-muted-foreground">Resultado</span>
          <span className="text-sm tabular-nums text-muted-foreground" aria-live="polite">{countLabel}</span>
        </div>
      </div>
    </div>
  );
}
