import { Badge } from '@/components/ui/badge';
import { channelToneClass } from '@/lib/channel-tones';
import { cn } from '@/lib/utils';

type ResolutionModeBadgeProps = {
  hostPatterns: string[];
};

export function ResolutionModeBadge({ hostPatterns }: ResolutionModeBadgeProps) {
  const byHost = hostPatterns.length > 0;

  return (
    <Badge
      variant="outline"
      className={cn(
        'font-normal',
        byHost ? channelToneClass('development') : channelToneClass('accent'),
      )}
    >
      {byHost ? 'Por host' : 'Somente por nome'}
    </Badge>
  );
}
