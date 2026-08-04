import { Badge } from '@/components/ui/badge';
import { channelToneClass } from '@/lib/channel-tones';
import { cn } from '@/lib/utils';

type HostPatternChipsProps = {
  patterns: string[];
  max?: number;
};

export function HostPatternChips({ patterns, max = 4 }: HostPatternChipsProps) {
  if (patterns.length === 0) return null;

  const visible = patterns.slice(0, max);
  const hidden = patterns.length - visible.length;

  return (
    <div className="flex flex-wrap gap-1.5">
      {visible.map((pattern) => (
        <Badge
          key={pattern}
          variant="outline"
          className={cn(channelToneClass('accent'), 'font-mono text-xs font-normal text-foreground')}
        >
          {pattern}
        </Badge>
      ))}
      {hidden > 0 ? (
        <Badge variant="secondary" className="text-xs font-normal">
          +{hidden}
        </Badge>
      ) : null}
    </div>
  );
}
