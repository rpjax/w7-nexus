import { authLabel } from '../../features/api-docs/utils';
import type { AuthLevel } from '../../features/api-docs/types';

type AuthBadgeProps = {
  auth: AuthLevel;
};

export function AuthBadge({ auth }: AuthBadgeProps) {
  return (
    <span className={`api-auth-badge api-auth-badge--${auth}`}>
      {authLabel(auth)}
    </span>
  );
}
