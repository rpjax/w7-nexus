import { methodTone } from '../../features/api-docs/utils';
import type { HttpMethod } from '../../features/api-docs/types';

type MethodBadgeProps = {
  method: HttpMethod;
  compact?: boolean;
};

export function MethodBadge({ method, compact }: MethodBadgeProps) {
  return (
    <span className={`api-method api-method--${methodTone(method)}${compact ? ' api-method--compact' : ''}`}>
      {method}
    </span>
  );
}
