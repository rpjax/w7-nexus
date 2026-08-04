import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { authLabel } from '../../features/api-docs/utils';
import type { AuthLevel } from '../../features/api-docs/types';

type AuthBadgeProps = {
  auth: AuthLevel;
};

const authStyles: Record<AuthLevel, string> = {
  none: 'text-success border-success/30',
  jwt: 'text-primary border-primary/30',
  'master-token': 'text-warning border-warning/30',
};

export function AuthBadge({ auth }: AuthBadgeProps) {
  return (
    <Badge variant="outline" className={cn('rounded-full text-[0.65rem]', authStyles[auth])}>
      {authLabel(auth)}
    </Badge>
  );
}
