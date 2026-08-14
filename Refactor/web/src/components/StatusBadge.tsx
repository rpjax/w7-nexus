import { Badge } from '@/components/ui/badge';
import { statusLabel } from '@/utils/accountAccess';

export function StatusBadge({ status }: { status?: string | null }) {
  if (!status) {
    return (
      <Badge variant="outline" className="text-muted-foreground">
        Sem status
      </Badge>
    );
  }

  const normalized = status.toLowerCase();
  const variant = normalized === 'disabled' ? 'destructive' : 'success';

  return <Badge variant={variant}>{statusLabel(status)}</Badge>;
}
