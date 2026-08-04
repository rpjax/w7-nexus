import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { methodTone } from '../../features/api-docs/utils';
import type { HttpMethod } from '../../features/api-docs/types';

type MethodBadgeProps = {
  method: HttpMethod;
  compact?: boolean;
};

const methodStyles: Record<string, string> = {
  get: 'bg-success/20 text-success border-success/20',
  post: 'bg-primary/20 text-primary border-primary/20',
  put: 'bg-warning/20 text-warning border-warning/20',
  patch: 'bg-purple-500/20 text-purple-300 border-purple-500/20',
  delete: 'bg-destructive/20 text-destructive border-destructive/20',
};

export function MethodBadge({ method, compact }: MethodBadgeProps) {
  const tone = methodTone(method);

  return (
    <Badge
      variant="outline"
      className={cn(
        'min-w-12 justify-center rounded-md font-bold tracking-wide',
        compact ? 'min-w-10 px-1.5 text-[0.6rem]' : 'px-1.5 text-[0.65rem]',
        methodStyles[tone] ?? methodStyles.get,
      )}
    >
      {method}
    </Badge>
  );
}
