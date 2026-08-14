import { reportError } from './port';
import type { ApiResultLike } from './types';

export function messageFromResult(
  result: ApiResultLike<unknown>,
  emptyMessage = 'Resposta inválida.',
): string | null {
  if (!result.ok) return result.error;
  if (result.data == null) return emptyMessage;
  return null;
}

/** Reports a user error when the API result failed. Returns true if failed. */
export function reportIfFailed(
  result: ApiResultLike<unknown>,
  emptyMessage?: string,
): result is { ok: false; error: string; status?: number } {
  const message = messageFromResult(result, emptyMessage);
  if (!message) return false;
  reportError(message);
  return true;
}
